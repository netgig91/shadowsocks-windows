using System;
using System.Runtime.InteropServices;

using Microsoft.Win32;

using NLog;

using Shadowsocks.Std.Util;

using static Shadowsocks.Std.Util.Utils;

namespace Shadowsocks.Std.Win.Util
{
    public enum WindowsThemeMode { Dark, Light }

    public static class WinUtils
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        #region Windows Theme

        // Support on Windows 10 1903+
        public static WindowsThemeMode GetWindows10SystemThemeSetting(bool isVerbose)
        {
            WindowsThemeMode themeMode = WindowsThemeMode.Dark;
            try
            {
                RegistryKey REG_ThemesPersonalize = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", false);

                if (REG_ThemesPersonalize.GetValue("SystemUsesLightTheme") != null)
                {
                    if ((int)(REG_ThemesPersonalize.GetValue("SystemUsesLightTheme")) == 0) // 0:dark mode, 1:light mode
                        themeMode = WindowsThemeMode.Dark;
                    else
                        themeMode = WindowsThemeMode.Light;
                }
                else
                {
                    throw new Exception("Reg-Value SystemUsesLightTheme not found.");
                }
            }
            catch (Exception)
            {
                if (isVerbose)
                {
                    _logger.Info("Cannot get Windows 10 system theme mode, return default value 0 (dark mode).");
                }
            }
            return themeMode;
        }

        #endregion Windows Theme

        #region System / Memory

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadLibrary(string path);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetProcessWorkingSetSize(IntPtr process, UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);

        #endregion System / Memory

        #region registry

        public static RegistryKey OpenRegKey(string name, bool writable, RegistryHive hive = RegistryHive.CurrentUser)
        {
            // we are building x86 binary for both x86 and x64, which will
            // cause problem when opening registry key
            // detect operating system instead of CPU
            if (String.IsNullOrEmpty(name)) throw new ArgumentException(nameof(name));
            try
            {
                return RegistryKey.OpenBaseKey(hive, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32).OpenSubKey(name, writable);
            }
            catch (ArgumentException ae)
            {
                Application.InfoMessageBox($"OpenRegKey: {ae.ToString()}");
            }
            catch (Exception e)
            {
                _logger.LogUsefulException(e);
            }

            return null;
        }

        // See: https://msdn.microsoft.com/en-us/library/hh925568(v=vs.110).aspx
        public static bool IsSupportedRuntimeVersion()
        {
            // TODO 需要重新完善

            /*
             * +-----------------------------------------------------------------+----------------------------+
             * | Version                                                         | Value of the Release DWORD |
             * +-----------------------------------------------------------------+----------------------------+
             * | .NET Framework 4.6.2 installed on Windows 10 Anniversary Update | 394802                     |
             * | .NET Framework 4.6.2 installed on all other Windows OS versions | 394806                     |
             * +-----------------------------------------------------------------+----------------------------+
             */
            const int minSupportedRelease = 394802;

            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
            using (var ndpKey = OpenRegKey(subkey, false, RegistryHive.LocalMachine))
            {
                if (ndpKey?.GetValue("Release") != null && (int)ndpKey.GetValue("Release") >= minSupportedRelease)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion registry
    }
}