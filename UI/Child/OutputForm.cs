using MapleShark2.Theme;
using MapleShark2.Tools;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI.Child {
    public sealed partial class OutputForm : DockContent {
        public OutputForm(string pTitle) {
            InitializeComponent();
            ThemeApplier.ApplyTheme(Config.Instance.Theme, this);

            Text = pTitle;
        }

        public void Append(string pOutput) {
            mTextBox.AppendText(pOutput);
        }
    }
}