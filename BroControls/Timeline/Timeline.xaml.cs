using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using BroDirectX;
using System.Windows;

namespace BroControls
{

    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        public static double NormalizeTime(DateTime point, IDurationable parent)
        {
            double duration = (parent.Finish - parent.Start).TotalSeconds;
            return (point - parent.Start).TotalSeconds / duration;
        }

        public static Segment NormalizeTime(IDurationable interval, IDurationable parent)
        {
            double duration = (parent.Finish - parent.Start).TotalSeconds;
            return new Segment
            {
                Start = (interval.Start - parent.Start).TotalSeconds / duration,
                Finish = (interval.Finish - parent.Start).TotalSeconds / duration,
            };
        }

        public static bool Contains(IDurationable parent, IDurationable child)
        {
            return parent.Start <= child.Start && child.Finish <= parent.Finish;
        }

        public struct Segment
        {
            public double Start;
            public double Finish;
            public double Length => Finish - Start;
        }

        public interface IDurationable
        {
            DateTime Start { get; }
            DateTime Finish { get; }
        }

        public interface IItem : IDurationable
        {
            String Name { get; }
            Color Color { get; }
            double BaseHeight { get; }
            double Height { get; }
            List<IItem> Children { get; }
            bool IsStroke { get; }
        }

        public interface IGroup : IDurationable
        {
            double Height { get; }
            List<IItem> Children { get; }
        }

        public interface IBoard : IDurationable
        {
            double Height { get; }
            List<IGroup> Children { get; }
        }


        public class TrackItem
        {
            const double TextOffset = 1.0;

            public IItem DataContext { get; set; }

            public double MaxHeight { get; set; }

            public struct TextEntry
            {
                public String Text;
                public Point Position;
                public double Width;
            }

            List<TextEntry> TextBlocks;

            Mesh Mesh { get; set; }
            Mesh Lines { get; set; }

            public TrackItem(IItem item)
            {
                DataContext = item;
            }

            internal void Build(IGroup group, DXCanvas canvas)
            {
                TextBlocks = new List<TextEntry>();

                DynamicMesh meshBuilder = canvas.CreateMesh(DXCanvas.MeshType.Tris);
                DynamicMesh lineBuilder = canvas.CreateMesh(DXCanvas.MeshType.Lines);

                MaxHeight = group.Height;

                Build(DataContext, DataContext, 0.0, meshBuilder, lineBuilder);

                Mesh = meshBuilder.Freeze(canvas.RenderDevice);
                Lines = lineBuilder.Freeze(canvas.RenderDevice);

                double duration = (group.Finish - group.Start).TotalSeconds;

                Matrix localTransform = new Matrix();
                localTransform.Scale((DataContext.Finish - DataContext.Start).TotalSeconds / duration, 1.0);
                localTransform.Translate((DataContext.Start - group.Start).TotalSeconds / duration, 0.0);

                if (Mesh != null)
                    Mesh.LocalTransform = localTransform;

                if (Lines != null)
                    Lines.LocalTransform = localTransform;

                TextBlocks.Sort((a, b) => -a.Width.CompareTo(b.Width));
            }

            internal void Build(IItem root, IItem item, double offset, DynamicMesh meshBuilder, DynamicMesh lineBuilder)
            {
                double duration = (root.Finish - root.Start).TotalSeconds;

                Rect rect = new Rect((item.Start - root.Start).TotalSeconds / duration, offset / MaxHeight, (item.Finish - item.Start).TotalSeconds / duration, item.BaseHeight / MaxHeight);

                if (!String.IsNullOrEmpty(item.Name))
                {
                    TextBlocks.Add(new TextEntry()
                    {
                        Text = item.Name,
                        Position = rect.TopLeft,
                        Width = rect.Width,
                    });
                }

                meshBuilder.AddRect(rect, item.Color);

                if (item.IsStroke)
                    lineBuilder.AddRect(rect, Colors.Black);

                offset += item.BaseHeight;

                if (item.Children != null)
                {
                    foreach (IItem child in item.Children)
                    {
                        Build(root, child, offset, meshBuilder, lineBuilder);
                    }
                }
            }

            internal Matrix Transform
            {
                set
                {
                    if (Mesh != null)
                        Mesh.WorldTransform = value;

                    if (Lines != null)
                        Lines.WorldTransform = value;
                }
            }

            const double TextDrawLimit = 2.0;

            internal void Draw(DXCanvas canvas, DXCanvas.Layer layer, IBoard board, ZoomCanvas.ZoomScroll scroll, Vector offset)
            {
                canvas.Draw(Mesh);
                canvas.Draw(Lines);

                Segment unitBox = NormalizeTime(DataContext, board);

                double unitThreashold = scroll.ToUnitLength(TextDrawLimit);

                foreach (TextEntry block in TextBlocks)
                {
                    double blockWidth = scroll.ToPixelLength(unitBox.Length * block.Width);

                    if (blockWidth < TextDrawLimit)
                        break;

                    Point pixelPos = new Point(scroll.ToPixel(unitBox.Start + unitBox.Length * block.Position.X) + TextOffset, offset.Y + block.Position.Y * MaxHeight);
                    canvas.Text.Draw(pixelPos, block.Text, Colors.Black, TextAlignment.Left, blockWidth);
                }
                
            }
        }

        public class Track
        {
            public Vector Offset { get; set; }
            public IGroup DataContext { get; set; }
            public List<TrackItem> Children { get; set; }

            public Track(IGroup group)
            {
                DataContext = group;

                Children = new List<TrackItem>();
                foreach (IItem item in DataContext.Children)
                    if (item.Finish > item.Start)
                        Children.Add(new TrackItem(item));
            }

            internal void Build(DXCanvas canvas)
            {
                Children.ForEach(item => item.Build(DataContext, canvas));
            }

            internal Matrix Transform
            {
                set
                {
                    Children.ForEach(item => item.Transform = value);
                }
            }

            internal void Draw(DXCanvas canvas, DXCanvas.Layer layer, IBoard board, ZoomCanvas.ZoomScroll scroll)
            {
                Children.ForEach(item => item.Draw(canvas, layer, board, scroll, Offset));
            }
        }

        public List<Track> Tracks = new List<Track>();

        System.Windows.Media.Color ContentColor { get; set; }

        private void InitColors()
        {
            ContentColor = (System.Windows.Media.Color)FindResource("WhiteColor");
        }

        private void InitSurface()
        {
            Surface.Canvas.Background = new SolidColorBrush(ContentColor);
            Surface.OnDraw += Surface_OnDraw;
        }

        private void UpdateTransforms()
        {
            DateTime start = DateTime.MaxValue;
            DateTime finish = DateTime.MinValue;
            double totalHeight = 0.0;

            foreach (Track track in Tracks)
            {
                if (track.DataContext.Start < start)
                    start = track.DataContext.Start;

                if (track.DataContext.Finish > finish)
                    finish = track.DataContext.Finish;

                track.Offset = new Vector(0, totalHeight);
                totalHeight += track.DataContext.Height;
            }

            double totalDuration = (finish - start).TotalSeconds;

            double height = 0.0;
            foreach (Track track in Tracks)
            {
                Matrix transform = new Matrix();
                transform.Scale((track.DataContext.Finish - track.DataContext.Start).TotalSeconds / totalDuration, track.DataContext.Height / totalHeight);
                transform.Translate((track.DataContext.Start - start).TotalSeconds / totalDuration, height / totalHeight);
                track.Transform = transform;
                height += track.DataContext.Height;
            }

            Surface.Canvas.Height = totalHeight;
        }

        private void Surface_OnDraw(DXCanvas canvas, DXCanvas.Layer layer, ZoomCanvas.ZoomScroll scroll)
        {
            switch (layer)
            {
                case DXCanvas.Layer.Normal:
                    foreach (Track track in Tracks)
                    {
                        track.Draw(canvas, layer, Board, scroll);
                    }
                    break;
            }
        }

        public Timeline()
        {
            InitializeComponent();
            InitColors();

            Loaded += Timeline_Loaded;
        }

        private void Timeline_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            InitSurface();
            Surface.Canvas.Update();
        }

        private IBoard _board;
        public IBoard Board
        {
            get { return _board; }
            set
            {
                UpdateRoot(value);
            }
        }
            
        private void UpdateRoot(IBoard board)
        {
            if (board == null)
                return;

            _board = board;

            Tracks = new List<Track>();

            foreach (IGroup group in board.Children)
                Tracks.Add(new Track(group));

            foreach (Track track in Tracks)
                track.Build(Surface.Canvas);

            UpdateTransforms();

            Surface.Canvas.Update();
        }
    }
}
