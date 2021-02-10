using System;
using System.Windows.Forms;

namespace MapleShark2.UI.Child {
    public sealed partial class SessionInfoForm : Form {
        public SessionInfoForm() {
            InitializeComponent();
        }

        private void closeButton_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void SessionInformation_Load(object sender, EventArgs e) { }
    }
}