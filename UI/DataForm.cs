using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Be.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.UI.Control;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public partial class DataForm : DockContent {
        private MainForm MainForm => ParentForm as MainForm;

        public long SelectionStart => mHex.SelectionStart;
        public long SelectionLength => mHex.SelectionLength;

        public DataForm() {
            InitializeComponent();
        }

        public byte[] GetHexBoxBytes() {
            var provider = (DynamicByteProvider) mHex.ByteProvider;
            return provider.Bytes.ToArray();
        }

        public byte[] GetHexBoxSelectedBytes() {
            var provider = (DynamicByteProvider) mHex.ByteProvider;
            return provider.Bytes.GetRange((int) mHex.SelectionStart, (int) mHex.SelectionLength).ToArray();
        }

        public void SelectHexBoxRange(long start, long length) {
            mHex.SelectionStart = start;
            mHex.SelectionLength = length;
            mHex.ScrollByteIntoView();
        }

        public void SetHexBoxBytes(byte[] bytes) {
            mHex.ByteProvider = new DynamicByteProvider(bytes);
        }

        public void ClearHexBox() {
            mHex.ByteProvider = null;
        }

        public void ClearHexBoxSelection() {
            mHex.SelectionLength = 0;
        }

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