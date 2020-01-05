using System;
using System.Drawing;

namespace Shadowsocks.Std.Model
{
    [Serializable]
    public class LogViewerConfig
    {
        public bool topMost;
        public bool wrapText;
        public bool toolbarShown;

        public string fontName = "Consolas";
        public float fontSize = 8F;

        public Color BackgroundColor { get; set; } = Color.Black;

        public Color TextColor { get; set; } = Color.White;

        public LogViewerConfig()
        {
            topMost = false;
            wrapText = false;
            toolbarShown = false;
        }
    }
}