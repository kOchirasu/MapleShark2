using System;
using System.Text;

namespace MapleShark2.Logging {
    public class MaplePacket {
        public DateTime Timestamp { get; private set; }
        public bool Outbound { get; private set; }
        public uint Build { get; private set; }
        public byte Locale { get; private set; }
        public ushort Opcode { get; private set; }

        public byte[] Buffer { get; private set; }
        public int Cursor { get; private set; }
        public int Length => Buffer.Length;
        public int Remaining => Length - Cursor;
        public uint PreDecodeIV { get; private set; }
        public uint PostDecodeIV { get; private set; }

        internal MaplePacket(DateTime pTimestamp, bool pOutbound, uint pBuild, ushort pOpcode, byte[] pBuffer, uint pPreDecodeIV = 0, uint pPostDecodeIV = 0) {
            Timestamp = pTimestamp;
            Outbound = pOutbound;
            Build = pBuild;
            Opcode = pOpcode;
            Buffer = pBuffer;
            PreDecodeIV = pPreDecodeIV;
            PostDecodeIV = pPostDecodeIV;
        }

        public void Rewind() { Cursor = 0; }

        public bool ReadByte(out byte pValue)
        {
            pValue = 0;
            if (Cursor + 1 > Length) return false;
            pValue = Buffer[Cursor++];
            return true;
        }
        public bool ReadSByte(out sbyte pValue)
        {
            pValue = 0;
            if (Cursor + 1 > Length) return false;
            pValue = (sbyte)Buffer[Cursor++];
            return true;
        }
        public bool ReadUShort(out ushort pValue)
        {
            pValue = 0;
            if (Cursor + 2 > Length) return false;
            pValue = (ushort)(Buffer[Cursor++] |
                              Buffer[Cursor++] << 8);
            return true;
        }
        public bool ReadShort(out short pValue)
        {
            pValue = 0;
            if (Cursor + 2 > Length) return false;
            pValue = (short)(Buffer[Cursor++] |
                             Buffer[Cursor++] << 8);
            return true;
        }
        public bool ReadUInt(out uint pValue)
        {
            pValue = 0;
            if (Cursor + 4 > Length) return false;
            pValue = (uint)(Buffer[Cursor++] |
                            Buffer[Cursor++] << 8 |
                            Buffer[Cursor++] << 16 |
                            Buffer[Cursor++] << 24);
            return true;
        }
        public bool ReadInt(out int pValue)
        {
            pValue = 0;
            if (Cursor + 4 > Length) return false;
            pValue = (int)(Buffer[Cursor++] |
                           Buffer[Cursor++] << 8 |
                           Buffer[Cursor++] << 16 |
                           Buffer[Cursor++] << 24);
            return true;
        }
        public bool ReadFloat(out float pValue)
        {
            pValue = 0;
            if (Cursor + 4 > Length) return false;
            pValue = BitConverter.ToSingle(Buffer, Cursor);
            Cursor += 4;
            return true;
        }
        public bool ReadULong(out ulong pValue)
        {
            pValue = 0;
            if (Cursor + 8 > Length) return false;
            pValue = (ulong)(Buffer[Cursor++] |
                             Buffer[Cursor++] << 8 |
                             Buffer[Cursor++] << 16 |
                             Buffer[Cursor++] << 24 |
                             Buffer[Cursor++] << 32 |
                             Buffer[Cursor++] << 40 |
                             Buffer[Cursor++] << 48 |
                             Buffer[Cursor++] << 56);
            return true;
        }
        public bool ReadLong(out long pValue)
        {
            pValue = 0;
            if (Cursor + 8 > Length) return false;
            pValue = (long)(Buffer[Cursor++] |
                            Buffer[Cursor++] << 8 |
                            Buffer[Cursor++] << 16 |
                            Buffer[Cursor++] << 24 |
                            Buffer[Cursor++] << 32 |
                            Buffer[Cursor++] << 40 |
                            Buffer[Cursor++] << 48 |
                            Buffer[Cursor++] << 56);
            return true;
        }
        public bool ReadFlippedLong(out long pValue) // 5 6 7 8 1 2 3 4
        {
            pValue = 0;
            if (Cursor + 8 > Length) return false;
            pValue = (long)(
                            Buffer[Cursor++] << 32 |
                            Buffer[Cursor++] << 40 |
                            Buffer[Cursor++] << 48 |
                            Buffer[Cursor++] << 56 |
                            Buffer[Cursor++] |
                            Buffer[Cursor++] << 8 |
                            Buffer[Cursor++] << 16 |
                            Buffer[Cursor++] << 24);
            return true;
        }
        public bool ReadDouble(out double pValue)
        {
            pValue = 0;
            if (Cursor + 8 > Length) return false;
            pValue = BitConverter.ToDouble(Buffer, Cursor);
            Cursor += 8;
            return true;
        }
        public bool ReadBytes(byte[] pBytes) { return ReadBytes(pBytes, 0, pBytes.Length); }
        public bool ReadBytes(byte[] pBytes, int pStart, int pLength)
        {
            if (Cursor + pLength > Length) return false;

            System.Buffer.BlockCopy(Buffer, Cursor, pBytes, pStart, pLength);
            Cursor += pLength;
            return true;
        }

        public bool ReadPaddedString(out string pValue, int pLength)
        {
            pValue = "";
            if (Cursor + pLength > Length) return false;
            int length = 0;
            while (length < pLength && Buffer[Cursor + length] != 0x00) ++length;
            if (length > 0) pValue = Encoding.UTF8.GetString(Buffer, Cursor, length);
            Cursor += pLength;
            return true;
        }

        public bool ReadUnicodeString(out string pValue, int pLength)
        {
            int bLength = pLength * 2;
            pValue = "";
            if (Cursor + bLength > Length) return false;
            int length = 0;
            while (length < bLength && !(Buffer[Cursor + length] != 0x00 && Buffer[Cursor + length + 1] != 0x00)) {
                length += 2;
            }
            if (length > 0) pValue = Encoding.Unicode.GetString(Buffer, Cursor, length);
            Cursor += bLength;
            return true;
        }
    }
}