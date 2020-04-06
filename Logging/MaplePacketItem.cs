using System.Windows.Forms;

namespace MapleShark
{
    public sealed class MaplePacketItem : ListViewItem {
        public readonly MaplePacket Packet;

        internal MaplePacketItem(MaplePacket packet, string name) : base(new string[] {
            packet.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                packet.Outbound ? "Outbound" : "Inbound",
                packet.Buffer.Length.ToString(),
                "0x" + packet.Opcode.ToString("X4"),
                name }) {
            this.Name = name;
            this.Packet = packet;
        }
    }
}
