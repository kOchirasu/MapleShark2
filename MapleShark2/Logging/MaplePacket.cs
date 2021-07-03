using System;
using Maple2.PacketLib.Tools;

namespace MapleShark2.Logging {
    public class MaplePacket {
        public DateTime Timestamp { get; }
        public bool Outbound { get; }
        public uint Version { get; }
        public byte Locale { get; }
        public ushort Opcode { get; }

        private readonly ArraySegment<byte> buffer;
        private readonly ByteReader reader;

        public int Position => reader.Position - buffer.Offset;
        public int Offset => buffer.Offset;
        public int Length => buffer.Count;
        public int Available => reader.Available;

        internal MaplePacket(DateTime timestamp, bool outbound, uint version, ushort opcode, ArraySegment<byte> buffer) {
            Timestamp = timestamp;
            Outbound = outbound;
            Version = version;
            Locale = MapleLocale.UNKNOWN;
            Opcode = opcode;
            this.buffer = buffer;
            reader = new ByteReader(this.buffer.Array, this.buffer.Offset);
        }

        public void Reset() {
            reader.Skip(-reader.Position + buffer.Offset);
        }

        public long Search(byte[] pattern, long start = 0) {
            if (pattern == null || buffer.Array == null || start < 0) {
                return -1;
            }

            long startIndex = buffer.Offset + start;
            for (long i = startIndex; i <= buffer.Array.Length - pattern.Length; i++) {
                bool match = true;
                for (int j = 0; match && j < pattern.Length; j++) {
                    match = buffer.Array[i + j] == pattern[j];
                }

                if (match) {
                    return i - buffer.Offset;
                }
            }

            return -1;
        }

        public ArraySegment<byte> GetReadSegment(int length) {
            return new ArraySegment<byte>(reader.Buffer, reader.Position, length);
        }

        public ArraySegment<byte> GetSegment(int offset, int length) {
            return new ArraySegment<byte>(reader.Buffer, offset, length);
        }

        public T Read<T>() where T : struct => reader.Read<T>();
        public byte[] Read(int count) => reader.ReadBytes(count);
        public void Skip(int count) => reader.Skip(count);

        private unsafe string ToHexString() {
            fixed (byte* bytesPtr = buffer.AsSpan()) {
                return HexEncoding.ToHexString(bytesPtr, buffer.Count, ' ');
            }
        }

        public override string ToString() {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}][{(Outbound ? "OUT" : "IN ")}][{Opcode:X4}] {ToHexString()}";
        }
    }
}