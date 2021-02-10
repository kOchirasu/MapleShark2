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
            Dark
        }

        private static readonly IMapleSharkTheme lightTheme = new LightTheme();
        private static readonly IMapleSharkTheme darkTheme = new DarkTheme();

        public string Interface = "";
        public ushort LowPort = 30000; //MS2 Gateway
        public ushort HighPort = 33001; //MS2 Channel Ranges
        public ThemeType WindowTheme = ThemeType.Light;

        [XmlIgnore] public IMapleSharkTheme Theme = lightTheme;
        [XmlIgnore] public bool LoadedFromFile = false;

        private static Config sInstance = null;

        internal static Config Instance {
            get {
                if (sInstance == null) {
                    if (!File.Exists("Config.xml")) {
                        sInstance = new Config();
                        sInstance.Save();
                    } else {
                        try {
                            using (XmlReader xr = XmlReader.Create("Config.xml")) {
                                XmlSerializer xs = new XmlSerializer(typeof(Config));
                                sInstance = xs.Deserialize(xr) as Config;
                                sInstance.LoadTheme();
                                sInstance.LoadedFromFile = true;
                            }
                        } catch (Exception ex) {
                            MessageBox.Show(
                                "The configuration file is broken and could not be read. You'll have to reconfigure MapleShark... Sorry!\r\nAdditional exception info:\r\n"
                                + ex.ToString(), "MapleShark", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            return GetDefinition(packet.Build, packet.Locale, packet.Outbound, packet.Opcode);
        }

        internal Definition GetDefinition(uint pBuild, byte pLocale, bool pOutbound, ushort pOpcode) {
            return DefinitionsContainer.Instance.GetDefinition(pLocale, pBuild, pOpcode, pOutbound);
            // return Definitions.Find(d => d.Locale == pLocale && d.Build == pBuild && d.Outbound == pOutbound && d.Opcode == pOpcode);
        }

        internal static string GetPropertiesFile(bool pOutbound, byte pLocale, uint pVersion) {
            string[] parts = new[] {
                Environment.CurrentDirectory, "Scripts", pLocale.ToString(), pVersion.ToString(),
                (pOutbound ? "send" : "recv") + ".properties"
            };
            return Path.Combine(parts);
        }

        internal void Save() {
            XmlWriterSettings xws = new XmlWriterSettings() {
                Indent = true,
                IndentChars = "  ",
                NewLineOnAttributes = true,
                OmitXmlDeclaration = true
            };
            using (XmlWriter xw = XmlWriter.Create("Config.xml", xws)) {
                XmlSerializer xs = new XmlSerializer(typeof(Config));
                xs.Serialize(xw, this);
            }

            if (!Directory.Exists("Scripts")) Directory.CreateDirectory("Scripts");
        }
    }
}