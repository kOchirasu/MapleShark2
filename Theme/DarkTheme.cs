using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.Theme {
    public class DarkTheme : IMapleSharkTheme {
        public ThemeBase DockSuiteTheme { get; } = new VS2015DarkTheme();
        public Color ThemeColor => DockSuiteTheme.ColorPalette.MainWindowActive.Background;

        public Color BackColor { get; } = Color.FromArgb(255, 25, 25, 25);
        public Color ForeColor { get; } = Color.FromArgb(255, 220, 220, 220);
        public Color ControlBackColor { get; } = Color.FromArgb(255, 60, 60, 60);
        public Color SelectionBackColor { get; } = Color.CornflowerBlue;
        public Color SelectionForeColor => ForeColor;

        public Color HighlightColor { get; } = Color.FromArgb(255, 40, 60, 80);
        public Color BorderColor { get; } = Color.FromArgb(255, 75, 75, 75);
    }
}