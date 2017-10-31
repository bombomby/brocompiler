using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BroDirectX
{
    /// <summary>
    /// Interaction logic for ZoomCanvas.xaml
    /// </summary>
    public partial class ZoomCanvas : UserControl
    {
        public bool CanZoomX { get; set; }
        public bool CanZoomY { get; set; }
        public DXCanvas Canvas { get { return CanvasElement; } }

        public System.Drawing.Size Size
        {
            get
            {
                return Canvas.RenderCanvas.ClientSize;
            }
        }

        public class ZoomScroll
        {
            public const double ZoomSpeed = 0.01;

            public Point PanOrigin { get; set; }
            public bool IsPanning { get; set; }
            public Rect Area { get; set; }

            public ZoomScroll()
            {
                Area = new Rect(0, 0, 1.0, 1.0);
            }
        }

        public ZoomScroll Scroll { get; set; }

        private void InitInput()
        {
            CanZoomX = true;
            CanZoomY = true;

            Scroll = new ZoomScroll();
            Canvas.RenderCanvas.MouseMove += RenderCanvas_MouseMove;
            Canvas.RenderCanvas.MouseWheel += RenderCanvas_MouseWheel;
            Canvas.RenderCanvas.MouseDown += RenderCanvas_MouseDown;
            Canvas.RenderCanvas.MouseUp += RenderCanvas_MouseUp;
            Canvas.RenderCanvas.MouseLeave += RenderCanvas_MouseLeave;

            Canvas.OnDraw += Canvas_OnDraw;
        }

        private void RenderCanvas_MouseLeave(object sender, EventArgs e)
        {
            Scroll.IsPanning = false;
        }

        private void RenderCanvas_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                Scroll.IsPanning = false;
        }

        private void RenderCanvas_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                Scroll.PanOrigin = new Point(e.Location.X, e.Location.Y);
                Scroll.IsPanning = true;
            }
        }

        private void RenderCanvas_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Point pos = new Point(e.Location.X, e.Location.Y);

            double delta = e.Delta * ZoomScroll.ZoomSpeed;
            double scale = delta > 0.0 ? 1 / delta : -delta;

            Vector ratio = new Vector(pos.X / Size.Width, pos.Y / Size.Height);
            Rect area = Scroll.Area;

            area.Width *= (CanZoomX ? scale : 1.0);
            area.Height *= (CanZoomY ? scale : 1.0);

            area.Location = area.Location + new Vector((Scroll.Area.Width - area.Width) * ratio.X, (Scroll.Area.Height - area.Height) * ratio.Y);

            Scroll.Area = area;

            Canvas.Update();
        }

        private void RenderCanvas_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (Scroll.IsPanning)
            {
                Point newPos = new Point(e.X, e.Y);
                Vector delta = newPos - Scroll.PanOrigin;
                Scroll.PanOrigin = newPos;

                Rect area = Scroll.Area;
                area.Location -= new Vector(area.Width * delta.X / Size.Width, area.Height * delta.Y / Size.Height);

                Scroll.Area = area;

                Canvas.Update();
            }
        }

        public delegate void OnDrawHandler(DXCanvas canvas, DXCanvas.Layer layer, ZoomScroll scroll);
        public event OnDrawHandler OnDraw;

        private void Canvas_OnDraw(DXCanvas canvas, DXCanvas.Layer layer)
        {
            Vector scale = new Vector(1.0 / Scroll.Area.Width, 1.0 / Scroll.Area.Height);
            Matrix cameraView = new Matrix(scale.X, 0, 0, scale.Y, -scale.X * Scroll.Area.Location.X, -scale.Y * Scroll.Area.Location.Y);
            canvas.CameraView = cameraView;

            OnDraw?.Invoke(canvas, layer, Scroll);
        }

        public ZoomCanvas()
        {
            InitializeComponent();
            InitInput();
        }
    }
}
