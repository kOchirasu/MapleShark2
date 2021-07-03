using System;
using System.Windows.Forms;
using MapleShark2.Theme;
using MapleShark2.Tools;

namespace MapleShark2.UI.Child {
    public sealed partial class SessionInfoForm : Form {
        public SessionInfoForm() {
            InitializeComponent();
            ThemeApplier.ApplyTheme(Config.Instance.Theme, this);
        }

        private void closeButton_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void SessionInformation_Load(object sender, EventArgs e) { }
    }
}