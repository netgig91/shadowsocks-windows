using System.Collections.Generic;

using Shadowsocks.Std.Controller;

namespace Shadowsocks.Std.Strategy
{
    internal class StrategyManager
    {
        private List<IStrategy> _strategies;

        public StrategyManager(ShadowsocksController controller)
        {
            _strategies = new List<IStrategy>
            {
                new BalancingStrategy(controller),
                new HighAvailabilityStrategy(controller),
                new StatisticsStrategy(controller)
            };
            // TODO: load DLL plugins
        }

        public IList<IStrategy> GetStrategies() => _strategies;
    }
}