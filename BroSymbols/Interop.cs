using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BroSymbols
{
    public class Interop
    {
        public enum SYSTEM_INFORMATION_CLASS
        {
            SystemBasicInformation,
            SystemProcessorInformation,
            SystemPerformanceInformation,
            SystemTimeOfDayInformation,
            SystemPathInformation,
            SystemProcessInformation,
            SystemCallCountInformation,
            SystemDeviceInformation,
            SystemProcessorPerformanceInformation,
            SystemFlagsInformation,
            SystemCallTimeInformation,
            SystemModuleInformation,
            SystemLocksInformation,
            SystemStackTraceInformation,
            SystemPagedPoolInformation,
            SystemNonPagedPoolInformation,
            SystemHandleInformation,
            SystemObjectInformation,
            SystemPageFileInformation,
            SystemVdmInstemulInformation,
            SystemVdmBopInformation,
            SystemFileCacheInformation,
            SystemPoolTagInformation,
            SystemInterruptInformation,
            SystemDpcBehaviorInformation,
            SystemFullMemoryInformation,
            SystemLoadGdiDriverInformation,
            SystemUnloadGdiDriverInformation,
            SystemTimeAdjustmentInformation,
            SystemSummaryMemoryInformation,
            SystemNextEventIdInformation,
            SystemEventIdsInformation,
            SystemCrashDumpInformation,
            SystemExceptionInformation,
            SystemCrashDumpStateInformation,
            SystemKernelDebuggerInformation,
            SystemContextSwitchInformation,
            SystemRegistryQuotaInformation,
            SystemExtendServiceTableInformation,
            SystemPrioritySeperation,
            SystemPlugPlayBusInformation,
            SystemDockInformation,
            SystemPowerInformation,
            SystemProcessorSpeedInformation,
            SystemCurrentTimeZoneInformation,
            SystemLookasideInformation,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct SYSTEM_MODULE_INFORMATION
        {
            public UInt32 reserved1;
            public UInt32 reserved2;
            public IntPtr MappedBase;
            public IntPtr ImageBase;
            public UInt32 ImageSize;
            public UInt32 Flags;
            public UInt16 LoadOrderIndex;
            public UInt16 InitOrderIndex;
            public UInt16 LoadCount;
            public UInt16 ModuleNameOffset;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string ImageName;
        }

        public static class ntdll
        {
            [DllImport("ntdll.dll", EntryPoint = "ZwQuerySystemInformation")]
            public static extern IntPtr ZwQuerySystemInformation(
                SYSTEM_INFORMATION_CLASS SystemInformationClass, 
                IntPtr SystemInformation, 
                uint SystemInformationLength, 
                ref uint ReturnLength);
        }


        public static List<SYSTEM_MODULE_INFORMATION> GetLoadedModuleList()
        {
            uint returnSize = 0;
            ntdll.ZwQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemModuleInformation, IntPtr.Zero, 0, ref returnSize);

            // Allocate enough memory
            IntPtr pModuleList = Marshal.AllocHGlobal((int)returnSize);

            List<SYSTEM_MODULE_INFORMATION> modules = new List<SYSTEM_MODULE_INFORMATION>();

            try
            {
                // Query all the modules
                uint readSize = 0;
                IntPtr result = ntdll.ZwQuerySystemInformation(SYSTEM_INFORMATION_CLASS.SystemModuleInformation, pModuleList, returnSize, ref readSize);
                Debug.WriteLine("Result:" + result.ToInt64());

                int moduleCount = Marshal.ReadInt32(pModuleList);
                modules = new List<SYSTEM_MODULE_INFORMATION>(moduleCount);

                for (int i = 0; i < moduleCount; ++i)
                {
                    SYSTEM_MODULE_INFORMATION info = (SYSTEM_MODULE_INFORMATION)Marshal.PtrToStructure(pModuleList + 8 + i * Marshal.SizeOf(typeof(SYSTEM_MODULE_INFORMATION)), typeof(SYSTEM_MODULE_INFORMATION));
                    Debug.WriteLine("Loading {0}: {1:X}-{2:X}", info.ImageName, (UInt64)info.ImageBase.ToInt64(), (UInt64)info.ImageBase.ToInt64() + info.ImageSize);
                    modules.Add(info);
                }

                // Cast memory
                //moduleList = (MODULE_LIST)Marshal.PtrToStructure(pModuleList, typeof(MODULE_LIST));
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to marshal pointer to loaded module list:  " + ex.Message);
            }

            Marshal.FreeHGlobal(pModuleList);

            return modules;
        }
    }
}
