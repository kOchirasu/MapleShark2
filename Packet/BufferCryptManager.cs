using System.Collections.Generic;

namespace MapleShark2.Packet {
    public class BufferCryptManager {
        private readonly ICrypter[] encryptSeq;
        private readonly ICrypter[] decryptSeq;

        public BufferCryptManager(uint version, uint blockIV) {
            // Initialize Crypter Sequence
            List<ICrypter> cryptSeq = InitCryptSeq(version, blockIV);
            encryptSeq = cryptSeq.ToArray();
            cryptSeq.Reverse();
            decryptSeq = cryptSeq.ToArray();
        }

        private static List<ICrypter> InitCryptSeq(uint version, uint blockIV) {
            ICrypter[] crypt = new ICrypter[4];
            crypt[RearrangeCrypter.GetIndex(version)] = new RearrangeCrypter();
            crypt[XORCrypter.GetIndex(version)] = new XORCrypter(version);
            crypt[TableCrypter.GetIndex(version)] = new TableCrypter(version);

            List<ICrypter> cryptSeq = new List<ICrypter>();
            while (blockIV > 0) {
                var crypter = crypt[blockIV % 10];
                if (crypter != null) {
                    cryptSeq.Add(crypter);
                }
                blockIV /= 10;
            }

            return cryptSeq;
        }

        public void Decrypt(byte[] packet) {
            foreach (ICrypter crypter in decryptSeq) {
                crypter.Decrypt(packet);
            }
        }

        public void Encrypt(byte[] packet) {
            foreach (ICrypter crypter in encryptSeq) {
                crypter.Encrypt(packet);
            }
        }
    }
}
