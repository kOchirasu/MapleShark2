using System;
using MapleShark.Packet;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapleShark {
    public sealed class MapleStream {
        private const int DEFAULT_SIZE = 4096;
        private const int HEADER_SIZE = 6;
        private const int OPCODE_SIZE = 2;

        private readonly bool isOutbound;
        private readonly uint version;
        private readonly BufferCryptManager crypter;

        private byte[] buffer = new byte[DEFAULT_SIZE];
        private uint iv;
        private int cursor;

        public MapleStream(bool isOutbound, uint version, uint iv, uint blockIV) {
            this.isOutbound = isOutbound;
            this.version = version;
            this.iv = iv;
            this.crypter = new BufferCryptManager(version, blockIV);
        }

        public void Append(byte[] packet) {
            Append(packet, 0, packet.Length);
        }

        public void Append(byte[] packet, int offset, int length) {
            lock (this) {
                if (buffer.Length - cursor < length) {
                    int newSize = buffer.Length * 2;
                    while (newSize < cursor + length) {
                        newSize *= 2;
                    }
                    byte[] newBuffer = new byte[newSize];
                    Buffer.BlockCopy(buffer, 0, newBuffer, 0, cursor);
                    buffer = newBuffer;
                }
                Buffer.BlockCopy(packet, offset, buffer, cursor, length);
                cursor += length;
            }
        }

        public bool TryRead(DateTime pTransmitted, out MaplePacket packet) {
            lock (this) {
                if (cursor < HEADER_SIZE) {
                    packet = null;
                    return false;
                }

                int packetSize = BitConverter.ToInt32(buffer, 2);
                int bufferSize = HEADER_SIZE + packetSize;
                if (cursor < bufferSize) {
                    packet = null;
                    return false;
                }

                uint preDecodeIV = iv;
                ushort encSeq = BitConverter.ToUInt16(buffer, 0);
                if (DecodeSeqBase(encSeq) != version) {
                    throw new ArgumentException("Packet has invalid sequence header.");
                }

                byte[] packetBuffer = new byte[packetSize];
                Buffer.BlockCopy(buffer, HEADER_SIZE, packetBuffer, 0, packetSize);

                // Remove packet from buffer
                cursor -= bufferSize;
                Buffer.BlockCopy(buffer, bufferSize, buffer, 0, cursor);
                crypter.Decrypt(packetBuffer);

                ushort opcode = BitConverter.ToUInt16(packetBuffer, 0);
                Buffer.BlockCopy(packetBuffer, OPCODE_SIZE, packetBuffer, 0, packetSize - OPCODE_SIZE);
                Array.Resize(ref packetBuffer, packetSize - OPCODE_SIZE);

                packet = new MaplePacket(pTransmitted, isOutbound, version, opcode, packetBuffer, preDecodeIV, iv);
            }

            return true;
        }

        private void AdvanceIV() {
            iv = Rand32.CrtRand(iv);
        }

        public ushort DecodeSeqBase(ushort encSeq) {
            ushort decSeq =  (ushort)((iv >> 16) ^ encSeq);
            AdvanceIV();
            return decSeq;
        }
    }
}
