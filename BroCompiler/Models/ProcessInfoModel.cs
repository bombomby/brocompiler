using BroCollector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroCompiler.Models
{
    class ProcessInfoModel
    {
        public class IOInfo
        {
            public String Name { get; set; }
            public double TotalDuration { get; set; }
            public UInt64 TotalSize { get; set; }
            public int Count { get; set; }
            public double AvgDuration { get; set; }
            public UInt64 AvgSize { get; set; }

            public void Normalize()
            {
                AvgDuration = TotalDuration / Count;
                AvgSize = TotalSize / (UInt64)Count;
            }

            public void Add(IOData data)
            {
                Count++;
                TotalDuration += data.Duration;
                TotalSize += (UInt64)data.Size;
            }
        }

        public List<IOInfo> FileIO { get; set; }

        public ProcessInfoModel(List<ProcessData> dataCollection)
        {
            BuildFileIO(dataCollection);
        }

        private void BuildFileIO(List<ProcessData> dataCollection)
        {
            Dictionary<String, IOInfo> dictionary = new Dictionary<string, IOInfo>();

            foreach (ProcessData processData in dataCollection)
            {
                foreach (KeyValuePair<int, ThreadData> pair in processData.Threads)
                {
                    foreach (IOData ioData in pair.Value.IORequests)
                    {
                        IOInfo info = null;
                        if (!dictionary.TryGetValue(ioData.FileName, out info))
                            dictionary.Add(ioData.FileName, info = new IOInfo() { Name = ioData.FileName });

                        info.Add(ioData);
                    }
                }
            }

            FileIO = dictionary.Values.ToList();
            FileIO.ForEach(item => item.Normalize());
            FileIO.Sort((a, b) => -a.TotalDuration.CompareTo(b.TotalDuration));
        }

    }
}
