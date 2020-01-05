using System;
using System.Collections.Generic;
using System.Net;

using NLog;

using Shadowsocks.Std.Controller;
using Shadowsocks.Std.Model;
using Shadowsocks.Std.Util.Resource;

namespace Shadowsocks.Std.Strategy
{
    internal class HighAvailabilityStrategy : IStrategy
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        protected ServerStatus _currentServer;
        protected Dictionary<Server, ServerStatus> _serverStatus;
        private readonly ShadowsocksController _controller;
        private readonly Random _random;

        public class ServerStatus
        {
            // time interval between SYN and SYN+ACK
            public TimeSpan latency;

            public DateTime lastTimeDetectLatency;

            // last time anything received
            public DateTime lastRead;

            // last time anything sent
            public DateTime lastWrite;

            // connection refused or closed before anything received
            public DateTime lastFailure;

            public Server server;

            public double score;
        }

        public HighAvailabilityStrategy(ShadowsocksController controller)
        {
            _controller = controller;
            _random = new Random();
            _serverStatus = new Dictionary<Server, ServerStatus>();
        }

        public string Name
        {
            get { return I18N.GetString("High Availability"); }
        }

        public string ID
        {
            get { return "com.shadowsocks.strategy.ha"; }
        }

        public void ReloadServers()
        {
            // make a copy to avoid locking
            var newServerStatus = new Dictionary<Server, ServerStatus>(_serverStatus);

            foreach (var server in _controller.GetCurrentConfiguration().configs)
            {
                if (!newServerStatus.ContainsKey(server))
                {
                    var status = new ServerStatus
                    {
                        server = server,
                        lastFailure = DateTime.MinValue,
                        lastRead = DateTime.Now,
                        lastWrite = DateTime.Now,
                        latency = new TimeSpan(0, 0, 0, 0, 10),
                        lastTimeDetectLatency = DateTime.Now
                    };
                    newServerStatus[server] = status;
                }
                else
                {
                    // update settings for existing server
                    newServerStatus[server].server = server;
                }
            }
            _serverStatus = newServerStatus;

            ChooseNewServer();
        }

        public Server GetAServer(IStrategyCallerType type, IPEndPoint localIPEndPoint, EndPoint destEndPoint)
        {
            if (type == IStrategyCallerType.TCP)
            {
                ChooseNewServer();
            }
            if (_currentServer == null)
            {
                return null;
            }
            return _currentServer.server;
        }

        /**
         * once failed, try after 5 min
         * and (last write - last read) < 5s
         * and (now - last read) <  5s  // means not stuck
         * and latency < 200ms, try after 30s
         */

        public void ChooseNewServer()
        {
            List<ServerStatus> servers = new List<ServerStatus>(_serverStatus.Values);
            DateTime now = DateTime.Now;
            foreach (var status in servers)
            {
                // all of failure, latency, (lastread - lastwrite) normalized to 1000, then
                // 100 * failure - 2 * latency - 0.5 * (lastread - lastwrite)
                status.score =
                    100 * 1000 * Math.Min(5 * 60, (now - status.lastFailure).TotalSeconds)
                    - 2 * 5 * (Math.Min(2000, status.latency.TotalMilliseconds) / (1 + (now - status.lastTimeDetectLatency).TotalSeconds / 30 / 10) +
                    -0.5 * 200 * Math.Min(5, (status.lastRead - status.lastWrite).TotalSeconds));
                _logger.Debug(String.Format("server: {0} latency:{1} score: {2}", status.server.FriendlyName(), status.latency, status.score));
            }
            ServerStatus max = null;
            foreach (var status in servers)
            {
                if (max == null)
                {
                    max = status;
                }
                else
                {
                    if (status.score >= max.score)
                    {
                        max = status;
                    }
                }
            }
            if (max != null)
            {
                if (_currentServer == null || max.score - _currentServer.score > 200)
                {
                    _currentServer = max;
                    _logger.Info($"HA switching to server: {_currentServer.server.FriendlyName()}");
                }
            }
        }

        public void UpdateLatency(Server server, TimeSpan latency)
        {
            _logger.Debug($"latency: {server.FriendlyName()} {latency}");

            if (_serverStatus.TryGetValue(server, out ServerStatus status))
            {
                status.latency = latency;
                status.lastTimeDetectLatency = DateTime.Now;
            }
        }

        public void UpdateLastRead(Server server)
        {
            _logger.Debug($"last read: {server.FriendlyName()}");

            if (_serverStatus.TryGetValue(server, out ServerStatus status))
            {
                status.lastRead = DateTime.Now;
            }
        }

        public void UpdateLastWrite(Server server)
        {
            _logger.Debug($"last write: {server.FriendlyName()}");

            if (_serverStatus.TryGetValue(server, out ServerStatus status))
            {
                status.lastWrite = DateTime.Now;
            }
        }

        public void SetFailure(Server server)
        {
            _logger.Debug($"failure: {server.FriendlyName()}");

            if (_serverStatus.TryGetValue(server, out ServerStatus status))
            {
                status.lastFailure = DateTime.Now;
            }
        }
    }
}