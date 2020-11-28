using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace UHWID
{
    public static class UHWIDEngine
    {
        public static string SimpleUid { get; private set; }

        public static string AdvancedUid { get; private set; }

        static UHWIDEngine()
        {
            var volumeSerial = "";
            try
            {
                volumeSerial = DiskId.GetDiskId();
            } catch { }
            var cpuId = "";
            try
            {
                cpuId = CpuId.GetCpuId();
            } catch { }
            var windowsId = "";
            try
            {
                windowsId = WindowsId.GetWindowsId();
            } catch { }
            SimpleUid = volumeSerial + cpuId;
            AdvancedUid = SimpleUid + windowsId;
        }
    }
    internal static class CpuId
    {

        [DllImport("user32", EntryPoint = "CallWindowProcW", CharSet = CharSet.Unicode, SetLastError = true,
            ExactSpelling = true)]
        private static extern IntPtr CallWindowProcW([In] byte[] bytes, IntPtr hWnd, int msg, [In, Out] byte[] wParam,
            IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool VirtualProtect([In] byte[] bytes, IntPtr size, int newProtect, out int oldProtect);

        const int PAGE_EXECUTE_READWRITE = 0x40;

        public static string GetCpuId()
        {
            var sn = new byte[8];

            return !ExecuteCode(ref sn) ? "ND" : string.Format("{0:X8}{1:X8}", BitConverter.ToUInt32(sn, 4), BitConverter.ToUInt32(sn, 0));
        }

        private static bool ExecuteCode(ref byte[] result)
        {
            /* The opcodes below implement a C function with the signature:
             * __stdcall CpuIdWindowProc(hWnd, Msg, wParam, lParam);
             * with wParam interpreted as an 8 byte unsigned character buffer.
             * */

            var isX64Process = IntPtr.Size == 8;
            byte[] code;

            if (isX64Process)
            {
                code = new byte[]
                {
                    0x53, /* push rbx */
                    0x48, 0xc7, 0xc0, 0x01, 0x00, 0x00, 0x00, /* mov rax, 0x1 */
                    0x0f, 0xa2, /* cpuid */
                    0x41, 0x89, 0x00, /* mov [r8], eax */
                    0x41, 0x89, 0x50, 0x04, /* mov [r8+0x4], edx */
                    0x5b, /* pop rbx */
                    0xc3, /* ret */
                };
            }
            else
            {
                code = new byte[]
                {
                    0x55, /* push ebp */
                    0x89, 0xe5, /* mov  ebp, esp */
                    0x57, /* push edi */
                    0x8b, 0x7d, 0x10, /* mov  edi, [ebp+0x10] */
                    0x6a, 0x01, /* push 0x1 */
                    0x58, /* pop  eax */
                    0x53, /* push ebx */
                    0x0f, 0xa2, /* cpuid    */
                    0x89, 0x07, /* mov  [edi], eax */
                    0x89, 0x57, 0x04, /* mov  [edi+0x4], edx */
                    0x5b, /* pop  ebx */
                    0x5f, /* pop  edi */
                    0x89, 0xec, /* mov  esp, ebp */
                    0x5d, /* pop  ebp */
                    0xc2, 0x10, 0x00, /* ret  0x10 */
                };
            }

            var ptr = new IntPtr(code.Length);

            if (!VirtualProtect(code, ptr, PAGE_EXECUTE_READWRITE, out _))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            ptr = new IntPtr(result.Length);
            return CallWindowProcW(code, IntPtr.Zero, 0, result, ptr) != IntPtr.Zero;

        }
    }
    internal class DiskId
    {
        public static string GetDiskId()
        {
            return GetDiskId("");
        }
        private static string GetDiskId(string diskLetter)
        {
            //Find first drive
            if (string.IsNullOrEmpty(diskLetter))
            {
                foreach (var compDrive in DriveInfo.GetDrives())
                {
                    if (compDrive.IsReady)
                    {
                        diskLetter = compDrive.RootDirectory.ToString();
                        break;
                    }
                }
            }
            if (!string.IsNullOrEmpty(diskLetter) && diskLetter.EndsWith(":\\"))
            {
                //C:\ -> C
                diskLetter = diskLetter.Substring(0, diskLetter.Length - 2);
            }
            var disk = new ManagementObject(@"win32_logicaldisk.deviceid=""" + diskLetter + @":""");
            disk.Get();

            var volumeSerial = disk["VolumeSerialNumber"].ToString();
            disk.Dispose();

            return volumeSerial;
        }
    }
    internal class WindowsId
    {
        public static string GetWindowsId()
        {
            var windowsInfo = "";
            var managClass = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_OperatingSystem");

            var managCollec = managClass.Get();

            var is64Bits = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"));

            foreach (var o in managCollec)
            {
                var managObj = (ManagementObject)o;
                windowsInfo = managObj.Properties["Caption"].Value + Environment.UserName + (string)managObj.Properties["Version"].Value;
                break;
            }
            windowsInfo = windowsInfo.Replace(" ", "");
            windowsInfo = windowsInfo.Replace("Windows", "");
            windowsInfo = windowsInfo.Replace("windows", "");
            windowsInfo += (is64Bits) ? " 64bit" : " 32bit";

            //md5 hash of the windows version
            var md5Hasher = MD5.Create();
            var wi = md5Hasher.ComputeHash(Encoding.Default.GetBytes(windowsInfo));
            var wiHex = BitConverter.ToString(wi).Replace("-", "");
            return wiHex;
        }
    }
}