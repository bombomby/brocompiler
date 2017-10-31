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
            Surface.Background = new SolidColorBrush(ContentColor);
            Surface.OnDraw += Surface_OnDraw;
        }

        DynamicMesh BackgroundMesh;

        private void Surface_OnDraw(DXCanvas canvas, DXCanvas.Layer layer)
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
            InitSurface();
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
            BackgroundMesh = Surface.CreateMesh();
            BackgroundMesh.AddRect(new System.Windows.Rect(0.0, 0.0, 0.5, 0.5), Colors.Red);
            BackgroundMesh.Update(Surface.RenderDevice);

            if (item == null)
                return;

            _root = item;
        }
    }
}
