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
        public ProcessGroup Group { get; set; }

        private ETWCollector ETWCollector { get; set; }

        public DataCollector(Config config)
        {
            Group = new ProcessGroup();

            ETWCollector = new ETWCollector();
            ETWCollector.SetProcessFilter(config.ProcessFilters);
            ETWCollector.ProcessEvent += ETWCollector_ProcessEvent;
        }

        private void ETWCollector_ProcessEvent(ProcessData obj)
        {
            Application.Current.Dispatcher.Invoke((Action)(() =>
            {
                lock(Group)
                {
                    Group.Add(obj);
                }
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
