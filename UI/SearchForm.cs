using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Be.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Tools;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public partial class SearchForm : DockContent {
        public SearchForm() {
            InitializeComponent();
            mSequenceHex.ByteProvider = new DynamicByteProvider(new List<byte>());
            ((DynamicByteProvider) mSequenceHex.ByteProvider).Changed += mSequenceHex_ByteProviderChanged;
        }

        public MainForm MainForm => ParentForm as MainForm;
        public ComboBox ComboBox => mOpcodeCombo;
        public HexBox HexBox => mSequenceHex;

        public void RefreshOpcodes(bool pReselect) {
            if (!(DockPanel.ActiveDocument is SessionForm session)) {
                mOpcodeCombo.Items.Clear();
                return;
            }

            Opcode selected =
                pReselect
                && mOpcodeCombo.SelectedIndex >= 0
                && session.Opcodes.Count > mOpcodeCombo.SelectedIndex
                    ? session.Opcodes[mOpcodeCombo.SelectedIndex]
                    : null;
            mOpcodeCombo.Items.Clear();
            session.UpdateOpcodeList();

            foreach (Opcode op in session.Opcodes) {
                Definition definition =
                    Config.Instance.GetDefinition(session.Build, session.Locale, op.Outbound, op.Header);
                int addedIndex = mOpcodeCombo.Items.Add(
                    $"{(op.Outbound ? "OUT " : "IN  ")} 0x{op.Header:X4} {definition?.Name ?? ""}");

                if (selected != null && selected.Outbound == op.Outbound && selected.Header == op.Header) {
                    mOpcodeCombo.SelectedIndex = addedIndex;
                }
            }
        }

        private void mOpcodeCombo_SelectedIndexChanged(object pSender, EventArgs pArgs) {
            mNextOpcodeButton.Enabled = mPrevOpcodeButton.Enabled = mOpcodeCombo.SelectedIndex >= 0;
        }

        private void mNextOpcodeButton_Click(object pSender, EventArgs pArgs) {
            if (!(DockPanel.ActiveDocument is SessionForm session) || mOpcodeCombo.SelectedIndex == -1) {
                return;
            }

            Opcode search = session.Opcodes[mOpcodeCombo.SelectedIndex];
            int initialIndex = session.ListView.SelectedIndices.Count == 0
                ? 0
                : session.ListView.SelectedIndices[0] + 1;
            for (int index = initialIndex; index < session.ListView.VirtualListSize; ++index) {
                MaplePacket packetItem = session.FilteredPackets[index];
                if (packetItem.Outbound == search.Outbound && packetItem.Opcode == search.Header) {
                    session.ListView.Select(index);
                    session.ListView.Focus();
                    return;
                }
            }

            MessageBox.Show("No further packets found with the selected opcode.", "End Of Search", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            session.ListView.Focus();
        }

        private void mSequenceHex_ByteProviderChanged(object pSender, EventArgs pArgs) {
            mNextSequenceButton.Enabled /* = mPrevSequenceButton.Enabled*/ = mSequenceHex.ByteProvider.Length > 0;
        }

        private void mSequenceHex_KeyPress(object pSender, KeyPressEventArgs pArgs) {
            if (pArgs.KeyChar == (char) Keys.Enter) {
                pArgs.Handled = true;
                NextSequence();
            }
        }

        private void mNextSequenceButton_Click(object pSender, EventArgs pArgs) {
            NextSequence();
        }

        private void NextSequence() {
            if (!(DockPanel.ActiveDocument is SessionForm session)) {
                return;
            }

            int initialIndex = session.ListView.SelectedIndices.Count == 0 ? 0 : session.ListView.SelectedIndices[0];
            byte[] pattern = (mSequenceHex.ByteProvider as DynamicByteProvider)?.Bytes.ToArray();
            long startIndex = MainForm.DataForm.HexBox.SelectionLength > 0
                ? MainForm.DataForm.HexBox.SelectionStart
                : -1;
            for (int index = initialIndex; index < session.ListView.VirtualListSize; ++index) {
                MaplePacket packetItem = session.FilteredPackets[index];
                long searchIndex = startIndex + 1;
                bool found = false;
                while (pattern != null && searchIndex <= packetItem.Buffer.Length - pattern.Length) {
                    found = true;
                    for (int patternIndex = 0; found && patternIndex < pattern.Length; ++patternIndex)
                        found = packetItem.Buffer[searchIndex + patternIndex] == pattern[patternIndex];
                    if (found) break;
                    ++searchIndex;
                }

                if (found) {
                    session.ListView.Select(index);
                    MainForm.DataForm.HexBox.SelectionStart = searchIndex;
                    MainForm.DataForm.HexBox.SelectionLength = pattern.Length;
                    MainForm.DataForm.HexBox.ScrollByteIntoView();
                    session.ListView.Focus();
                    return;
                }

                startIndex = -1;
            }

            MessageBox.Show("No further sequences found.", "End Of Search", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            session.ListView.Focus();
        }

        private void mPrevOpcodeButton_Click(object sender, EventArgs e) {
            if (!(DockPanel.ActiveDocument is SessionForm session) || mOpcodeCombo.SelectedIndex == -1) {
                return;
            }

            Opcode search = session.Opcodes[mOpcodeCombo.SelectedIndex];
            int initialIndex = session.ListView.SelectedIndices.Count == 0 ? 0 : session.ListView.SelectedIndices[0];
            for (int index = initialIndex - 1; index > 0; --index) {
                MaplePacket packetItem = session.FilteredPackets[index];
                if (packetItem.Outbound == search.Outbound && packetItem.Opcode == search.Header) {
                    session.ListView.Select(index);
                    session.ListView.Focus();
                    return;
                }
            }

            MessageBox.Show("No further packets found with the selected opcode.", "End Of Search", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            session.ListView.Focus();
        }

        private void SearchForm_Load(object sender, EventArgs e) { }
    }
}