using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Win32;

using NLog;

using Shadowsocks.Std.Model;
using Shadowsocks.Std.Sys;
using Shadowsocks.Std.Util;
using Shadowsocks.Std.Win.Util;

namespace Shadowsocks.Std.Win.Sys
{
    public class AutoStartup : IAutoStartup
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        // Don't use Application.ExecutablePath
        // see https://stackoverflow.com/questions/12945805/odd-c-sharp-path-issue
        private static readonly string ExecutablePath = Assembly.GetEntryAssembly().Location;

        private static readonly string Key = $"Shadowsocks_{Utils.Application.StartupPath().GetHashCode()}";

        private readonly ShadowsocksContext context;

        public AutoStartup(ShadowsocksContext context)
        {
            this.context = context;
        }

        public bool Set(bool enabled)
        {
            RegistryKey runKey = null;
            try
            {
                runKey = WinUtils.OpenRegKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (runKey == null)
                {
                    _logger.Error(@"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Run");
                    return false;
                }
                if (enabled)
                {
                    runKey.SetValue(Key, ExecutablePath);
                }
                else
                {
                    runKey.DeleteValue(Key);
                }
                // When autostartup setting change, change RegisterForRestart state to avoid start 2 times
                context.RegisterForRestart(!enabled);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogUsefulException(e);
                return false;
            }
            finally
            {
                if (runKey != null)
                {
                    try
                    {
                        runKey.Close();
                        runKey.Dispose();
                    }
                    catch (Exception e)
                    {
                        _logger.LogUsefulException(e);
                    }
                }
            }
        }

        public bool Check()
        {
            RegistryKey runKey = null;
            try
            {
                runKey = WinUtils.OpenRegKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (runKey == null)
                {
                    _logger.Error(@"Cannot find HKCU\Software\Microsoft\Windows\CurrentVersion\Run");
                    return false;
                }
                string[] runList = runKey.GetValueNames();
                foreach (string item in runList)
                {
                    if (item.Equals(Key, StringComparison.OrdinalIgnoreCase))
                        return true;
                    else if (item.Equals("Shadowsocks", StringComparison.OrdinalIgnoreCase)) // Compatibility with older versions
                    {
                        string value = Convert.ToString(runKey.GetValue(item));
                        if (ExecutablePath.Equals(value, StringComparison.OrdinalIgnoreCase))
                        {
                            runKey.DeleteValue(item);
                            runKey.SetValue(Key, ExecutablePath);
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                _logger.LogUsefulException(e);
                return false;
            }
            finally
            {
                if (runKey != null)
                {
                    try
                    {
                        runKey.Close();
                        runKey.Dispose();
                    }
                    catch (Exception e)
                    {
                        _logger.LogUsefulException(e);
                    }
                }
            }
        }
    }

    public static class WinAutoStartup
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int RegisterApplicationRestart([MarshalAs(UnmanagedType.LPWStr)] string commandLineArgs, int Flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int UnregisterApplicationRestart();

        [Flags]
        private enum ApplicationRestartFlags
        {
            RESTART_ALWAYS = 0,
            RESTART_NO_CRASH = 1,
            RESTART_NO_HANG = 2,
            RESTART_NO_PATCH = 4,
            RESTART_NO_REBOOT = 8,
        }

        // register restart after system reboot/update
        public static void RegisterForRestart(this ShadowsocksContext context, bool register)
        {
            // requested register and not autostartup
            if (register && !context.autoStartup.Check())
            {
                // escape command line parameter
                string[] args = context.runArgs.ToList()
                    .Select(p => p.Replace("\"", "\\\""))                   // escape " to \"
                    .Select(p => p.IndexOf(" ") >= 0 ? "\"" + p + "\"" : p) // encapsule with "
                    .ToArray();
                string cmdline = string.Join(" ", args);
                // first parameter is process command line parameter
                // needn't include the name of the executable in the command line
                RegisterApplicationRestart(cmdline, (int)(ApplicationRestartFlags.RESTART_NO_CRASH | ApplicationRestartFlags.RESTART_NO_HANG));
                _logger.Debug($"Register restart after system reboot, command line:{cmdline}");
            }
            // requested unregister, which has no side effect
            else if (!register)
            {
                UnregisterApplicationRestart();
                _logger.Debug("Unregister restart after system reboot");
            }
        }
    }
}