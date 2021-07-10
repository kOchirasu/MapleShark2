using System.Collections.Generic;
using System.Collections.Immutable;
using System.Drawing;
using System.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Tools;
using Microsoft.Scripting.Utils;

namespace MapleShark2.UI.Control {
    public class PacketListView : ListView {
        public virtual IReadOnlyList<MaplePacket> FilteredPackets => Items.Select(item => (item as MaplePacketItem)?.Packet).ToImmutableList();
        public virtual MaplePacket Selected => SelectedItems.Count > 0 ? (SelectedItems[0] as MaplePacketItem)?.Packet : default;
        public virtual int Count => Items.Count;

        public Color DividerColor = DefaultBackColor;
        public Color HighlightColor = DefaultBackColor;

        public PacketListView() {
            DoubleBuffered = true;
            OwnerDraw = true;
            DrawColumnHeader += packetList_DrawColumnHeader;
            DrawItem += packetList_DrawItem;
            DrawSubItem += packetList_DrawSubItem;

            // Clear the selected item, this allows auto-scroll to resume
            KeyDown += (sender, e) => {
                if (e.KeyCode == Keys.Escape) {
                    SelectedIndices.Clear();
                }
            };
        }
        
        public new virtual void BeginUpdate() {
            base.BeginUpdate();
        }

        public new virtual void EndUpdate() {
            base.EndUpdate();
        }
        
        public virtual int AddPacket(MaplePacket packetItem) {
            Items.Add(CreateListItem(packetItem));
            return Items.Count - 1;
        }
        
        public virtual bool Select(int index) {
            if (index < 0 || index >= Items.Count) {
                return false;
            }

            SelectedIndices.Clear();
            SelectedIndices.Add(index);
            Items[index]?.EnsureVisible();
            return true;
        }

        public new virtual void Clear() {
            Items.Clear();
        }
        
        private void packetList_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e) {
            using (var backBrush = new SolidBrush(BackColor)) {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            var top = new Point(e.Bounds.Right - 1, e.Bounds.Top);
            var bottom = new Point(e.Bounds.Right - 1, e.Bounds.Bottom);
            using (var dividerPen = new Pen(DividerColor)) {
                e.Graphics.DrawLine(dividerPen, top, bottom);
            }

            TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, Rectangle.Inflate(e.Bounds, -5, -2), ForeColor,
                TextFormatFlags.WordEllipsis);
        }

        private void packetList_DrawItem(object sender, DrawListViewItemEventArgs e) {
            e.DrawDefault = true;
        }

        private void packetList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e) {
            e.DrawDefault = true;
        }
        
        protected MaplePacketItem CreateListItem(MaplePacket packet) {
            Definition definition = Config.Instance.GetDefinition(packet);
            string name = definition == null ? "" : definition.Name;

            var item = new MaplePacketItem(packet, name);
            if (item.Packet.Outbound) {
                item.BackColor = HighlightColor;
            }

            return item;
        }
    }
}
