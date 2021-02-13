using System.Collections.Generic;

namespace MapleShark2.Logging {
    public sealed class Definition {
        public bool Outbound = false;
        public ushort Opcode = 0;
        public string Name = "";
        public bool Ignore = false;

        public override string ToString() {
            return $"Name: {Name}; Opcode: 0x{Opcode:X4}; Outbound: {Outbound}; Ignored: {Ignore}";
        }
    }

    // Used to serialize and deserialize PacketDefinitions.xml
    public sealed class DefinitionConfig {
        public byte Locale;
        public uint Version;
        public List<Definition> Definitions;

        public DefinitionConfig() {
            Definitions = new List<Definition>();
        }
    }
}