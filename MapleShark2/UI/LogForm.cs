using MapleShark2.Theme;
using MapleShark2.Tools;
using NLog.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public partial class LogForm : DockContent {
        private MainForm MainForm => ParentForm as MainForm;

        public LogForm() {
            InitializeComponent();

            RichTextBoxTarget.ReInitializeAllTextboxes(this);
        }

        public void ApplyTheme() {
            BackColor = Config.Instance.Theme.DockSuiteTheme.ColorPalette.MainWindowActive.Background;
            ThemeApplier.ApplyTheme(Config.Instance.Theme, Controls);
        }
    }
}