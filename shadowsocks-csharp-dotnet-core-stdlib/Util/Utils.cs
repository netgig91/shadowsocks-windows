using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;

using NLog;

using Shadowsocks.Std.Model;
using Shadowsocks.Std.Sys;
using Shadowsocks.Std.Util.Resource;
using Shadowsocks.Std.Util.SystemProxy;

using static Shadowsocks.Std.Util.DelegateUtil;

namespace Shadowsocks.Std.Util
{
    public struct BandwidthScaleInfo
    {
        public float value;
        public long unit;
        public string unitName;

        public BandwidthScaleInfo(float value, string unitName, long unit)
        {
            this.value = value;
            this.unitName = unitName;
            this.unit = unit;
        }
    }

    public interface IApplication
    {
        public int InfoMessageBox(string message, string caption = "Shadowsocks");

        public int WarnMessageBox(string message, string caption);

        public int ErrorMessageBox(string message, string caption);

        public string StartupPath();

        public string ExecutablePath();
    }

    public static class Utils
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static string _tempPath = null;

        public static List<IGetResources> Resources
        {
            get; internal set;
        }

        public static IApplication Application
        {
            get; internal set;
        }

        public static LoadLibrary loadLibrary
        {
            get => loadLibrary;
            set
            {
                if (loadLibrary == null)
                {
                    _logger.Debug("Delegate LoadLibrary...");
                    loadLibrary = value;
                }
            }
        }

        public static SetProcessWorkingSetSize setProcessWorkingSetSize
        {
            get => setProcessWorkingSetSize;
            set
            {
                if (setProcessWorkingSetSize == null)
                {
                    _logger.Debug("Delegate SetProcessWorkingSetSize...");
                    setProcessWorkingSetSize = value;
                }
            }
        }

        #region Get Temp Path

        // return path to store temporary files
        public static string GetTempPath()
        {
            if (_tempPath == null)
            {
                bool isPortableMode = Configuration.Load().portableMode;
                try
                {
                    if (isPortableMode)
                    {
                        _tempPath = Directory.CreateDirectory("ss_win_temp").FullName;
                        // don't use "/", it will fail when we call explorer /select xxx/ss_win_temp\xxx.log
                    }
                    else
                    {
                        _tempPath = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"Shadowsocks\\ss_win_temp_{Application.ExecutablePath().GetHashCode()}")).FullName;
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e);

                    ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                }
            }

            return _tempPath;
        }

        // return a full path with filename combined which pointed to the temporary directory
        public static string GetTempPath(string filename) => GetTempPath(filename, null);

        public static string GetTempPath(string filename, IApplication application)
        {
            if (application != null)
            {
                Application = application;
            }

            return Path.Combine(GetTempPath(), filename);
        }

        #endregion Get Temp Path

        #region Log

        public static void Debug(this Logger logger, EndPoint local, EndPoint remote, int len, string header = null, string tailer = null)
        {
            if (header == null && tailer == null)
                logger.Debug($"{local} => {remote} (size={len})");
            else if (header == null && tailer != null)
                logger.Debug($"{local} => {remote} (size={len}), {tailer}");
            else if (header != null && tailer == null)
                logger.Debug($"{header}: {local} => {remote} (size={len})");
            else
                logger.Debug($"{header}: {local} => {remote} (size={len}), {tailer}");
        }

        public static void Debug(this Logger logger, Socket sock, int len, string header = null, string tailer = null)
        {
            logger.Debug(sock.LocalEndPoint, sock.RemoteEndPoint, len, header, tailer);
        }

        public static void Dump(this Logger logger, string tag, byte[] arr, int length)
        {
            var sb = new StringBuilder($"{Environment.NewLine}{tag}: ");
            for (int i = 0; i < length - 1; i++)
            {
                sb.Append($"0x{arr[i]:X2}, ");
            }
            sb.Append($"0x{arr[length - 1]:X2}");
            sb.Append(Environment.NewLine);
            logger.Debug(sb.ToString());
        }

        public static void LogUsefulException(this Logger logger, Exception e)
        {
            // just log useful exceptions, not all of them
            if (e is SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionAborted)
                {
                    // closed by browser when sending
                    // normally happens when download is canceled or a tab is closed before page is loaded
                }
                else if (se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // received rst
                }
                else if (se.SocketErrorCode == SocketError.NotConnected)
                {
                    // The application tried to send or receive data, and the System.Net.Sockets.Socket is not connected.
                }
                else if (se.SocketErrorCode == SocketError.HostUnreachable)
                {
                    // There is no network route to the specified host.
                }
                else if (se.SocketErrorCode == SocketError.TimedOut)
                {
                    // The connection attempt timed out, or the connected host has failed to respond.
                }
                else
                {
                    logger.Info(e);
                }
            }
            else if (e is ObjectDisposedException)
            {
            }
            else if (e is Win32Exception winex)
            {
                // Win32Exception (0x80004005): A 32 bit processes cannot access modules of a 64 bit process.
                if ((uint)winex.ErrorCode != 0x80004005)
                {
                    logger.Info(e);
                }
            }
            else if (e is ProxyException pe)
            {
                switch (pe.Type)
                {
                    case ProxyExceptionType.FailToRun:
                    case ProxyExceptionType.QueryReturnMalformed:
                    case ProxyExceptionType.SysproxyExitError:
                        logger.Error($"sysproxy - {pe.Type.ToString()}:{pe.Message}");
                        break;

                    case ProxyExceptionType.QueryReturnEmpty:
                    case ProxyExceptionType.Unspecific:
                        logger.Error($"sysproxy - {pe.Type.ToString()}");
                        break;
                }
            }
            else
            {
                logger.Info(e);
            }
        }

        #endregion Log

        #region Bandwidth

        public static string FormatBandwidth(long n)
        {
            var result = GetBandwidthScale(n);
            return $"{result.value:0.##}{result.unitName}";
        }

        public static string FormatBytes(long bytes)
        {
            const long K = 1024L;
            const long M = K * 1024L;
            const long G = M * 1024L;
            const long T = G * 1024L;
            const long P = T * 1024L;
            const long E = P * 1024L;

            if (bytes >= P * 990)
                return (bytes / (double)E).ToString("F5") + "EiB";
            if (bytes >= T * 990)
                return (bytes / (double)P).ToString("F5") + "PiB";
            if (bytes >= G * 990)
                return (bytes / (double)T).ToString("F5") + "TiB";
            if (bytes >= M * 990)
            {
                return (bytes / (double)G).ToString("F4") + "GiB";
            }
            if (bytes >= M * 100)
            {
                return (bytes / (double)M).ToString("F1") + "MiB";
            }
            if (bytes >= M * 10)
            {
                return (bytes / (double)M).ToString("F2") + "MiB";
            }
            if (bytes >= K * 990)
            {
                return (bytes / (double)M).ToString("F3") + "MiB";
            }
            if (bytes > K * 2)
            {
                return (bytes / (double)K).ToString("F1") + "KiB";
            }
            return bytes.ToString() + "B";
        }

        /// <summary>
        /// Return scaled bandwidth
        /// </summary>
        /// <param name="n">Raw bandwidth</param>
        /// <returns>
        /// The BandwidthScaleInfo struct
        /// </returns>
        public static BandwidthScaleInfo GetBandwidthScale(long n)
        {
            long scale = 1;
            float f = n;
            string unit = "B";
            if (f > 1024)
            {
                f /= 1024;
                scale <<= 10;
                unit = "KiB";
            }
            if (f > 1024)
            {
                f /= 1024;
                scale <<= 10;
                unit = "MiB";
            }
            if (f > 1024)
            {
                f /= 1024;
                scale <<= 10;
                unit = "GiB";
            }
            if (f > 1024)
            {
                f /= 1024;
                scale <<= 10;
                unit = "TiB";
            }
            return new BandwidthScaleInfo(f, unit, scale);
        }

        #endregion Bandwidth

        #region Gzip

        public static string UnGzip(byte[] bufBytes)
        {
            byte[] buffer = new byte[1024];
            int n;
            using MemoryStream ms = new MemoryStream();
            using (GZipStream input = new GZipStream(new MemoryStream(bufBytes), CompressionMode.Decompress, false))
            {
                while ((n = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, n);
                }
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        #endregion Gzip

        #region System / Memory

        public static void ReleaseMemory(bool removePages)
        {
            // release any unused pages
            // making the numbers look good in task manager
            // this is totally nonsense in programming
            // but good for those users who care
            // making them happier with their everyday life
            // which is part of user experience
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
            if (removePages)
            {
                // as some users have pointed out
                // removing pages from working set will cause some IO
                // which lowered user experience for another group of users
                //
                // so we do 2 more things here to satisfy them:
                // 1. only remove pages once when configuration is changed
                // 2. add more comments here to tell users that calling
                //    this function will not be more frequent than
                //    IM apps writing chat logs, or web browsers writing cache files
                //    if they're so concerned about their disk, they should
                //    uninstall all IM apps and web browsers
                //
                // please open an issue if you're worried about anything else in your computer
                // no matter it's GPU performance, monitor contrast, audio fidelity
                // or anything else in the task manager
                // we'll do as much as we can to help you
                //
                // just kidding
                setProcessWorkingSetSize(Process.GetCurrentProcess().Handle,
                                         (UIntPtr)0xFFFFFFFF,
                                         (UIntPtr)0xFFFFFFFF);
            }
        }

        #endregion System / Memory

        #region Exec / Lib

        private static readonly List<string> loaded = new List<string>();

        #region lib

        public const string libsscrypto = "libsscrypto";

        #endregion lib

        #region exec

        public const string ss_privoxy = "ss_privoxy";

        public const string sysproxy = "sysproxy";

        #endregion exec

        public static bool GetAndUncompressExec(string name)
        {
            if (!loaded.Contains(name))
            {
                var newName = name.Clone().ToString();

                for (int i = 0; i < Resources.Count; i++)
                {
                    var bytes = Resources[i].GetExec(ref newName);

                    if (bytes != null)
                    {
                        var libPath = GetTempPath(newName);

                        FileManager.UncompressFile(libPath, bytes);

                        loaded.Add(name);

                        return true;
                    }
                }
            }

            // TODO I18N
            throw new IOException("");
        }

        public static bool GetAndUncompressLib(string name)
        {
            if (!loaded.Contains(name))
            {
                var newName = name.Clone().ToString();

                for (int i = 0; i < Resources.Count; i++)
                {
                    var bytes = Resources[i].GetLib(ref newName);

                    if (bytes != null)
                    {
                        var libPath = GetTempPath(newName);

                        FileManager.UncompressFile(libPath, bytes);

                        loadLibrary(libPath);

                        loaded.Add(name);

                        return true;
                    }
                }
            }

            // TODO I18N
            throw new IOException("");
        }

        #endregion Exec / Lib

        public static bool IsWinVistaOrHigher() => Environment.OSVersion.Version.Major > 5;
    }
}