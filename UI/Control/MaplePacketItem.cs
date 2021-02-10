using System.Windows.Forms;
using MapleShark2.Logging;

namespace MapleShark2.UI.Control
{
    public sealed class MaplePacketItem : ListViewItem {
        public readonly MaplePacket Packet;

        internal MaplePacketItem(MaplePacket packet, string name) : base(new[] {
            packet.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
            packet.Outbound ? "OUT" : "IN",
            packet.Length.ToString(),
            $"0x{packet.Opcode:X4}",
            name
        }) {
            this.Name = name;
            this.Packet = packet;
        }
    }
}
