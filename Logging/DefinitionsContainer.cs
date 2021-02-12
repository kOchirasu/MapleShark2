using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MapleShark2.Tools;

namespace MapleShark2.Logging {
    public sealed class DefinitionsContainer {
        public static DefinitionsContainer Instance { get; private set; }

        private readonly Dictionary<byte, Dictionary<uint, List<Definition>>> definitions =
            new Dictionary<byte, Dictionary<uint, List<Definition>>>();

        public DefinitionsContainer() {
            LoadDefinitions();
        }

        public Definition GetDefinition(byte pLocale, uint pVersion, ushort pOpcode, bool pOutbound) {
            if (!definitions.ContainsKey(pLocale)) return null;
            if (!definitions[pLocale].ContainsKey(pVersion)) return null;

            return definitions[pLocale][pVersion].Find(d => d.Outbound == pOutbound && d.Opcode == pOpcode);
        }

        public void SaveDefinition(Definition pDefinition) {
            if (!definitions.ContainsKey(pDefinition.Locale))
                definitions.Add(pDefinition.Locale, new Dictionary<uint, List<Definition>>());
            if (!definitions[pDefinition.Locale].ContainsKey(pDefinition.Build))
                definitions[pDefinition.Locale].Add(pDefinition.Build, new List<Definition>());

            definitions[pDefinition.Locale][pDefinition.Build].RemoveAll(d =>
                d.Outbound == pDefinition.Outbound && d.Opcode == pDefinition.Opcode);

            definitions[pDefinition.Locale][pDefinition.Build].Add(pDefinition);
        }

        public static void Load() {
            Instance = new DefinitionsContainer();
        }

        private void LoadDefinitions() {
            string scriptsRoot = Helpers.GetScriptsRoot();
            if (!Directory.Exists(scriptsRoot))
                return;

            foreach (string localePath in Directory.GetDirectories(scriptsRoot)) {
                string localeDirName = Path.GetFileName(localePath.TrimEnd(Path.DirectorySeparatorChar));
                if (!byte.TryParse(localeDirName, out byte locale)) continue;

                definitions.Add(locale, new Dictionary<uint, List<Definition>>());
                foreach (string versionPath in Directory.GetDirectories(localePath)) {
                    string versionDirName = Path.GetFileName(versionPath.TrimEnd(Path.DirectorySeparatorChar));
                    if (!ushort.TryParse(versionDirName, out ushort version)) continue;

                    string definitionsPath = Path.Combine(versionPath, "PacketDefinitions.xml");
                    if (!File.Exists(definitionsPath)) continue;

                    try {
                        var xs = new XmlSerializer(typeof(List<Definition>));
                        using (var xr = XmlReader.Create(definitionsPath)) {
                            definitions[locale].Add(version, xs.Deserialize(xr) as List<Definition>);
                        }
                    } catch { }
                }
            }
        }

        public void Save() {
            foreach (KeyValuePair<byte, Dictionary<uint, List<Definition>>> kvpLocale in definitions) {
                foreach (KeyValuePair<uint, List<Definition>> kvpVersion in kvpLocale.Value) {
                    string path = Helpers.GetScriptFolder(kvpLocale.Key, kvpVersion.Key);
                    Directory.CreateDirectory(path);

                    var xws = new XmlWriterSettings {
                        Indent = true,
                        IndentChars = "  ",
                        NewLineOnAttributes = true,
                        OmitXmlDeclaration = true
                    };

                    string definitionsPath = Path.Combine(path, "PacketDefinitions.xml");
                    using (var xw = XmlWriter.Create(definitionsPath, xws)) {
                        var xs = new XmlSerializer(typeof(List<Definition>));
                        xs.Serialize(xw, kvpVersion.Value);
                    }
                }
            }

            SaveProperties();
        }

        internal void SaveProperties() {
            Dictionary<byte, Dictionary<uint, IDictionary<ushort, string>>>[] headerList =
                new Dictionary<byte, Dictionary<uint, IDictionary<ushort, string>>>[2];

            for (int i = 0; i < 2; i++) {
                headerList[i] = new Dictionary<byte, Dictionary<uint, IDictionary<ushort, string>>>();
            }

            foreach (KeyValuePair<byte, Dictionary<uint, List<Definition>>> kvpLocale in definitions) {
                foreach (KeyValuePair<uint, List<Definition>> kvpVersion in kvpLocale.Value) {
                    foreach (Definition d in kvpVersion.Value) {
                        if (d.Opcode == 0xFFFF) return;
                        byte outbound = (byte) (d.Outbound ? 1 : 0);

                        if (!headerList[outbound].ContainsKey(d.Locale))
                            headerList[outbound].Add(d.Locale, new Dictionary<uint, IDictionary<ushort, string>>());
                        if (!headerList[outbound][d.Locale].ContainsKey(d.Build))
                            headerList[outbound][d.Locale].Add(d.Build, new SortedDictionary<ushort, string>());
                        if (!headerList[outbound][d.Locale][d.Build].ContainsKey(d.Opcode))
                            headerList[outbound][d.Locale][d.Build].Add(d.Opcode, d.Name);
                        else
                            headerList[outbound][d.Locale][d.Build][d.Opcode] = d.Name;
                    }
                }
            }

            for (int i = 0; i < 2; i++) {
                foreach (KeyValuePair<byte, Dictionary<uint, IDictionary<ushort, string>>> kvpLocale in headerList[i]) {
                    string localePath = Path.Combine(Helpers.GetScriptsRoot(), kvpLocale.Key.ToString());
                    Directory.CreateDirectory(localePath);

                    foreach (KeyValuePair<uint, IDictionary<ushort, string>> kvpVersion in kvpLocale.Value) {
                        string versionPath = Path.Combine(localePath, kvpVersion.Key.ToString());
                        Directory.CreateDirectory(versionPath);

                        var builder = new StringBuilder();
                        builder.AppendLine("# Generated by MapleShark2");
                        foreach (KeyValuePair<ushort, string> kvp2 in kvpVersion.Value) {
                            builder.AppendLine(
                                $"{(kvp2.Value == "" ? "# NOT SET: " : kvp2.Value.Replace(' ', '_'))} = 0x{kvp2.Key:X4}");
                        }

                        string propertiesPath = Path.Combine(versionPath, $"{(i == 0 ? "send" : "recv")}.properties");
                        File.WriteAllText(propertiesPath, builder.ToString());
                    }
                }
            }
        }
    }
}