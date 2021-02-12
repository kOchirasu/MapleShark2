using System;
using System.IO;

namespace MapleShark2.Tools {
    public static class Helpers {
        public static string GetScriptsRoot() {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts");
        }

        public static string GetScriptFolder(byte locale, uint build) {
            return Path.Combine(GetScriptsRoot(), locale.ToString(), build.ToString());
        }

        public static string GetScriptPath(byte locale, uint build, bool outbound, ushort opcode) {
            return Path.Combine(GetScriptFolder(locale, build), outbound ? "Outbound" : "Inbound", $"{opcode:X4}.txt");
        }

        public static string GetCommonScriptPath(byte locale, uint build) {
            return Path.Combine(GetScriptFolder(locale, build), "Common.txt");
        }
    }
}