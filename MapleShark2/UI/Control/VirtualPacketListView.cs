using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Tools;

namespace MapleShark2.UI.Control {
    public sealed class VirtualPacketListView : PacketListView {
        private readonly List<MaplePacket> filteredPackets;
        public override IReadOnlyList<MaplePacket> FilteredPackets => filteredPackets;
        public override MaplePacket Selected =>
            SelectedIndices.Count > 0 ? FilteredPackets[this.SelectedIndices[0]] : default;

        public override int Count => VirtualListSize;

        private int firstItem; //stores the index of the first item in the cache
        private MaplePacketItem[] cache; //array to cache items for the virtual list
        private bool updating;

        public VirtualPacketListView() {
            filteredPackets = new List<MaplePacket>();
            DoubleBuffered = true;
            VirtualMode = true;

            RetrieveVirtualItem += packetList_RetrieveVirtualItem;
            CacheVirtualItems += packetList_CacheVirtualItem;
            SearchForVirtualItem += packetList_SearchForVirtualItem;
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

        public override void BeginUpdate() {
            updating = true;
            base.BeginUpdate();
        }

        public override void EndUpdate() {
            UpdateCount();
            updating = false;
            base.EndUpdate();
        }

        private void UpdateCount() {
            try {
                VirtualListSize = filteredPackets.Count;
            } catch {
                /* ignored */
            }
        }

        public override int AddPacket(MaplePacket packetItem) {
            filteredPackets.Add(packetItem);
            // No need to set VirtualListSize while updating, it will be set on completion
            if (!updating) {
                VirtualListSize = filteredPackets.Count;
            }

            return filteredPackets.Count - 1;
        }

        public override bool Select(int index) {
            if (index < 0 || index >= VirtualListSize) {
                return false;
            }

            SelectedIndices.Clear();
            SelectedIndices.Add(index);
            Items[index]?.EnsureVisible();
            return true;
        }

        public override void Clear() {
            VirtualListSize = 0;
            filteredPackets.Clear();
            cache = null;
        }

        // Private Event Handlers
        private void packetList_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e) {
            //check to see if the requested item is currently in the cache
            if (cache != null && e.ItemIndex >= firstItem && e.ItemIndex < firstItem + cache.Length) {
                //A cache hit, so get the ListViewItem from the cache instead of making a new one.
                e.Item = cache[e.ItemIndex - firstItem];
            } else if (filteredPackets.Count > e.ItemIndex) {
                //A cache miss, so create a new ListViewItem and pass it back.
                MaplePacket packet = filteredPackets[e.ItemIndex];
                e.Item = CreateListItem(packet);
            }
        }

        private void packetList_CacheVirtualItem(object sender, CacheVirtualItemsEventArgs e) {
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
                MaplePacket packet = filteredPackets[i + firstItem];
                cache[i] = CreateListItem(packet);
            }
        }

        private void packetList_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e) {
            e.Index = e.StartIndex;
        }
    }
}