namespace MapleShark.Packet
{
    public class BufferCryptManager
    {
        public static readonly int
            ENCRYPT_NONE        = 0,
            ENCRYPT_REARRANGE   = 1,
            ENCRYPT_XOR         = 2,
            ENCRYPT_TABLE       = 3
        ;

        private readonly Crypter[] aEncrypt;

        public BufferCryptManager(uint version)
        {
            this.aEncrypt = new Crypter[4];

            this.aEncrypt[ENCRYPT_NONE] = null;
            this.aEncrypt[(version + ENCRYPT_REARRANGE) % 3 + 1] = new RearrangeCrypter();
            this.aEncrypt[(version + ENCRYPT_XOR) % 3 + 1]       = new XORCrypter();
            this.aEncrypt[(version + ENCRYPT_TABLE) % 3 + 1]     = new TableCrypter();

            for (int i = 3; i > 0; i--)
            {
                this.aEncrypt[i].Init(version);
            }
        }

        public bool Decrypt(byte[] src, int offset, uint seqBlock, uint seqRcv)
        {
            if (seqBlock != 0)
            {
                uint block = 0;

                while (seqBlock > 0)
                {
                    block = seqBlock + 10 * (block - seqBlock / 10);

                    seqBlock /= 10;
                }

                if (block != 0)
                {
                    uint dest;
                    while (block > 0)
                    {
                        dest = block / 10;

                        Crypter crypt = aEncrypt[block % 10];
                        if (crypt != null)
                        {
                            if (crypt.Decrypt(src, offset, seqRcv) == 0)
                                return false;
                        }

                        block = dest;
                    }
                    return true;
                }
            }
            return true;
        }

        public uint Encrypt(byte[] src, int offset, uint seqBlock, uint seqSnd)
        {
            uint dest = 0;
            if (seqBlock != 0)
            {
                uint block = seqBlock / 10;

                while (block != 0)
                {
                    block = seqBlock / 10;
                    dest = 10 * block;

                    Crypter crypt = aEncrypt[seqBlock % 10];
                    if (crypt != null)
                    {
                        crypt.Encrypt(src, offset, seqSnd);
                    }

                    seqBlock = block;
                }
            }
            return dest;
        }
    }
}
