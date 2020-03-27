using System;
using System.Drawing;
using System.Text;

namespace MapleShark
{
    public sealed class StructureSegment
    {
        private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private byte[] mBuffer;

        public StructureSegment(byte[] pBuffer, int pStart, int pLength, byte locale)
        {
            mBuffer = new byte[pLength];
            Buffer.BlockCopy(pBuffer, pStart, mBuffer, 0, pLength);
        }

        public byte? Byte { get { if (mBuffer.Length >= 1) return mBuffer[0]; return null; } }
        public sbyte? SByte { get { if (mBuffer.Length >= 1) return (sbyte)mBuffer[0]; return null; } }
        public ushort? UShort { get { if (mBuffer.Length >= 2) return BitConverter.ToUInt16(mBuffer, 0); return null; } }
        public short? Short { get { if (mBuffer.Length >= 2) return BitConverter.ToInt16(mBuffer, 0); return null; } }
        public uint? UInt { get { if (mBuffer.Length >= 4) return BitConverter.ToUInt32(mBuffer, 0); return null; } }
        public int? Int { get { if (mBuffer.Length >= 4) return BitConverter.ToInt32(mBuffer, 0); return null; } }
        public float? Float { get { if (mBuffer.Length >= 4) return BitConverter.ToSingle(mBuffer, 0); return null; } }
        public ulong? ULong { get { if (mBuffer.Length >= 8) return BitConverter.ToUInt64(mBuffer, 0); return null; } }
        public long? Long { get { if (mBuffer.Length >= 8) return BitConverter.ToInt64(mBuffer, 0); return null; } }
        public string IpAddress
        {
            get
            {

                if (mBuffer.Length == 4)
                {
                    return string.Format("{0}.{1}.{2}.{3}", mBuffer[0].ToString(), mBuffer[1].ToString(), mBuffer[2].ToString(), mBuffer[3].ToString());
                }

                return null;
            }
        }

        public DateTime? Date
        {
            get
            {
                try
                {
                    if (mBuffer.Length >= 8)
                        return DateTime.FromFileTimeUtc(BitConverter.ToInt64(mBuffer, 0));
                }
                catch { }
                return null;
            }
        }

        public DateTime? EpochDate
        {
            get
            {
                try {
                    if (mBuffer.Length >= 8)
                        return epoch.AddSeconds(BitConverter.ToInt64(mBuffer, 0));
                }
                catch { }
                return null;
            }
        }

        public string String
        {
            get
            {
                if (mBuffer.Length == 0) return null;
                if (mBuffer[0] == 0x00) return "";
                for (int index = 0; index < mBuffer.Length; ++index) {
                    if (mBuffer[index] == 0x00)
                        return Encoding.UTF8.GetString(mBuffer, 0, index);
                }
                return Encoding.UTF8.GetString(mBuffer, 0, mBuffer.Length);
            }
        }

        public string UnicodeString
        {
            get {
                if (mBuffer.Length < 2) return null;
                if (mBuffer[0] == 0x00 && mBuffer[1] == 0x00) return "";
                for (int index = 0; index < mBuffer.Length - 1; index+=2) {
                    if (mBuffer[index] == 0x00 && mBuffer[index + 1] == 0x00)
                        return Encoding.Unicode.GetString(mBuffer, 0, index);
                }
                return Encoding.Unicode.GetString(mBuffer, 0, mBuffer.Length);
            }
        }

        public string StringIgnore
        {
            get
            {
                byte[] sBuffer = new byte[mBuffer.Length];
                Buffer.BlockCopy(mBuffer, 0, sBuffer, 0, mBuffer.Length);
                if (sBuffer.Length == 0) return null;
                for (int index = 0; index < sBuffer.Length; ++index) {
                    if (sBuffer[index] < 0x20)
                        sBuffer[index] = 0x2E;
                }
                return Encoding.UTF8.GetString(sBuffer, 0, sBuffer.Length);
            }
        }

        public Color Color => mBuffer.Length < 4 ? default : Color.FromArgb(mBuffer[3], mBuffer[2], mBuffer[1], mBuffer[0]);

        public string Length => mBuffer.Length + (mBuffer.Length != 1 ? " bytes" : " byte");
    }
}
