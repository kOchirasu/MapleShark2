using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI.Child {
    public sealed partial class OutputForm : DockContent {
        public OutputForm(string pTitle) {
            InitializeComponent();
            Text = pTitle;
        }

        public void Append(string pOutput) {
            mTextBox.AppendText(pOutput);
        }
    }
}