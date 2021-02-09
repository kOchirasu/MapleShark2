namespace MapleShark2.Packet {
    public interface ICrypter {
        void Encrypt(byte[] src);
        void Decrypt(byte[] src);
    }
}