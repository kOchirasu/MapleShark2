using System;
using Maple2.PacketLib.Tools;

namespace MapleShark2.Logging {
    public class MaplePacket {
        public DateTime Timestamp { get; }
        public bool Outbound { get; }
        public uint Build { get; }
        public byte Locale { get; private set; }
        public ushort Opcode { get; }

        private readonly ArraySegment<byte> buffer;
        private readonly ByteReader reader;

        public int Position => reader.Position - buffer.Offset;
        public int Length => buffer.Count;
        public int Available => reader.Available;

        internal MaplePacket(DateTime pTimestamp, bool pOutbound, uint pBuild, ushort pOpcode, ArraySegment<byte> pBuffer) {
            Timestamp = pTimestamp;
            Outbound = pOutbound;
            Build = pBuild;
            Opcode = pOpcode;
            buffer = pBuffer;
            reader = new ByteReader(buffer.Array, buffer.Offset);
        }

        public void Reset() {
            reader.Skip(-reader.Position + buffer.Offset);
        }

        public ArraySegment<byte> GetSegment(int length) {
            return new ArraySegment<byte>(reader.Buffer, reader.Position, length);
        }

        public ArraySegment<byte> GetSegment(int offset, int length) {
            return new ArraySegment<byte>(reader.Buffer, offset, length);
        }

        public T Read<T>() where T : struct => reader.Read<T>();
        public string ReadRawString(int size) => reader.ReadRawString(size);
        public string ReadRawUnicodeString(int size) => reader.ReadRawUnicodeString(size);
        public void Skip(int count) => reader.Skip(count);

        public Span<byte> AsSpan() {
            return buffer.AsSpan();
        }

        public unsafe string ToHexString() {
            fixed (byte* bytesPtr = buffer.AsSpan()) {
                return HexEncoding.ToHexString(bytesPtr, buffer.Count);
            }
        }
    }
}