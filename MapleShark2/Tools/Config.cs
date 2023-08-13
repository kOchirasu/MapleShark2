using System;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using MapleShark2.Logging;
using MapleShark2.Theme;

namespace MapleShark2.Tools {
    public sealed class Config {
        public enum ThemeType {
            Light,
            Dark,
        }

        private static readonly IMapleSharkTheme lightTheme = new LightTheme();
        private static readonly IMapleSharkTheme darkTheme = new DarkTheme();

        public string Interface = "";
        public ushort LowPort = 30000; //MS2 Gateway
        public ushort HighPort = 33001; //MS2 Channel Ranges
        public int PacketRate = 300;
        public ThemeType WindowTheme = ThemeType.Light;

        [XmlIgnore] public IMapleSharkTheme Theme = lightTheme;
        [XmlIgnore] public bool LoadedFromFile = false;

        private static Config sInstance = null;

        internal static Config Instance {
            get {
                if (sInstance == null) {
                    string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml");
                    if (!File.Exists(configPath)) {
                        sInstance = new Config();
                        sInstance.Save();
                    } else {
                        try {
                            using var xr = XmlReader.Create(configPath);
                            var xs = new XmlSerializer(typeof(Config));
                            sInstance = xs.Deserialize(xr) as Config;
                            sInstance.LoadTheme();
                            sInstance.LoadedFromFile = true;
                        } catch (Exception ex) {
                            const string message = "The configuration file is broken and could not be read. "
                                                   + "You'll have to reconfigure MapleShark."
                                                   + "\n\nAdditional exception info:\n{0}";
                            string formatted = string.Format(message, ex);
                            MessageBox.Show(formatted, "MapleShark2", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            sInstance = new Config();
                        }
                    }
                }

                return sInstance;
            }
        }

        internal void LoadTheme() {
            Theme = sInstance.WindowTheme == ThemeType.Dark ? darkTheme : lightTheme;
        }

        internal Definition GetDefinition(MaplePacket packet) {
            return GetDefinition(packet.Version, packet.Locale, packet.Outbound, packet.Opcode);
        }

        internal Definition GetDefinition(uint pBuild, byte pLocale, bool pOutbound, ushort pOpcode) {
            return DefinitionsContainer.Instance.GetDefinition(pLocale, pBuild, pOpcode, pOutbound);
        }

        internal static string GetPropertiesFile(bool pOutbound, byte pLocale, uint pVersion) {
            return Path.Combine(Helpers.GetScriptFolder(pLocale, pVersion),
                $"{(pOutbound ? "send" : "recv")}.properties");
        }

        internal void Save() {
            var xws = new XmlWriterSettings {
                Indent = true,
                IndentChars = "  ",
                NewLineOnAttributes = true,
                OmitXmlDeclaration = true
            };

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.xml");
            using (var xw = XmlWriter.Create(configPath, xws)) {
                var xs = new XmlSerializer(typeof(Config));
                xs.Serialize(xw, this);
            }

            Directory.CreateDirectory(Helpers.GetScriptsRoot());
        }
    }
}
