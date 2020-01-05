using System;

using NLog;

using Shadowsocks.Std.Model;
using Shadowsocks.Std.Service;
using Shadowsocks.Std.Util;
using Shadowsocks.Std.Util.Resource;
using Shadowsocks.Std.Util.SystemProxy;
using Shadowsocks.Std.Win.Util.SystemProxy;

namespace Shadowsocks.Std.Win.Sys
{
    public static class SystemProxy
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static string GetTimestamp(DateTime value) => value.ToString("yyyyMMddHHmmssfff");

        public static void Update(Configuration config, bool forceDisable, PACServer pacSrv, bool noRetry = false)
        {
            bool global = config.global;
            bool enabled = config.enabled;

            if (forceDisable)
            {
                enabled = false;
            }

            try
            {
                if (enabled)
                {
                    if (global)
                    {
                        Sysproxy.SetIEProxy(true, true, "localhost:" + config.localPort.ToString(), null);
                    }
                    else
                    {
                        string pacUrl;
                        if (config.useOnlinePac && !config.pacUrl.IsNullOrEmpty())
                        {
                            pacUrl = config.pacUrl;
                        }
                        else
                        {
                            pacUrl = pacSrv.PacUrl;
                        }
                        Sysproxy.SetIEProxy(true, false, null, pacUrl);
                    }
                }
                else
                {
                    Sysproxy.SetIEProxy(false, false, null, null);
                }
            }
            catch (ProxyException ex)
            {
                _logger.LogUsefulException(ex);
                if (ex.Type != ProxyExceptionType.Unspecific && !noRetry)
                {
                    var ret = Utils.Application.WarnMessageBox(I18N.GetString("Error occured when process proxy setting, do you want reset current setting and retry?"), I18N.GetString("Shadowsocks"));
                    if (ret == 1)
                    {
                        Sysproxy.ResetIEProxy();
                        Update(config, forceDisable, pacSrv, true);
                    }
                }
                else
                {
                    Utils.Application.ErrorMessageBox(I18N.GetString("Unrecoverable proxy setting error occured, see log for detail"), I18N.GetString("Shadowsocks"));
                }
            }
        }
    }
}