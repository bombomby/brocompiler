using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using BroDirectX;

namespace BroControls
{

    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        public interface IItem
        {
            String Name { get; }
            DateTime Start { get; }
            DateTime Finish { get; }
            List<IItem> Children { get; }
        }

        System.Windows.Media.Color ContentColor { get; set; }

        private void InitColors()
        {
            ContentColor = (System.Windows.Media.Color)FindResource("WhiteColor");
        }

        private void InitSurface()
        {
            Surface.Canvas.Background = new SolidColorBrush(ContentColor);
            Surface.OnDraw += Surface_OnDraw1;

            BackgroundMesh = Surface.Canvas.CreateMesh();
            BackgroundMesh.AddRect(new System.Windows.Rect(0.0, 0.0, 0.5, 0.5), Colors.Red);
            BackgroundMesh.Update(Surface.Canvas.RenderDevice);
        }

        DynamicMesh BackgroundMesh;

        private void Surface_OnDraw1(DXCanvas canvas, DXCanvas.Layer layer, ZoomCanvas.ZoomScroll scroll)
        {
            switch (layer)
            {
                case DXCanvas.Layer.Background:
                    canvas.Draw(BackgroundMesh);
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
        }
    }
}
