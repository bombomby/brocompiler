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
using BroInterop;

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

            if (group.Processes.Count > 0)
            {
                Start = group.Processes.Min(p => p.Start);
                Finish = group.Processes.Max(p => p.Finish);
            }

            if (group.Counters != null)
            {
                for (int i = 0; i < group.Counters.Descriptions.Count; ++i)
                {
                    ProcessCountersGroup countersGroup = new ProcessCountersGroup(Start, Finish, group.Counters, i);
                    countersGroup.Start = Start;
                    countersGroup.Finish = Finish;
                    Children.Add(countersGroup);
                }
            }

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

            Height = Children.Sum(c => c.Height);
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

    // _____/-----\____/----\___/\__
    public class ProcessCountersGroup : Timeline.IGroup
    {
        public double Height { get; set; }

        public List<Timeline.IItem> Children { get; set; }

        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }

        public ProcessCountersGroup(DateTime start, DateTime finish, CounterGroup counters, int index)
        {
            Start = start;
            Finish = finish;

            Children = new List<Timeline.IItem>();

            CounterDescription desc = counters.Descriptions[index];
            ProcessChartLineItem item = new ProcessChartLineItem() { Name = desc.Name, Start = start, Finish = finish };

            for (int x = 0; x < counters.Samples.Count; ++x)
            {
                item.Points.Add(new KeyValuePair<DateTime, double>(counters.Samples[x].Timestamp, counters.Samples[x].Values[index]));
            }

            Children.Add(item);

            Height = Children.Max(c => c.Height);
        }
    }

    public class ProcessChartLineItem : Timeline.ILine
    {
        public Color StrokeColor => Colors.Red;
        public List<KeyValuePair<DateTime, double>> Points { get; set; }
        public string Name { get; set; }
        public double BaseHeight => Consts.RowHeight * 2;
        public double Height => BaseHeight;
        public bool IsStroke => true;
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }

        public ProcessChartLineItem() { Points = new List<KeyValuePair<DateTime, double>>(); }

        public Color Color => throw new NotImplementedException();
        public List<Timeline.IItem> Children => throw new NotImplementedException();
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
        public bool IsStroke => true;

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

            List<SymbolServer.Image> images = new List<SymbolServer.Image>();
            foreach (ImageData image in process.Images)
                images.Add(new SymbolServer.Image() { Name = image.FileName, ImageBase = new IntPtr((long)image.ImageBase), ImageSize = (uint)image.ImageSize });

            images.ForEach(img => ProcessUtils.Symbols.LoadModule(img));

            foreach (ThreadData thread in process.Threads.Values)
            {
                Children.Add(new ThreadTimeLineGroup(process, thread));

                if (thread.Start < Start)
                    Start = thread.Start;

                if (thread.Finish > Finish)
                    Finish = thread.Finish;
            }

            images.ForEach(img => ProcessUtils.Symbols.UnloadModule(img));

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

            List<FunctionTimelineItem> toAdd = new List<FunctionTimelineItem>();

            foreach (SysCallData call in thread.SysCalls)
            {
                SymbolInfo symbol = new SymbolInfo();
                ProcessUtils.Symbols.Resolve(call.Address, out symbol);
                toAdd.Add(new FunctionTimelineItem(symbol.Symbol, call));
            }

            foreach (IOData data in thread.IORequests)
            {
                toAdd.Add(new FunctionTimelineItem(data.FileName, data));
            }

            //GenerateSampledTree(process, thread);

            toAdd.Sort((a, b) => a.Start.CompareTo(b.Start));
            toAdd.ForEach(child => AddChild(child));

            for (int i = 0; i < thread.WorkIntervals.Count - 1; ++i)
            {
                EventData ev = new EventData() { Start = thread.WorkIntervals[i].Finish, Finish = thread.WorkIntervals[i + 1].Start };
                if (ev.IsValid)
                    Children.Add(new WaitTimelineItem(ev));
            }
        }

        private void GenerateSampledTree(ProcessData process, ThreadData thread)
        {
            List<FunctionTimelineItem> items = new List<FunctionTimelineItem>();
            List<Tuple<UInt64, EventData>> currentCallstack = new List<Tuple<ulong, EventData>>();
            SymbolInfo symbol = new SymbolInfo();

            foreach (CallstackData cs in thread.Callstacks)
            {
                int matchCount = 0;
                for (int i = 0; i < Math.Min(currentCallstack.Count, cs.Callstack.Length); ++i)
                    if (currentCallstack[i].Item1 == cs.Callstack[i])
                        ++matchCount;

                for (int i = currentCallstack.Count - 1; i >= matchCount; --i)
                    currentCallstack[i].Item2.Finish = cs.Timestamp;

                currentCallstack.RemoveRange(matchCount, currentCallstack.Count - matchCount);

                for (int i = matchCount; i < cs.Callstack.Length; ++i)
                {
                    EventData ev = new EventData() { Start = cs.Timestamp };
                    currentCallstack.Add(new Tuple<ulong, EventData>(cs.Callstack[i], ev));

                    if (ProcessUtils.Symbols.Resolve(cs.Callstack[i], out symbol))
                        items.Add(new FunctionTimelineItem(symbol.Symbol, ev));
                }
            }

            foreach (var pair in currentCallstack)
                pair.Item2.Finish = thread.Callstacks.Last().Timestamp;

            items.ForEach(item => AddChild(item));
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

        public bool IsStroke => false;

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

    public class WaitTimelineItem : Timeline.IItem
    {
        public EventData DataContext { get; set; }
        public Color Color => Colors.Tomato;
        public double BaseHeight => 4.0;
        public double Height => 4.0;
        public string Name => null;
        public List<Timeline.IItem> Children => null;
        public DateTime Start => DataContext.Start;
        public DateTime Finish => DataContext.Finish;
        public bool IsStroke => false;

        public WaitTimelineItem(EventData data)
        {
            DataContext = data;
        }
    }
}
