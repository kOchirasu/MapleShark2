using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public partial class PropertyForm : DockContent {
        public PropertyForm() {
            InitializeComponent();
        }

        public MainForm MainForm => ParentForm as MainForm;
        public PropertyGrid Properties => mProperties;
    }
}