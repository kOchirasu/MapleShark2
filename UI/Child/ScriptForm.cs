using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Theme;
using MapleShark2.Tools;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI.Child {
    public sealed partial class ScriptForm : DockContent {
        private const string SYNTAX_DEF = "MapleShark2.Resources.ScriptSyntax.txt";

        private readonly string path;
        internal MaplePacket Packet { get; }

        public ScriptForm(string pPath, MaplePacket packet) {
            path = pPath;
            Packet = packet;

            InitializeComponent();
            ThemeApplier.ApplyTheme(Config.Instance.Theme, this);

            Text = packet != null
                ? $"Script 0x{packet.Opcode:X4}, {(packet.Outbound ? "Outbound" : "Inbound")}"
                : "Common Script";
        }

        private void ScriptForm_Load(object pSender, EventArgs pArgs) {
            mScriptEditor.Document.SetSyntaxFromEmbeddedResource(Assembly.GetExecutingAssembly(), SYNTAX_DEF);
            if (File.Exists(path)) {
                mScriptEditor.Open(path);
            }
        }

        private void mScriptEditor_TextChanged(object pSender, EventArgs pArgs) {
            mSaveButton.Enabled = true;
        }

        private void mSaveButton_Click(object pSender, EventArgs pArgs) {
            if (mScriptEditor.Document.Text.Length == 0) {
                File.Delete(path);
            } else {
                mScriptEditor.Save(path);
            }

            Close();
        }

        private void mImportButton_Click(object sender, EventArgs e) {
            if (FileImporter.ShowDialog() != DialogResult.OK) return;
            if (!File.Exists(FileImporter.FileName)) return;

            if (mScriptEditor.Document.Text.Length > 0) {
                const string message = "Are you sure you want to open this file? "
                                       + "The current script will be replaced with the file you selected.";
                DialogResult result =
                    MessageBox.Show(message, "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.No) return;
            }

            mScriptEditor.Open(FileImporter.FileName);
        }
    }
}