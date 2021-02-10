using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Tools;

namespace MapleShark2.UI.Control {
    public sealed class PacketListView : ListView {
        public IReadOnlyList<MaplePacket> FilteredPackets => mFilteredPackets;
        public MaplePacket Selected =>
            this.SelectedIndices.Count > 0 ? mFilteredPackets[this.SelectedIndices[0]] : default;

        public Color DividerColor = DefaultBackColor;
        public Color HighlightColor = DefaultBackColor;
        private readonly List<MaplePacket> mFilteredPackets;
        private bool updating;

        public PacketListView() {
            DoubleBuffered = true;
            mFilteredPackets = new List<MaplePacket>();
            VirtualMode = true;
            OwnerDraw = true;
            RetrieveVirtualItem += this.mPacketList_RetrieveVirtualItem;
            SearchForVirtualItem += this.mPacketList_SearchForVirtualItem;
            DrawColumnHeader += this.mPacketList_DrawColumnHeader;
            DrawItem += this.mPacketList_DrawItem;
            DrawSubItem += this.mPacketList_DrawSubItem;
        }

        public new int VirtualListSize {
            get => base.VirtualListSize;
            set {
                if (this.SelectedIndices.Count <= 0) {
                    base.VirtualListSize = value;
                } else {
                    this.SetVirtualListSizeWithoutRefresh(value);
                }
            }
        }

        public new void BeginUpdate() {
            updating = true;
            base.BeginUpdate();
        }

        public new void EndUpdate() {
            UpdateCount();
            updating = false;
            base.EndUpdate();
        }

        public void UpdateCount() {
            try {
                VirtualListSize = mFilteredPackets.Count;
            } catch { /* ignored */ }
        }

        public int AddPacket(MaplePacket packetItem) {
            mFilteredPackets.Add(packetItem);
            // No need to set VirtualListSize while updating, it will be set on completion
            if (!updating) {
                VirtualListSize = mFilteredPackets.Count;
            }

            return mFilteredPackets.Count - 1;
        }

        public bool Select(int index) {
            if (index < 0 || index >= VirtualListSize) {
                return false;
            }

            SelectedIndices.Clear();
            SelectedIndices.Add(index);
            Items[index]?.EnsureVisible();
            return true;
        }

        public new void Clear() {
            VirtualListSize = 0;
            mFilteredPackets.Clear();
        }

        // Private Event Handlers
        private void mPacketList_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e) {
            if (mFilteredPackets.Count <= e.ItemIndex) {
                File.AppendAllText("MapleShark Error.txt",
                    $"Retrieving VirtualItem:{e.ItemIndex} >= Size:{mFilteredPackets.Count}\n");
                return;
            }

            MaplePacket packet = mFilteredPackets[e.ItemIndex];
            Definition definition = Config.Instance.GetDefinition(packet);
            string name = definition == null ? "" : definition.Name;

            var packetItem = new MaplePacketItem(packet, name);
            if (packetItem.Packet.Outbound) {
                packetItem.BackColor = HighlightColor;
            }
            e.Item = packetItem;
        }

        private void mPacketList_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e) {
            e.Index = e.StartIndex;
        }

        private void mPacketList_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e) {
            e.Graphics.FillRectangle(new SolidBrush(BackColor), e.Bounds);
            var top = new Point(e.Bounds.Right - 1, e.Bounds.Top);
            var bottom = new Point(e.Bounds.Right - 1, e.Bounds.Bottom);
            e.Graphics.DrawLine(new Pen(DividerColor), top, bottom);
            /*e.Graphics.DrawString(e.Header.Text, e.Font, new SolidBrush(ForeColor), e.Header.,
                StringFormat.GenericDefault);*/
            TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, Rectangle.Inflate(e.Bounds, -5, -2), ForeColor, TextFormatFlags.WordEllipsis);
        }

        private void mPacketList_DrawItem(object sender, DrawListViewItemEventArgs e) {
            e.DrawDefault = true;
        }

        private void mPacketList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e) {
            e.DrawDefault = true;
        }
    }
}