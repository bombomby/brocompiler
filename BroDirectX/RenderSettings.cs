using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BroDirectX
{
    class RenderSettings
    {
        public static Vector DpiScale = new Vector(1.0, 1.0);

        static RenderSettings()
        {
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                DpiScale = new Vector(g.DpiX / 96.0, g.DpiY / 96.0);
            }
        }
    }
}
