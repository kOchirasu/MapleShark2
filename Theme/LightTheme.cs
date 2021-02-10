using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.Theme {
    public class LightTheme : IMapleSharkTheme {
        public ThemeBase DockSuiteTheme { get; } = new VS2015LightTheme();
        public Color ThemeColor => DockSuiteTheme.ColorPalette.MainWindowActive.Background;

        public Color BackColor { get; } = SystemColors.Window;
        public Color ForeColor { get; } = SystemColors.WindowText;
        public Color ControlBackColor { get; } = SystemColors.Control;
        public Color SelectionBackColor { get; } = Color.CornflowerBlue;
        public Color SelectionForeColor => ForeColor;

        public Color HighlightColor { get; } = Color.AliceBlue;
        public Color BorderColor { get; } = SystemColors.ActiveBorder;
    }
}