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
    public class EventData
    {
        [DataMember]
        public DateTime Start { get; set; }
        [DataMember]
        public DateTime Finish { get; set; }
        public TimeSpan Duration => Finish - Start;
    }

    [DataContract]
    public class ThreadData : EventData
    {
        [DataMember]
        public int ThreadID { get; set; }
    }

    [DataContract]
    public class ProcessData : EventData, INotifyPropertyChanged
    {
        [DataMember]
        public UInt64 UniqueKey { get; set; }
        [DataMember]
        public String Name { get; set; }
        [DataMember]
        public String CommandLine { get; set; }
        [DataMember]
        public int ProcessID { get; set; }
        [DataMember]
        public int Result { get; set; }
        [DataMember]
        public Dictionary<String, String> Artifacts { get; set; }
        [DataMember]
        public List<ThreadData> Threads { get; set; }

        public String Text { get { return Artifacts != null ? Artifacts.Values.First() : String.Empty; } }

        public void AddArtifact(String name, String val)
        {
            if (Artifacts == null)
                Artifacts = new Dictionary<string, string>();

            Artifacts.Add(name, val);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Artifacts"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Text"));
        }

        public ProcessData()
        {
            Threads = new List<ThreadData>();
        }

        public event PropertyChangedEventHandler PropertyChanged;
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
