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
        public interface IDurationable
        {
            DateTime Start { get; }
            DateTime Finish { get; }
        }

        public interface IItem : IDurationable
        {
            Color Color { get; }
            double Height { get; }
            String Name { get; }
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
            public IItem DataContext { get; set; }

            Mesh Mesh { get; set; }
            Mesh Lines { get; set; }

            public TrackItem(IItem item)
            {
                DataContext = item;
            }

            internal void Build(IGroup group, DXCanvas canvas)
            {
                DynamicMesh meshBuilder = canvas.CreateMesh(DXCanvas.MeshType.Tris);
                DynamicMesh lineBuilder = canvas.CreateMesh(DXCanvas.MeshType.Lines);

                Rect rect = new Rect(0, 0, 1.0, 1.0);

                meshBuilder.AddRect(rect, DataContext.Color);
                lineBuilder.AddRect(rect, Colors.Black);

                Mesh = meshBuilder.Freeze(canvas.RenderDevice);
                Lines = lineBuilder.Freeze(canvas.RenderDevice);

                double duration = (group.Finish - group.Start).TotalSeconds;

                Matrix localTransform = new Matrix();
                localTransform.Scale((DataContext.Finish - DataContext.Start).TotalSeconds / duration, DataContext.Height / group.Height);
                localTransform.Translate((DataContext.Start - group.Start).TotalSeconds / duration, 0.0);

                Mesh.LocalTransform = localTransform;
                Lines.LocalTransform = localTransform;
            }

            internal Matrix Transform
            {
                set
                {
                    Mesh.WorldTransform = value;
                    Lines.WorldTransform = value;
                }
            }

            internal void Draw(DXCanvas canvas, DXCanvas.Layer layer)
            {
                canvas.Draw(Mesh);
                canvas.Draw(Lines);
            }
        }

        public class Track
        {
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

            internal void Draw(DXCanvas canvas, DXCanvas.Layer layer)
            {
                Children.ForEach(item => item.Draw(canvas, layer));
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

        private void DrawTracks(DXCanvas canvas, DXCanvas.Layer layer, ZoomCanvas.ZoomScroll scroll)
        {

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

            Surface.Height = totalHeight;
        }

        private void Surface_OnDraw(DXCanvas canvas, DXCanvas.Layer layer, ZoomCanvas.ZoomScroll scroll)
        {
            switch (layer)
            {
                case DXCanvas.Layer.Normal:
                    foreach (Track track in Tracks)
                    {
                        track.Draw(canvas, layer);
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
