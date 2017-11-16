using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BroSymbols
{
    public class SymbolServer
    {
        public class Symbol
        {
            public ulong Address { get; set; }
            public String Name { get; set; }
            public Image Module { get; set; }

            public static Symbol Unknown = new Symbol() { Name = "Unknown" };
        }

        public class Image
        {
            public String Name { get; set; }
            public IntPtr ImageBase { get; set; }
            public UInt32 ImageSize { get; set; }
        }

        public List<Image> Images { get; set; }

        private void LoadSystemSymbols()
        {
            List<Interop.SYSTEM_MODULE_INFORMATION> moduleList = Interop.GetLoadedModuleList();
            foreach (Interop.SYSTEM_MODULE_INFORMATION module in moduleList)
            {
                Images.Add(new Image()
                {
                    Name = module.ImageName,
                    ImageBase = module.ImageBase,
                    ImageSize = module.ImageSize,
                });
            }
        }

        public SymbolServer()
        {
            Images = new List<Image>();
            LoadSystemSymbols();
        }

        public Image GetSystemModule(ulong address)
        {
            foreach (Image image in Images)
            {
                ulong baseAddress = (ulong)image.ImageBase.ToInt64();
                if (baseAddress <= address && address < baseAddress + image.ImageSize)
                    return image;
            }
            return null;
        }

        public Symbol Resolve(ulong address)
        {
            Image module = GetSystemModule(address);
            if (module != null)
            {
                return new Symbol()
                {
                    Address = address,
                    Module = module,
                    Name = "TEST!!"
                };
            }
            return Symbol.Unknown;
        }
    }
}
