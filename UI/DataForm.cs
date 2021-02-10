using System;
using System.Windows.Forms;
using Be.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.UI.Control;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public partial class DataForm : DockContent {
        public DataForm() {
            InitializeComponent();
        }

        public MainForm MainForm => ParentForm as MainForm;

        public HexBox HexBox => mHex;

        private void mHex_SelectionLengthChanged(object pSender, EventArgs pArgs) {
            if (mHex.SelectionLength == 0) MainForm.PropertyForm.Properties.SelectedObject = null;
            else {
                ArraySegment<byte> buffer = default;
                StructureNode match = null;
                foreach (TreeNode node in MainForm.StructureForm.Tree.Nodes) {
                    var realNode = (StructureNode) node;
                    buffer = realNode.Data;
                    if (mHex.SelectionStart == realNode.Data.Offset && mHex.SelectionLength == realNode.Data.Count) {
                        match = realNode;
                        break;
                    }
                }

                MainForm.StructureForm.Tree.SelectedNode = match;
                if (buffer.Count > 0)
                    MainForm.PropertyForm.Properties.SelectedObject = new StructureSegment(buffer, MainForm.Locale);
                else MainForm.PropertyForm.Properties.SelectedObject = null;
            }
        }

        private void mHex_KeyDown(object pSender, KeyEventArgs pArgs) {
            MainForm.CopyPacketHex(pArgs);
        }
    }
}