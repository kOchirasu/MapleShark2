using System;
using System.Windows.Forms;
using Be.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Theme;
using MapleShark2.UI.Control;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public partial class DataForm : DockContent {
        private MainForm MainForm => ParentForm as MainForm;

        private byte[] hexBoxBytes;

        public long SelectionStart => mHex.SelectionStart;
        public long SelectionLength => mHex.SelectionLength;

        public DataForm() {
            InitializeComponent();
        }

        public new void Show(DockPanel panel) {
            base.Show(panel);
            BackColor = MainForm.Theme.DockSuiteTheme.ColorPalette.MainWindowActive.Background;
            ThemeApplier.ApplyTheme(MainForm.Theme, Controls);
        }

        public ArraySegment<byte> GetHexBoxSelectedBytes() {
            return hexBoxBytes == null
                ? new ArraySegment<byte>()
                : new ArraySegment<byte>(hexBoxBytes, (int) mHex.SelectionStart, (int) mHex.SelectionLength);
        }

        public void SelectHexBoxRange(long start, long length) {
            mHex.SelectionStart = start;
            mHex.SelectionLength = length;
            mHex.ScrollByteIntoView();
        }

        public void SetHexBoxBytes(byte[] bytes) {
            hexBoxBytes = bytes;
            mHex.ByteProvider = new DynamicByteProvider(hexBoxBytes);
        }

        public void ClearHexBox() {
            hexBoxBytes = null;
            mHex.ByteProvider = null;
        }

        public void ClearHexBoxSelection() {
            mHex.SelectionLength = 0;
        }

        private void mHex_SelectionLengthChanged(object pSender, EventArgs pArgs) {
            if (mHex.SelectionLength == 0) {
                MainForm.PropertyForm.Properties.SelectedObject = null;
            } else {
                StructureNode match = null;
                foreach (TreeNode node in MainForm.StructureForm.Tree.Nodes) {
                    var realNode = (StructureNode) node;
                    if (mHex.SelectionStart == realNode.Data.Offset && mHex.SelectionLength == realNode.Data.Count) {
                        match = realNode;
                        break;
                    }
                }

                MainForm.StructureForm.Tree.SelectedNode = match;
                MainForm.PropertyForm.Properties.SelectedObject =
                    new StructureSegment(GetHexBoxSelectedBytes(), MainForm.Locale);
            }
        }

        private void mHex_KeyDown(object pSender, KeyEventArgs pArgs) {
            MainForm.CopyPacketHex(pArgs);
        }
    }
}