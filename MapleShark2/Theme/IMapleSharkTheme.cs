using System.Drawing;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.Theme {
    public interface IMapleSharkTheme {
        ThemeBase DockSuiteTheme { get; }
        Color ThemeColor { get; }

        Color BackColor { get; }
        Color ForeColor { get; }
        Color ControlBackColor { get; }
        Color SelectionBackColor { get; }
        Color SelectionForeColor { get; }
        Color HighlightColor { get; }
        Color BorderColor { get; }
    }
}