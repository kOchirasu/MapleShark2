namespace MapleShark.Packet
{
    public class RearrangeCrypter : Crypter
    {
        public RearrangeCrypter()
        {

        }

        public void Init(uint version)
        {

        }

        public int Encrypt(byte[] src, int offset, uint seqKey)
        {
            int len = offset >> 1;

            if (len != 0)
            {
                for (int i = 0; i < len; i++)
                {
                    byte data = src[i];

                    src[i] = src[i + len];
                    src[i + len] = data;
                }
            }

            return 0;
        }

        public int Decrypt(byte[] src, int offset, uint seqKey)
        {
            int len = offset >> 1;

            if (len != 0)
            {
                for (int i = 0; i < len; i++)
                {
                    byte data = src[i];

                    src[i] = src[i + len];
                    src[i + len] = data;
                }
            }

            return 1;
        }
    }
}
