using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IronPython.Hosting;
using MapleShark2.UI;
using Microsoft.Scripting.Hosting;

namespace MapleShark2.Tools {
    public class ScriptManager {
        private static readonly Regex LocaleVersionRegex = new Regex(@"^(\d+)[\\/](\d+)[\\/].+$");
        
        private readonly StructureForm form;
        private readonly Dictionary<(byte Locale, uint Version), ScriptEngine> engines;

        public ScriptManager(StructureForm form) {
            this.form = form;
            this.engines = new Dictionary<(byte Locale, uint Version), ScriptEngine>();

            // reloads modules for all engines when changed
            CreateScriptsWatcher();
        }

        public void ExecuteScript(byte locale, uint version, bool outbound, ushort opcode) {
            string scriptPath = Helpers.GetScriptPath(locale, version, outbound, opcode);
            if (!File.Exists(scriptPath)) {
                return;
            }

            ScriptEngine engine = GetEngine(locale, version);
            ScriptSource script = engine.CreateScriptSourceFromFile(scriptPath);
            // TODO: Compile scripts for reuse? "script.Compile();"
            script.Execute();
        }

        // Returns the engine for the specified locale/version with caching.
        public ScriptEngine GetEngine(byte locale, uint version) {
            if (engines.TryGetValue((locale, version), out ScriptEngine engine)) {
                return engine;
            }

            engine = CreateBaseEngine();
            ICollection<string> paths = engine.GetSearchPaths();
            paths.Add(Helpers.GetScriptFolder(locale, version));
            engine.SetSearchPaths(paths);
            new Task(() => {
                // Warm up these modules because they are commonly used
                engine.Execute("import script_api");
                engine.Execute("import common");
            }).Start();

            engines[(locale, version)] = engine;
            return engine;
        }

        private ScriptEngine CreateBaseEngine() {
            ScriptEngine engine = Python.CreateEngine();
            ICollection<string> paths = engine.GetSearchPaths();
            paths.Add(Helpers.GetScriptsRoot());
            engine.SetSearchPaths(paths);
            engine.Runtime.Globals.SetVariable("structure_form", form);

            return engine;
        }

        // Watch scripts root folder (script_api.py & others)
        private void CreateScriptsWatcher() {
            Helpers.MakeSureFileDirectoryExists(Helpers.GetScriptsRoot() + Path.DirectorySeparatorChar);
            var watcher = new FileSystemWatcher {
                Path = Helpers.GetScriptsRoot(),
                Filter = "*.py",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
            };

            watcher.Changed += (sender, args) => {
                Console.WriteLine(args.ChangeType);
                // Exclude version specific modules.
                Match match = LocaleVersionRegex.Match(args.Name);
                if (match.Success) {
                    byte locale = byte.Parse(match.Groups[1].Value);
                    uint version = uint.Parse(match.Groups[2].Value);
                    // Exclude opcode scripts in Inbound|Outbound.
                    if (Regex.IsMatch(args.Name, @$"^{locale}[\\/]{version}[\\/](Inbound|Outbound)[\\/].+$")) {
                        return;
                    }
                    
                    OnVersionedScriptChanged(locale, version);
                    return;
                }

                OnRootScriptChanged();
            };
        }

        private void OnRootScriptChanged() {
            // Clear all engines so they can be reloaded with updated modules.
            engines.Clear();
        }

        private void OnVersionedScriptChanged(byte locale, uint version) {
            // Remove affected engine so it can be reloaded with updated modules.
            engines.Remove((locale, version));
        }
    }
}
