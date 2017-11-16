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
using System.Diagnostics;
using BroSymbols;

namespace BroCompiler.Models
{
    public class Consts
    {
        public const double RowHeight = 16.0;
    }

    public class ProcessUtils
    {
        public static SymbolServer Symbols = new SymbolServer();

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
        public double BaseHeight => Height;

        public ProcessTimelineItem(ProcessData process)
        {
            Process = process;
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

            process.Images.Sort();

            Children = new List<Timeline.IGroup>();

            foreach (ThreadData thread in process.Threads.Values)
            {
                Children.Add(new ThreadTimeLineGroup(process, thread));

                if (thread.Start < Start)
                    Start = thread.Start;

                if (thread.Finish > Finish)
                    Finish = thread.Finish;
            }

            Height = Children.Sum(c => c.Height);
        }
    }

    public class ThreadTimeLineGroup : Timeline.IGroup
    {
        ProcessData Process { get; set; }
        ThreadData DataContext { get; set; }

        // IGroup
        public double Height { get; set; }
        public DateTime Start => DataContext.Start;
        public DateTime Finish => DataContext.Finish;
        public List<Timeline.IItem> Children { get; set; }

        public ThreadTimeLineGroup(ProcessData process, ThreadData thread)
        {
            Process = process;
            DataContext = thread;
            Children = new List<Timeline.IItem>();
            Timeline.IItem item = new ThreadTimelineItem(process, thread);
            Children.Add(item);
            Height = item.Height;
        }
    }

    // [Item] => {A, B, C}
    public class ThreadTimelineItem : FunctionTimelineItem
    {
        public ThreadTimelineItem(ProcessData process, ThreadData thread) : base(thread.ThreadID.ToString(), thread)
        {
            Color = Colors.SkyBlue;
            Children = new List<Timeline.IItem>();

            //foreach (WorkIntervalData work in thread.WorkIntervals)
            //{
            //    AddChild(new FunctionTimelineItem("Work", work));
            //}

            foreach (SysCallData call in thread.SysCalls)
            {
                SymbolServer.Symbol symbol = ProcessUtils.Symbols.Resolve(call.Address);
                //ImageData image = process.GetImageData(call.Address);

                AddChild(new FunctionTimelineItem(symbol.Name, call));
            }

            //thread.IORequests.Sort((a, b) => a.Start.CompareTo(b.Start));

            //foreach (IOData data in thread.IORequests)
            //{
            //    AddChild(new FunctionTimelineItem(data.IOType.ToString(), data));
            //}
        }
    }

    // [Funtion] => {A, B, C}
    public class FunctionTimelineItem : Timeline.IItem
    {
        public String Name { get; set; }
        public EventData DataContext { get; set; }

        public DateTime Start => DataContext.Start;
        public DateTime Finish => DataContext.Finish;

        public List<Timeline.IItem> Children { get; set; }

        public Color Color { get; set; }

        public double BaseHeight => Consts.RowHeight;

        private double _height = 0.0;
        public double Height
        {
            get
            {
                if (_height < BaseHeight)
                {
                    _height = (Children.Count > 0 ? Children.Max(c => c.Height) : 0.0) + BaseHeight;
                }

                return _height;
            }
        }

        public FunctionTimelineItem(String name, EventData function)
        {
            Color = Colors.Orange;
            DataContext = function;
            Name = name;
            Children = new List<Timeline.IItem>();
        }

        public void AddChild(FunctionTimelineItem child)
        {
            if (child.Finish <= child.Start)
                return;

            if (Children.Count == 0 || Children.Last().Finish <= child.Start)
            {
                Children.Add(child);
            }
            else
            {
                Debug.Assert(Timeline.Contains(Children.Last(), child), "Function array is not sorted!");
                (Children.Last() as FunctionTimelineItem).AddChild(child);
            }
        }
    }
}
