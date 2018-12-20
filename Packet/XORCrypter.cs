namespace MapleShark.Packet
{
    public class XORCrypter : Crypter
    {
        private readonly byte[] shuffle;

        public XORCrypter()
        {
            this.shuffle = new byte[2];
        }

        public void Init(uint version)
        {
            Rand32 rand1 = new Rand32(version);
            Rand32 rand2 = new Rand32(2 * version);

            shuffle[0] = (byte)(rand1.RandomFloat() * 255.0f);
            shuffle[1] = (byte)(rand2.RandomFloat() * 255.0f);
        }

        public int Encrypt(byte[] src, int offset, uint seqKey)
        {
            int dest = 0;
            if (offset != 0)
            {
                int flag = 0;
                while (dest < offset)
                {
                    src[dest] ^= shuffle[flag];

                    dest++;
                    flag ^= 1;
                }
            }
            return dest;
        }

        public int Decrypt(byte[] src, int offset, uint seqKey)
        {
            if (offset != 0)
            {
                int flag = 0;
                for (int i = 0; i < offset; i++)
                {
                    src[i] ^= shuffle[flag];

                    flag ^= 1;
                }
            }
            return 1;
        }
    }
}
