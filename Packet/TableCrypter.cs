using System;

namespace MapleShark.Packet
{
    public class TableCrypter : Crypter
    {
        private readonly byte[] decrypted;
        private readonly byte[] encrypted;

        public TableCrypter()
        {
            this.decrypted = new byte[256];
            this.encrypted = new byte[256];
        }

        public void Init(uint version)
        {
            int[] shuffle = new int[256];
            for (int i = 0; i < shuffle.Length; i++)
            {
                shuffle[i] = i;
            }

            Rand32 rand32 = new Rand32((uint) Math.Pow(version, 2));
            Shuffle(shuffle, rand32);

            // Shuffle the table of bytes
            for (int i = 0; i < shuffle.Length; i++)
            {
                encrypted[i] = (byte)(shuffle[i] & 0xFF);
                decrypted[encrypted[i] & 0xFF] = (byte)(i & 0xFF);
            }
        }

        public int Encrypt(byte[] src, int offset, uint seqKey)
        {
            int dest = 0;
            if (offset != 0)
            {
                while (dest < offset)
                {
                    src[dest] = encrypted[src[dest] & 0xFF];

                    dest++;
                }
            }
            return dest;
        }

        public int Decrypt(byte[] src, int offset, uint seqKey)
        {
            if (offset != 0)
            {
                for (int i = 0; i < offset; i++)
                {
                    src[i] = decrypted[src[i] & 0xFF];
                }
            }
            return 1;
        }

        private void Shuffle(int[] data, Rand32 rand32)
        {
            int len = data.Length - 1;

            while (len >= 1)
            {
                uint rand = (uint)(rand32.Random() % (len + 1));

                if (len != rand)
                {
                    if (rand >= data.Length || len >= data.Length)
                    {
                        return;
                    }
                    int val = data[len];

                    data[len] = data[rand];
                    data[rand] = val;
                }

                --len;
            }
        }
    }
}
