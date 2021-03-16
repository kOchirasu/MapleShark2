using System.Collections.Generic;
using System.IO;
using IronPython.Hosting;
using MapleShark2.UI;
using Microsoft.Scripting.Hosting;

namespace MapleShark2.Tools {
    public class ScriptManager {
        private readonly StructureForm form;
        private readonly Dictionary<(byte Locale, uint Version), ScriptEngine> engines;

        public ScriptManager(StructureForm form) {
            this.form = form;
            this.engines = new Dictionary<(byte Locale, uint Version), ScriptEngine>();
        }

        public void RunScript(byte locale, uint version, bool outbound, ushort opcode) {
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
    }
}