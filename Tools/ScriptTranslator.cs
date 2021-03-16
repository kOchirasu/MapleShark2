using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MapleShark2.Tools {
    // Really bad translator of old mapleshark scripts to python, you will certainly need to manually fix.
    public class ScriptTranslator {
        public static string RemoveScriptApi(StringReader reader) {
            const string usingScriptApi = "using (ScriptAPI) {";

            var result = new StringBuilder();
            string line;
            int braceCount = 0;
            while ((line = reader.ReadLine()) != null) {
                // Keep lines that are already empty
                line = line.TrimEnd();
                if (string.IsNullOrEmpty(line)) {
                    result.AppendLine(line);
                    continue;
                }

                if (line.Contains(usingScriptApi)) {
                    braceCount++;
                    continue;
                }

                if (braceCount == 0) {
                    result.AppendLine(line);
                    continue;
                }

                if (line.Contains("{")) {
                    braceCount++;
                }
                if (line.Contains("}")) {
                    braceCount--;
                    if (braceCount == 0) {
                        continue;
                    }
                }

                result.AppendLine(line.Substring(4));
            }

            return result.ToString();
        }

        public static bool ContainsAny(string str, params string[] search) {
            foreach (var find in search) {
                if (str.Contains(find)) {
                    return true;
                }
            }

            return false;
        }

        public static string FixEmptyBlocks(string script) {
            script = script.Replace("\t", "    ");
            script = script.Replace("//", "#"); // Convert comments
            script = script.Replace("/*", "\"\"\""); // Convert comments
            script = script.Replace("*/", "\"\"\""); // Convert comments
            script = script.Replace("# none", "pass # none");
            script = script.Replace("# None", "pass # none");
            return Regex.Replace(script, "{.+?\n( +)(//.+)\n( *)}", "{\n$1pass $2\n$3}", RegexOptions.Multiline);
        }

        public static string TranslateScript(string script) {
            var reader = new StringReader(RemoveScriptApi(new StringReader(script)));
            var result = new StringBuilder();
            result.AppendLine("from script_api import *");
            if (ContainsAny(script, "DecodeCoordF(", "DecodeCoordS(", "DecodeEquipColor(", "DecodeSkinColor(",
                "DecodeUgcData(", "DecodeSyncState(", "DecodeItem(", "DecodeStatOption(", "DecodeBonusOption(",
                "DecodePlayer(", "DecodeNpcStats(", "DecodeSkillTree(", "DecodeGemSockets(", "DecodeGemstone(",
                "DecodeMaid(", "DecodeAdditionalEffect(", "DecodeGuildInviteInfo(", "DecodeGuildRank(")) {
                result.AppendLine("from common import *");
            }

            result.AppendLine();

            string line;
            while ((line = reader.ReadLine()) != null) {
                // Keep lines that are already empty
                line = line.TrimEnd();
                if (string.IsNullOrEmpty(line)) {
                    result.AppendLine(line);
                    continue;
                }

                if (WithoutComments(line).Contains("switch")) {
                    int switchIndent = line.Length - line.TrimStart().Length;
                    string varName = Regex.Matches(line, "switch *\\((\\(.*?\\))? *(\\w+)\\)")[0].Groups[2].Value;
                    if (GetComments(line).Trim().Length > 0) {
                        result.AppendLine(GetComments(line));
                    }
                    bool firstCase = true;
                    while ((line = reader.ReadLine()) != null) {
                        // Keep lines that are already empty
                        line = line.TrimEnd();
                        if (string.IsNullOrEmpty(line)) {
                            result.AppendLine(line);
                            continue;
                        }

                        int blockIndentation = line.Length - line.TrimStart().Length;
                        if (blockIndentation <= switchIndent) {
                            break;
                        }

                        string padding = "";
                        for (int i = 0; i < switchIndent; i++) {
                            padding += ' ';
                        }

                        if (WithoutComments(line).Contains("case")) {
                            string caseValue = Regex.Matches(line, "case *(\\d+)")[0].Groups[1].Value;
                            if (firstCase) {
                                line = $"{padding}if {varName} == {caseValue}: {GetComments(line)}";
                                firstCase = false;
                            } else {
                                line = $"{padding}elif {varName} == {caseValue}: {GetComments(line)}".PadLeft(blockIndentation);
                            }
                        } else if (WithoutComments(line).Contains("default")) {
                            line = $"{padding}else: {GetComments(line)}";
                        } else {
                            line = TranslateLine(line).Substring(4); // 1 indent less for switch statement
                        }

                        line = line.TrimEnd();
                        if (!string.IsNullOrEmpty(line)) {
                            result.AppendLine(line);
                        }
                    }
                }
                line = TranslateLine(line);

                line = line.TrimEnd();
                if (!string.IsNullOrEmpty(line)) {
                    result.AppendLine(line);
                }
            }

            return result.ToString();
        }

        private static string WithoutComments(string line) {
            int index = line.IndexOf("//", StringComparison.Ordinal);
            if (index < 0) {
                index = line.IndexOf("#", StringComparison.Ordinal);
            }
            return index < 0 ? line : line.Substring(0, index);
        }

        private static string GetComments(string line) {
            int index = line.IndexOf("//", StringComparison.Ordinal);
            if (index < 0) {
                index = line.IndexOf("#", StringComparison.Ordinal);
            }
            return index < 0 ? string.Empty : line.Substring(index);
        }

        private static string TranslateLine(string line) {
            line = Regex.Replace(line, " *{", string.Empty);
            line = Regex.Replace(line, "} *", string.Empty);
            line = line.Replace("&&", "and");
            line = line.Replace("||", "or");
            line = line.Replace("(false)", "(False)");
            line = line.Replace("(true)", "(True)");
            line = Regex.Replace(line, "(\\(.*?)!(\\w.*?\\))", "$1not $2");
            line = Regex.Replace(line, "function *(\\w+)\\((.*?)\\)", "def $1($2):");

            // Convert if statements
            line = Regex.Replace(line, "else if *\\(([^#]+)\\) *", "elif $1:");
            line = Regex.Replace(line, "if *\\(([^#]+)\\) *", "if $1:");
            line = Regex.Replace(line, "else", "else:");

            // Convert for loops
            line = Regex.Replace(line, "for *\\((\\w+) = (\\d+); \\w+ < (\\w+); \\w+\\+\\+\\)", "for $1 in range($2, $3):");
            line = Regex.Replace(line, "for *\\((\\w+) = (\\d+); \\w+ <= (\\w+); \\w+\\+\\+\\)", "for $1 in range($2, $3 + 1):");

            line = line.Replace(";", string.Empty);
            return line;
        }
    }
}