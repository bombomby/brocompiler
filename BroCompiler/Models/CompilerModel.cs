using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BroControls;
using BroCollector;

namespace BroCompiler.Models
{
    public class ProcessGroupModel : Timeline.IItem
    {
        public string Name { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public List<Timeline.IItem> Children { get; set; }

        public ProcessGroupModel(String name, IList<ProcessData> processes)
        {
            Name = name;

            Start = DateTime.MaxValue;
            Finish = DateTime.MinValue;

            Children = new List<Timeline.IItem>(processes.Count);
            foreach (ProcessData process in processes)
            {
                Children.Add(new ProcessTimelineItem(process));
                if (process.Start < Start)
                    Start = process.Start;

                if (process.Finish > Finish)
                    Finish = process.Finish;
            }
        }
    }

    public class ProcessTimelineItem : Timeline.IItem
    {
        public ProcessData Process { get; set; }

        public string Name => Process.Name;
        public DateTime Start => Process.Start;
        public DateTime Finish => Process.Finish;

        public List<Timeline.IItem> Children { get; set; }

        public ProcessTimelineItem(ProcessData process)
        {
            Process = process;

            Children = new List<Timeline.IItem>(process.Threads.Count);
            process.Threads.ForEach(thread => Children.Add(new ThreadTimelineItem(thread)));
        }
    }

    public class ThreadTimelineItem : Timeline.IItem
    {
        public ThreadData Thread { get; set; }

        public string Name => Thread.ThreadID.ToString();
        public DateTime Start => Thread.Start;
        public DateTime Finish => Thread.Finish;

        public List<Timeline.IItem> Children { get; set; }

        public ThreadTimelineItem(ThreadData thread)
        {
            Thread = thread;
        }
    }
}
