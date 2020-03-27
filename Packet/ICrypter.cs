namespace MapleShark.Packet
{
    public interface ICrypter
    {
        void Encrypt(byte[] src);
        void Decrypt(byte[] src);
    }
}
