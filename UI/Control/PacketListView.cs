using System;
using System.Collections.Generic;
using System.Drawing;
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

        private int firstItem; //stores the index of the first item in the cache
        private MaplePacketItem[] cache; //array to cache items for the virtual list
        private readonly List<MaplePacket> mFilteredPackets;
        private bool updating;

        public PacketListView() {
            DoubleBuffered = true;
            mFilteredPackets = new List<MaplePacket>();
            VirtualMode = true;
            OwnerDraw = true;

            RetrieveVirtualItem += this.mPacketList_RetrieveVirtualItem;
            CacheVirtualItems += this.mPacketList_CacheVirtualItem;
            SearchForVirtualItem += this.mPacketList_SearchForVirtualItem;
            DrawColumnHeader += this.mPacketList_DrawColumnHeader;
            DrawItem += this.mPacketList_DrawItem;
            DrawSubItem += this.mPacketList_DrawSubItem;

            // Clear the selected item, this allows auto-scroll to resume
            KeyDown += (sender, e) => {
                if (e.KeyCode == Keys.Escape) {
                    SelectedIndices.Clear();
                }
            };
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
            } catch {
                /* ignored */
            }
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
            //check to see if the requested item is currently in the cache
            if (cache != null && e.ItemIndex >= firstItem && e.ItemIndex < firstItem + cache.Length) {
                //A cache hit, so get the ListViewItem from the cache instead of making a new one.
                e.Item = cache[e.ItemIndex - firstItem];
            } else if (mFilteredPackets.Count > e.ItemIndex) {
                //A cache miss, so create a new ListViewItem and pass it back.
                MaplePacket packet = mFilteredPackets[e.ItemIndex];
                e.Item = CreateListItem(packet);
            }
        }

        private void mPacketList_CacheVirtualItem(object sender, CacheVirtualItemsEventArgs e) {
            //We've gotten a request to refresh the cache. First check if it's really necessary.
            if (cache != null && e.StartIndex >= firstItem && e.EndIndex <= firstItem + cache.Length) {
                //If the newly requested cache is a subset of the old cache,
                //no need to rebuild everything, so do nothing.
                return;
            }

            //Now we need to rebuild the cache.
            firstItem = e.StartIndex;
            int length = e.EndIndex - e.StartIndex + 1; //indexes are inclusive
            cache = new MaplePacketItem[length];

            //Fill the cache with the appropriate ListViewItems.
            for (int i = 0; i < length; i++) {
                MaplePacket packet = mFilteredPackets[i + firstItem];
                cache[i] = CreateListItem(packet);
            }
        }

        private MaplePacketItem CreateListItem(MaplePacket packet) {
            Definition definition = Config.Instance.GetDefinition(packet);
            string name = definition == null ? "" : definition.Name;

            var item = new MaplePacketItem(packet, name);
            if (item.Packet.Outbound) {
                item.BackColor = HighlightColor;
            }

            return item;
        }

        private void mPacketList_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e) {
            e.Index = e.StartIndex;
        }

        private void mPacketList_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e) {
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

        private void mPacketList_DrawItem(object sender, DrawListViewItemEventArgs e) {
            e.DrawDefault = true;
        }

        private void mPacketList_DrawSubItem(object sender, DrawListViewSubItemEventArgs e) {
            e.DrawDefault = true;
        }
    }
}