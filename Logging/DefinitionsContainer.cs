using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using MapleShark2.Tools;
using DefinitionsIndex =
    System.Collections.Generic.Dictionary<(byte Locale, uint Version), System.Collections.Generic.Dictionary<(bool
        Outbound, ushort Opcode), MapleShark2.Logging.Definition>>;
using DefinitionConfigMap =
    System.Collections.Generic.Dictionary<(bool Outbound, ushort Opcode), MapleShark2.Logging.Definition>;

namespace MapleShark2.Logging {
    public sealed class DefinitionsContainer {
        public static DefinitionsContainer Instance { get; private set; }

        private readonly DefinitionsIndex definitions;

        private DefinitionsContainer(DefinitionsIndex index) {
            definitions = index;
        }

        public Definition GetDefinition(byte locale, uint version, ushort opcode, bool outbound) {
            if (!definitions.ContainsKey((locale, version))) return null;
            if (definitions[(locale, version)].TryGetValue((outbound, opcode), out Definition definition)) {
                return definition;
            }

            return null;
        }

        public void SaveDefinition(byte locale, uint version, Definition def) {
            if (!definitions.ContainsKey((locale, version))) {
                definitions.Add((locale, version), new DefinitionConfigMap());
            }

            definitions[(locale, version)][(def.Outbound, def.Opcode)] = def;
        }

        public static void Load() {
            string scriptsRoot = Helpers.GetScriptsRoot();
            if (!Directory.Exists(scriptsRoot)) return;

            DefinitionsIndex loadDefinitions = new DefinitionsIndex();
            foreach (string localePath in Directory.GetDirectories(scriptsRoot)) {
                foreach (string versionPath in Directory.GetDirectories(localePath)) {
                    string definitionsPath = Path.Combine(versionPath, "PacketDefinitions.xml");
                    if (!File.Exists(definitionsPath)) continue;

                    try {
                        var serializer = new XmlSerializer(typeof(DefinitionConfig));
                        using (var reader = XmlReader.Create(definitionsPath)) {
                            var config = serializer.Deserialize(reader) as DefinitionConfig;
                            DefinitionConfigMap definitionMap = new DefinitionConfigMap();
                            foreach (Definition definition in config.Definitions) {
                                definitionMap[(definition.Outbound, definition.Opcode)] = definition;
                            }

                            loadDefinitions[(config.Locale, config.Version)] = definitionMap;
                        }
                    } catch { }
                }
            }

            Instance = new DefinitionsContainer(loadDefinitions);
        }

        public void Save() {
            foreach ((byte locale, uint version) in definitions.Keys) {
                string path = Helpers.GetScriptFolder(locale, version);
                Directory.CreateDirectory(path);

                var settings = new XmlWriterSettings {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineOnAttributes = true,
                    OmitXmlDeclaration = true
                };

                var config = new DefinitionConfig {
                    Locale = locale,
                    Version = version,
                    Definitions = definitions[(locale, version)].Values.ToList(),
                };
                string definitionsPath = Path.Combine(path, "PacketDefinitions.xml");
                using (var writer = XmlWriter.Create(definitionsPath, settings)) {
                    var serializer = new XmlSerializer(typeof(DefinitionConfig));
                    serializer.Serialize(writer, config);
                }
            }

            SaveProperties();
        }

        internal void SaveProperties() {
            Dictionary<(byte Locale, uint Version), IDictionary<ushort, string>>[] headerList =
                new Dictionary<(byte Locale, uint Version), IDictionary<ushort, string>>[2];

            for (int i = 0; i < 2; i++) {
                headerList[i] = new Dictionary<(byte Locale, uint Version), IDictionary<ushort, string>>();
            }

            foreach ((byte locale, uint version) in definitions.Keys) {
                if (!headerList[0].ContainsKey((locale, version))) {
                    headerList[0].Add((locale, version), new SortedDictionary<ushort, string>());
                }

                if (!headerList[1].ContainsKey((locale, version))) {
                    headerList[1].Add((locale, version), new SortedDictionary<ushort, string>());
                }

                foreach (Definition def in definitions[(locale, version)].Values) {
                    if (def.Opcode == 0xFFFF) return;
                    byte outbound = (byte) (def.Outbound ? 1 : 0);

                    headerList[outbound][(locale, version)][def.Opcode] = def.Name;
                }
            }

            for (int i = 0; i < 2; i++) {
                foreach ((byte locale, uint version) in headerList[i].Keys) {
                    string path = Path.Combine(Helpers.GetScriptsRoot(), locale.ToString(), version.ToString());
                    Directory.CreateDirectory(path);

                    var builder = new StringBuilder();
                    builder.AppendLine("# Generated by MapleShark2");
                    foreach (KeyValuePair<ushort, string> kvp2 in headerList[i][(locale, version)]) {
                        builder.AppendLine(
                            $"{(kvp2.Value == "" ? "# UNSET: " : kvp2.Value.Replace(' ', '_'))} = 0x{kvp2.Key:X4}");
                    }

                    string propertiesPath = Path.Combine(path, $"{(i == 0 ? "send" : "recv")}.properties");
                    File.WriteAllText(propertiesPath, builder.ToString());
                }
            }
        }
    }
}