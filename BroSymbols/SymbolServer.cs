using BroInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BroInterop.ntdll;

namespace BroSymbols
{
    public class SymbolServer : IDisposable
    {
        public class Image
        {
            public String Name { get; set; }
            public IntPtr ImageBase { get; set; }
            public UInt32 ImageSize { get; set; }
        }

        private Dictionary<IntPtr, Image> ImageMap { get; set; }
        private DbgHelp DbgHelpProcessor;

        private void LoadSystemSymbols()
        {
            List<SYSTEM_MODULE_INFORMATION> moduleList = GetLoadedSystemModules();
            foreach (SYSTEM_MODULE_INFORMATION module in moduleList)
            {
                String name = module.ImageName.Replace("\\SystemRoot", "%SystemRoot%");
                String path = Environment.ExpandEnvironmentVariables(name);
                LoadModule(new Image() {
                    Name = path,
                    ImageBase = module.ImageBase,
                    ImageSize = module.ImageSize
                });
            }
        }

        private bool IsInitialized { get; set; }

        public SymbolServer()
        {
            DbgHelpProcessor = new DbgHelp(null);
            ImageMap = new Dictionary<IntPtr, Image>();
            LoadSystemSymbols();
        }

        public void LoadModule(Image image)
        {
            //ImageMap.Add(image.ImageBase, image);
            DbgHelpProcessor.LoadModule(image.Name, (ulong)image.ImageBase.ToInt64(), image.ImageSize);
        }

        public void UnloadModule(Image image)
        {
            //ImageMap.Remove(image.ImageBase);
            DbgHelpProcessor.UnloadModule((ulong)image.ImageBase.ToInt64());
        }

        public bool Resolve(ulong address, out SymbolInfo symbol)
        {
            return DbgHelpProcessor.LookupSymbol(address, out symbol);
        }

        public void Dispose()
        {
            DbgHelpProcessor.Dispose();
        }
    }
}
