using System;

namespace MapleShark.Packet
{
    public class Rand32
    {
        private uint s1;
        private uint s2;
        private uint s3;

        public Rand32(uint seed)
        {
            uint rand = CrtRand(seed);

            this.s1 = seed | 0x100000;
            this.s2 = rand | 0x1000;
            this.s3 = CrtRand(rand) | 0x10;
        }

        public static uint CrtRand(uint seed)
        {
            return 214013 * seed + 2531011;
        }

        public uint Random()
        {
            uint v3;
            uint v4;
            uint v5;

            v3 = ((((s1 >> 6) & 0x3FFFFFF) ^ (s1 << 12)) & 0x1FFF) ^ ((s1 >> 19) & 0x1FFF) ^ (s1 << 12);
            v4 = ((((s2 >> 23) & 0x1FF) ^ (s2 << 4)) & 0x7F) ^ ((s2 >> 25) & 0x7F) ^ (s2 << 4);
            v5 = ((((s3 << 17) ^ ((s3 >> 8) & 0xFFFFFF)) & 0x1FFFFF) ^ (s3 << 17)) ^ ((s3 >> 11) & 0x1FFFFF);

            s3 = v5;
            s1 = v3;
            s2 = v4;

            return s1 ^ s2 ^ s3;
        }

        public float RandomFloat()
        {
            uint bits = (uint)((Random() & 0x007FFFFF) | 0x3F800000);

            
            return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0) - 1.0f;
        }
    }
}
