using System.IO;

using NLog;

namespace Shadowsocks.Std.Util.SystemProxy
{
    public interface ISysproxy
    {
        enum RET_ERRORS : int
        {
            RET_NO_ERROR = 0,
            INVALID_FORMAT = 1,
            NO_PERMISSION = 2,
            SYSCALL_FAILED = 3,
            NO_MEMORY = 4,
            INVAILD_OPTION_COUNT = 5,
        };

        protected static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        protected const string _userWininetJson = "user-wininet.json";

        protected static readonly string[] _lanIP = {
            "<local>",
            "localhost",
            "127.*",
            "10.*",
            "172.16.*",
            "172.17.*",
            "172.18.*",
            "172.19.*",
            "172.20.*",
            "172.21.*",
            "172.22.*",
            "172.23.*",
            "172.24.*",
            "172.25.*",
            "172.26.*",
            "172.27.*",
            "172.28.*",
            "172.29.*",
            "172.30.*",
            "172.31.*",
            "192.168.*"
        };

        static ISysproxy()
        {
            try
            {
                Utils.GetAndUncompressExec(Utils.sysproxy);
            }
            catch (IOException e)
            {
                _logger.LogUsefulException(e);
            }
        }

        public void SetProxy(bool enable, bool global, string proxyServer, string pacURL);

        public void ResetProxy();
    }
}
