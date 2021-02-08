using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MapleShark.Tools;

namespace MapleShark {
    public sealed class PacketListView : ListView {
        public IReadOnlyList<MaplePacket> FilteredPackets => mFilteredPackets;
        public MaplePacket Selected =>
            this.SelectedIndices.Count > 0 ? mFilteredPackets[this.SelectedIndices[0]] : default;

        private readonly List<MaplePacket> mFilteredPackets;
        private bool updating;

        public PacketListView() {
            DoubleBuffered = true;
            mFilteredPackets = new List<MaplePacket>();
            VirtualMode = true;
            RetrieveVirtualItem += this.mPacketList_RetrieveVirtualItem;
            SearchForVirtualItem += this.mPacketList_SearchForVirtualItem;
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
                packetItem.BackColor = Color.AliceBlue;
            }
            e.Item = packetItem;
        }

        private void mPacketList_SearchForVirtualItem(object sender, SearchForVirtualItemEventArgs e) {
            e.Index = e.StartIndex;
        }
    }
}