using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BroCollector
{
    [DataContract]
    public class EventData : INotifyPropertyChanged
    {
        private DateTime start = DateTime.MaxValue;
        private DateTime finish = DateTime.MinValue;

        [DataMember]
        public DateTime Start
        {
            get { return start; }
            set
            {
                start = value;
                RaisePropertyChanged("Start");
                RaisePropertyChanged("Duration");
            }
        }

        [DataMember]
        public DateTime Finish
        {
            get { return finish; }
            set
            {
                finish = value;
                RaisePropertyChanged("Finish");
                RaisePropertyChanged("Duration");
            }
        }

        public TimeSpan Duration => Finish - Start;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    [DataContract]
    public class SysCallData : EventData
    {
        public ulong Address { get; set; }
    }

    [DataContract]
    public class WorkIntervalData : EventData
    {
        public int WaitReason { get; set; }
        public int CpuID { get; set; }
    }

    [DataContract]
    public class ThreadData : EventData
    {
        [DataMember]
        public int ThreadID { get; set; }

        [DataMember]
        public List<SysCallData> SysCalls { get; set; }

        [DataMember]
        public List<WorkIntervalData> WorkIntervals { get; set; }

        [DataMember]
        public List<IOData> IORequests { get; set; }

        public ThreadData()
        {
            SysCalls = new List<SysCallData>();
            WorkIntervals = new List<WorkIntervalData>();
            IORequests = new List<IOData>();
        }
    }

    [DataContract]
    public class IOData : EventData
    {
        [DataContract]
        public enum Type
        {
            [EnumMember]
            Read,
            [EnumMember]
            Write,
        }

        [DataMember]
        public Type IOType { get; set; }

        [DataMember]
        public int ThreadID { get; set; }

        [DataMember]
        public int Size { get; set; }

        [DataMember]
        public long Offset { get; set; }

        [DataMember]
        public String FileName { get; set; }
    }


    [DataContract]
    public class ProcessData : EventData
    {
        [DataMember]
        public UInt64 UniqueKey { get; set; }
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public String CommandLine { get; set; }
        [DataMember]
        public int ProcessID { get; set; }

        private int? result = null;
        [DataMember]
        public int? Result
        {
            get
            {
                return result;
            }
            set
            {
                result = value;
                RaisePropertyChanged("Result");
            }
        }

        [DataMember]
        public Dictionary<String, String> Artifacts { get; set; }
        [DataMember]
        public Dictionary<int, ThreadData> Threads { get; set; }

        public String Text { get { return Artifacts != null ? Artifacts.Values.First() : String.Empty; } }

        public void AddArtifact(String name, String val)
        {
            if (Artifacts == null)
                Artifacts = new Dictionary<string, string>();

            Artifacts.Add(name, val);

            RaisePropertyChanged("Artifacts");
            RaisePropertyChanged("Text");
        }

        public ProcessData()
        {
            Threads = new Dictionary<int, ThreadData>();
        }
    }

    [DataContract]
    public class ProcessGroup
    {
        [DataMember]
        public ObservableCollection<ProcessData> Processes { get; set; }

        public void Add(ProcessData process)
        {
            Processes.Add(process);
        }

        public ProcessGroup()
        {
            Processes = new ObservableCollection<ProcessData>();
        }
    }
}
