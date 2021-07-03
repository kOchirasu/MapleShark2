using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Theme;
using MapleShark2.Tools;
using ScintillaNET;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI.Child {
    public sealed partial class ScriptForm : DockContent {
        private readonly string path;
        private int maxLineNumberCharLength;
        internal MaplePacket Packet { get; }

        public ScriptForm(string path, MaplePacket packet) {
            this.path = path;
            Packet = packet;

            InitializeComponent();
            ThemeApplier.ApplyTheme(Config.Instance.Theme, this);

            Text = packet != null
                ? $"Script 0x{packet.Opcode:X4}, {(packet.Outbound ? "Outbound" : "Inbound")}"
                : "Common Script";
        }

        private void ScriptForm_Load(object sender, EventArgs args) {
            if (File.Exists(path)) {
                mScriptEditor.Text = File.ReadAllText(path);
            }
        }

        private void mScriptEditor_TextChanged(object sender, EventArgs args) {
            mSaveButton.Enabled = true;

            // Did the number of characters in the line number display change?
            // i.e. nnn VS nn, or nnnn VS nn, etc...
            int newMaxLineNumberCharLength = mScriptEditor.Lines.Count.ToString().Length;
            if (newMaxLineNumberCharLength == maxLineNumberCharLength) {
                return;
            }

            // Calculate the width required to display the last line number
            // and include some padding for good measure.
            const int padding = 2;
            mScriptEditor.Margins[0].Width = mScriptEditor.TextWidth(Style.LineNumber,
                new string('9', newMaxLineNumberCharLength + 1)) + padding;
            maxLineNumberCharLength = newMaxLineNumberCharLength;
        }

        private void mScriptEditor_InsertCheck(object sender, InsertCheckEventArgs e) {
            if (!e.Text.EndsWith("\r") && !e.Text.EndsWith("\n")) {
                return;
            }

            int curLine = mScriptEditor.LineFromPosition(e.Position);
            string curLineText = mScriptEditor.Lines[curLine].Text;

            Match indent = Regex.Match(curLineText, @"^\s*");
            e.Text += indent.Value; // Add indent following "\r\n"
        }

        private void mSaveButton_Click(object sender, EventArgs args) {
            if (string.IsNullOrWhiteSpace(mScriptEditor.Text)) {
                File.Delete(path);
            } else {
                File.WriteAllText(path, mScriptEditor.Text);
            }

            Close();
        }

        private void mImportButton_Click(object sender, EventArgs e) {
            if (FileImporter.ShowDialog() != DialogResult.OK) return;
            if (!File.Exists(FileImporter.FileName)) return;

            if (string.IsNullOrWhiteSpace(mScriptEditor.Text)) {
                const string message = "Are you sure you want to open this file? "
                                       + "The current script will be replaced with the file you selected.";
                DialogResult result =
                    MessageBox.Show(message, "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No) return;
            }

            mScriptEditor.Text = File.ReadAllText(FileImporter.FileName);
        }
    }
}