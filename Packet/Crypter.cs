namespace MapleShark.Packet
{
    public interface Crypter
    {
        void Init(uint version);
        int Encrypt(byte[] src, int offset, uint seqKey);
        int Decrypt(byte[] src, int offset, uint seqKey);
    }
}
