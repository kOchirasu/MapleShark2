using System.Windows.Forms;
using Be.Windows.Forms;
using MapleShark2.UI.Control;

namespace MapleShark2.Theme {
    public static class ThemeApplier {
        public static void ApplyTheme(IMapleSharkTheme theme, Control.ControlCollection container) {
            foreach (Control component in container) {
                if (component.Controls.Count > 0) {
                    ApplyTheme(theme, component.Controls);
                }

                if (component is Button button) {
                    button.BackColor = theme.ControlBackColor;
                    button.ForeColor = theme.ForeColor;
                    button.FlatAppearance.BorderColor = theme.BorderColor;
                } else if (component is TextBox textBox) {
                    textBox.BackColor = theme.ControlBackColor;
                    textBox.ForeColor = theme.ForeColor;
                } else if (component is ComboBox comboBox) {
                    comboBox.BackColor = theme.ControlBackColor;
                    comboBox.ForeColor = theme.ForeColor;
                } else if (component is PropertyGrid propertyGrid) {
                    propertyGrid.BackColor = theme.BackColor;
                    propertyGrid.ForeColor = theme.ForeColor;
                    propertyGrid.LineColor = theme.ThemeColor;
                } else if (component is HexBox hexBox) {
                    hexBox.BackColor = hexBox.ReadOnly ? theme.BackColor : theme.ControlBackColor;
                    hexBox.ForeColor = theme.ForeColor;
                    hexBox.SelectionBackColor = theme.SelectionBackColor;
                } else if (component is PacketListView packetList) {
                    packetList.BackColor = theme.BackColor;
                    packetList.ForeColor = theme.ForeColor;
                    packetList.DividerColor = theme.BorderColor;
                    packetList.HighlightColor = theme.HighlightColor;
                } else {
                    component.BackColor = theme.BackColor;
                    component.ForeColor = theme.ForeColor;
                }
            }
        }
    }
}