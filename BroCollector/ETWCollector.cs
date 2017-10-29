using System;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Diagnostics.Tracing.Parsers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

namespace BroCollector
{
    class ETWCollector : IDisposable
    {
        TraceEventSession Session { get; set; }
        Task ReaderTask { get; set; }

        Dictionary<int, ProcessData> ProcessDataMap { get; set; }
        HashSet<String> Filters { get; set; }

        public event Action<ProcessData> ProcessEvent;

        public ETWCollector()
        {
            Session = new TraceEventSession("BroCollector");
            Session.BufferSizeMB = 256;

            Session.EnableKernelProvider(KernelTraceEventParser.Keywords.Process | KernelTraceEventParser.Keywords.Thread);

            Session.Source.Kernel.ProcessStart += Kernel_ProcessStart;
            Session.Source.Kernel.ProcessStop += Kernel_ProcessStop;

            Session.Source.Kernel.ThreadStart += Kernel_ThreadStart;
            Session.Source.Kernel.ThreadStop += Kernel_ThreadStop;

            ProcessDataMap = new Dictionary<int, ProcessData>();
        }

        private void Kernel_ThreadStart(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ThreadTraceData obj)
        {
            ProcessData process = null;
            if (ProcessDataMap.TryGetValue(obj.ProcessID, out process))
            {
                process.Threads.Add(new ThreadData()
                {
                    ThreadID = obj.ThreadID,
                    Start = obj.TimeStamp,
                });
            }
        }

        private void Kernel_ThreadStop(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ThreadTraceData obj)
        {
            ProcessData process = null;
            if (ProcessDataMap.TryGetValue(obj.ProcessID, out process))
            {
                for (int i = process.Threads.Count - 1; i >= 0; --i)
                {
                    ThreadData thread = process.Threads[i];
                    if (thread.ThreadID == obj.ThreadID && thread.Finish < thread.Start)
                    {
                        process.Threads[i].Finish = obj.TimeStamp;
                        break;
                    }
                }
            }
        }

        public void SetProcessFilter(IEnumerable<String> filters)
        {
            Filters = new HashSet<string>(filters, StringComparer.OrdinalIgnoreCase);
        }

        private static void CollectArtifacts(ProcessData ev)
        {
            for (int start = ev.CommandLine.IndexOf('@'); start != -1; start = ev.CommandLine.IndexOf('@', start + 1))
            {
                int finish = Math.Max(ev.CommandLine.IndexOf(' ', start), ev.CommandLine.Length);
                String path = ev.CommandLine.Substring(start + 1, finish - start - 1);

                try
                {
                    String text = File.ReadAllText(path);
                    ev.AddArtifact(path, text);
                }
                catch (FileNotFoundException) { }
            }
        }

        private void Kernel_ProcessStart(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ProcessTraceData obj)
        {
            if (Filters.Contains(obj.ImageFileName))
            {
                ProcessData ev = new ProcessData()
                {
                    Name = obj.ImageFileName,
                    CommandLine = obj.CommandLine,
                    Start = obj.TimeStamp,
                    ProcessID = obj.ProcessID,
                    UniqueKey = obj.UniqueProcessKey
                };

                ProcessDataMap.Add(obj.ProcessID, ev);

                Task.Run(() => CollectArtifacts(ev));
            }
        }

        private void Kernel_ProcessStop(Microsoft.Diagnostics.Tracing.Parsers.Kernel.ProcessTraceData obj)
        {
            ProcessData ev = null;
            if (ProcessDataMap.TryGetValue(obj.ProcessID, out ev))
            {
                ev.Finish = obj.TimeStamp;
                ev.Result = obj.ExitStatus;
                ProcessDataMap.Remove(obj.ProcessID);

                ProcessEvent?.Invoke(ev);
            }
        }

        public void Start()
        {
            ReaderTask = Task.Factory.StartNew(() =>
            {
                Session.Source.Process();
            });
        }


        public void Stop()
        {
            // TODO!!!
        }

        public void Dispose()
        {
            Session?.Dispose();
        }
    }
}
