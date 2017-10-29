using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BroCollector
{
    public class DataCollector
    {
        public ObservableCollection<ProcessData> ProcessEvents { get; set; }

        private ETWCollector ETWCollector { get; set; }

        public DataCollector(Config config)
        {
            ProcessEvents = new ObservableCollection<ProcessData>();

            ETWCollector = new ETWCollector();
            ETWCollector.SetProcessFilter(config.ProcessFilters);
            ETWCollector.ProcessEvent += ETWCollector_ProcessEvent;
        }

        private void ETWCollector_ProcessEvent(ProcessData obj)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                ProcessEvents.Add(obj);
            }));
        }

        public void Start()
        {
            ETWCollector.Start();
        }

        public void Stop()
        {
            ETWCollector.Stop();
        }
    }
}
