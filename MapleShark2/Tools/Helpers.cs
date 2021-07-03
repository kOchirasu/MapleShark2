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
            return Path.Combine(GetScriptFolder(locale, build), outbound ? "Outbound" : "Inbound", $"0x{opcode:X4}.py");
        }

        public static string GetCommonScriptPath(byte locale, uint build) {
            return Path.Combine(GetScriptFolder(locale, build), "common.py");
        }

        public static void MakeSureFileDirectoryExists(string path) {
            string dirname = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dirname)) {
                return;
            }

            Directory.CreateDirectory(dirname);
        }
    }
}