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
        public interface IItem
        {
            Color Color { get; }
            double Height { get; }
            String Name { get; }
            DateTime Start { get; }
            DateTime Finish { get; }
            List<IItem> Children { get; }
        }

        public class Track
        {
            public IItem DataContext { get; set; }

            Mesh Mesh { get; set; }
            Mesh Lines { get; set; }

            public Track(IItem item)
            {
                DataContext = item;
            }

            internal void Build(DXCanvas canvas)
            {
                DynamicMesh meshBuilder = canvas.CreateMesh(DXCanvas.MeshType.Tris);
                DynamicMesh lineBuilder = canvas.CreateMesh(DXCanvas.MeshType.Lines);

                Rect rect = new Rect(0, 0, 1.0, 1.0);

                meshBuilder.AddRect(rect, DataContext.Color);
                lineBuilder.AddRect(rect, Colors.Black);

                Mesh = meshBuilder.Freeze(canvas.RenderDevice);
                Lines = lineBuilder.Freeze(canvas.RenderDevice);
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

        private IItem _root;
        public IItem Root
        {
            get { return _root; }
            set
            {
                UpdateRoot(value);
            }
        }
            
        private void UpdateRoot(IItem item)
        {
            if (item == null)
                return;

            _root = item;

            Tracks = new List<Track>();

            foreach (IItem child in item.Children)
                Tracks.Add(new Track(child));

            foreach (Track track in Tracks)
                track.Build(Surface.Canvas);

            UpdateTransforms();

            Surface.Canvas.Update();
        }
    }
}
