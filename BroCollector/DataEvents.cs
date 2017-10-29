using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroCollector
{
    public class EventData
    {
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public TimeSpan Duration => Finish - Start;
    }

    public class ThreadData : EventData
    {
        public int ThreadID { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class ProcessData : EventData, INotifyPropertyChanged
    {
        public UInt64 UniqueKey { get; set; }
        public String Name { get; set; }
        public String CommandLine { get; set; }
        public int ProcessID { get; set; }
        public int Result { get; set; }
        public Dictionary<String, String> Artifacts { get; set; }
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
}
