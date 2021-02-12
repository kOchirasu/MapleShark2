using System;
using System.Collections.Generic;
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
            if (ofdPropFile.ShowDialog() == DialogResult.OK) {
                txtPropFile.Text = ofdPropFile.FileName;
            }
        }

        private void btnImport_Click(object sender, EventArgs e) {
            if (!File.Exists(txtPropFile.Text)) {
                string message = $"File {txtPropFile.Text} does not exist!";
                MessageBox.Show(message, "MapleShark2", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            txtLog.Text = "";
            byte locale = Convert.ToByte(nudLocale.Value);
            ushort version = Convert.ToUInt16(nudVersion.Value);

            string[] opcodes = File.ReadAllLines(txtPropFile.Text);
            List<ushort> loadedOps = new List<ushort>();
            foreach (string opcode in opcodes) {
                string val = opcode;
                if (val.Contains("#"))
                    val = val.Remove(val.IndexOf('#'));

                val = val.Trim();
                if (val == "") continue;

                string[] splitted = val.Split('=');
                if (splitted.Length != 2) continue;

                string name = splitted[0].Trim();
                ushort header = 0;

                string headerval = splitted[1].Trim();
                if (headerval.StartsWith("0x"))
                    header = ushort.Parse(headerval.Substring(2), System.Globalization.NumberStyles.HexNumber);
                else
                    header = ushort.Parse(headerval, System.Globalization.NumberStyles.Integer);

                AddOpcode(version, locale, !chkIsSend.Checked, header, name);
            }

            Config.Instance.Save();
        }

        private void AddOpcode(ushort pBuild, byte pLocale, bool pOutbound, ushort pOpcode, string pName) {
            Definition def = Config.Instance.GetDefinition(pBuild, pLocale, pOutbound, pOpcode);
            if (def == null) {
                def = new Definition();
                txtLog.AppendText($"Adding opcode {pName}: 0x{pOpcode:X4}\n");
            } else {
                txtLog.AppendText($"Replacing opcode {def.Name} 0x{pOpcode:X4} for {pName}\n");
            }

            def.Build = pBuild;
            def.Locale = pLocale;
            def.Opcode = pOpcode;
            def.Outbound = pOutbound;
            def.Name = pName;
            def.Ignore = false;

            DefinitionsContainer.Instance.SaveDefinition(def);
        }
    }
}