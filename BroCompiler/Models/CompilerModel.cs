using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BroControls;
using BroCollector;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Media;

namespace BroCompiler.Models
{
    public class Consts
    {
        public const double RowHeight = 16.0;
    }

    public class ProcessUtils
    {
        public static Color CalculateColor(String name)
        {
            if (name.Equals("link.exe", StringComparison.InvariantCultureIgnoreCase))
                return Colors.LimeGreen;

            if (name.Equals("cl.exe", StringComparison.InvariantCultureIgnoreCase))
                return Colors.Tomato;

            Random rnd = new Random(name.GetHashCode());
            return new Color() { R = (byte)rnd.Next(), G = (byte)rnd.Next(), B = (byte)rnd.Next() };
        }
    }

    // [Item]...[Item]...[Item]
    // .........[Item].........
    // ....[Item]...[Item].....
    public class ProcessGroupModel : Timeline.IBoard
    {
        public string Name { get; set; }

        // IBoard
        public double Height { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public List<Timeline.IGroup> Children { get; set; }


        public ProcessGroupModel(String name, ProcessGroup group)
        {
            Name = name;

            Start = DateTime.MaxValue;
            Finish = DateTime.MinValue;

            Children = new List<Timeline.IGroup>();

            foreach (ProcessData process in group.Processes)
                Add(process);

            foreach (Timeline.IGroup g in Children)
            {
                Height = Height + g.Height;

                if (g.Start < Start)
                    Start = g.Start;

                if (g.Finish > Finish)
                    Finish = g.Finish;
            }
        }

        private void Add(ProcessData process)
        {
            Timeline.IItem item = new ProcessTimelineItem(process);
            
            foreach (Timeline.IGroup group in Children)
            {
                if (group.Finish < item.Start)
                {
                    (group as ProcessTimeLineGroup).Add(item);
                    return;
                }
            }

            ProcessTimeLineGroup newGroup = new ProcessTimeLineGroup();
            newGroup.Add(item);
            Children.Add(newGroup);
        }
    }

    // [Item]...[Item]...[Item]
    public class ProcessTimeLineGroup : Timeline.IGroup
    {
        // IGroup
        public double Height { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public List<Timeline.IItem> Children { get; set; }

        public ProcessTimeLineGroup()
        {
            Start = DateTime.MaxValue;
            Finish = DateTime.MinValue;
            Children = new List<Timeline.IItem>();
        }

        public void Add(Timeline.IItem item)
        {
            Children.Add(item);

            if (item.Start < Start)
                Start = item.Start;

            if (item.Finish > Finish)
                Finish = item.Finish;

            if (item.Height > Height)
                Height = item.Height;
        }
    }

    // [Item]
    public class ProcessTimelineItem : Timeline.IItem
    {
        public ProcessData Process { get; set; }

        public string Name => Process.Name;
        public DateTime Start => Process.Start;
        public DateTime Finish => Process.Finish;

        public List<Timeline.IItem> Children { get; set; }

        public Color Color => ProcessUtils.CalculateColor(Name);
        public double Height => Process.Finish > Process.Start ? Consts.RowHeight : 0.0;

        public ProcessTimelineItem(ProcessData process)
        {
            Process = process;

            Children = new List<Timeline.IItem>(process.Threads.Count);
            process.Threads.ForEach(thread => Children.Add(new ThreadTimelineItem(thread)));
        }
    }


    public class ThreadGroupModel : Timeline.IBoard
    {
        public ProcessData DataContext { get; set; }

        // IBoard
        public double Height { get; set; }
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
        public List<Timeline.IGroup> Children { get; set; }


        public ThreadGroupModel(ProcessData process)
        {
            DataContext = process;

            Start = DateTime.MaxValue;
            Finish = DateTime.MinValue;

            Children = new List<Timeline.IGroup>();

            foreach (ThreadData thread in process.Threads)
            {
                Children.Add(new ThreadTimeLineGroup(thread));

                if (thread.Start < Start)
                    Start = thread.Start;

                if (thread.Finish > Finish)
                    Finish = thread.Finish;
            }

            Height = Children.Sum(c => Height);
        }
    }

    public class ThreadTimeLineGroup : Timeline.IGroup
    {
        ThreadData DataContext { get; set; }

        // IGroup
        public double Height => Consts.RowHeight;
        public DateTime Start => DataContext.Start;
        public DateTime Finish => DataContext.Finish;
        public List<Timeline.IItem> Children { get; set; }

        public ThreadTimeLineGroup(ThreadData thread)
        {
            DataContext = thread;
            Children = new List<Timeline.IItem>();
            Children.Add(new ThreadTimelineItem(thread));
        }
    }

    // [Item] => {A, B, C}
    public class ThreadTimelineItem : Timeline.IItem
    {
        public ThreadData Thread { get; set; }

        public string Name => Thread.ThreadID.ToString();
        public DateTime Start => Thread.Start;
        public DateTime Finish => Thread.Finish;

        public List<Timeline.IItem> Children { get; set; }

        public Color Color => Colors.SkyBlue;
        public double Height => Consts.RowHeight;

        public ThreadTimelineItem(ThreadData thread)
        {
            Thread = thread;
        }
    }
}
