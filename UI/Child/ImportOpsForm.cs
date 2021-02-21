using System;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Theme;
using MapleShark2.Tools;

namespace MapleShark2.UI.Child {
    public sealed partial class ImportOpsForm : Form {
        public ImportOpsForm() {
            InitializeComponent();

            ThemeApplier.ApplyTheme(Config.Instance.Theme, this);
        }

        private void button1_Click(object sender, EventArgs e) {
            if (ofdPropFile.ShowDialog() != DialogResult.OK) {
                return;
            }

            txtPropFilePath.Text = ofdPropFile.FileName;

            // Automatically check the boxes
            string fileName = Path.GetFileName(ofdPropFile.FileName);
            if (fileName.StartsWith("send", StringComparison.OrdinalIgnoreCase)) {
                chkIsSend.Checked = true;
            } else if (fileName.StartsWith("recv", StringComparison.OrdinalIgnoreCase)) {
                chkIsSend.Checked = false;
            }
        }

        private void btnImport_Click(object sender, EventArgs e) {
            if (!File.Exists(txtPropFilePath.Text)) {
                string message = $"File {txtPropFilePath.Text} does not exist!";
                MessageBox.Show(message, "MapleShark2", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            txtLog.Text = "";
            byte locale = Convert.ToByte(nudLocale.Value);
            ushort version = Convert.ToUInt16(nudVersion.Value);

            string[] opcodes = File.ReadAllLines(txtPropFilePath.Text);
            foreach (string opcode in opcodes) {
                string val = opcode;

                // Strip comments
                if (val.Contains("#")) {
                    val = val.Remove(val.IndexOf('#'));
                }

                val = val.Trim();
                if (string.IsNullOrWhiteSpace(val)) continue;

                string[] keyValue = val.Split('=');
                if (keyValue.Length != 2) continue;

                string name = keyValue[0].Trim();

                string headerStr = keyValue[1].Trim();
                ushort header = headerStr.StartsWith("0x")
                    ? ushort.Parse(headerStr.Substring(2), NumberStyles.HexNumber)
                    : ushort.Parse(headerStr, NumberStyles.Integer);

                AddOpcode(version, locale, !chkIsSend.Checked, header, name);
            }

            Config.Instance.Save();
        }

        private void AddOpcode(ushort pBuild, byte pLocale, bool pOutbound, ushort pOpcode, string pName) {
            Definition def = Config.Instance.GetDefinition(pBuild, pLocale, pOutbound, pOpcode);
            if (def == null) {
                def = new Definition();
                txtLog.AppendText($"Adding opcode {pName}: 0x{pOpcode:X4}");
            } else {
                txtLog.AppendText($"Replacing opcode {def.Name} 0x{pOpcode:X4} for {pName}");
            }

            txtLog.AppendText(Environment.NewLine);

            def.Opcode = pOpcode;
            def.Outbound = pOutbound;
            def.Name = pName;
            def.Ignore = false;

            DefinitionsContainer.Instance.SaveDefinition(pLocale, pBuild, def);
        }
    }
}