using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using MapleShark2.Logging;
using MapleShark2.Theme;
using MapleShark2.Tools;
using MapleShark2.UI.Control;
using Microsoft.Scripting.Hosting;
using NLog;
using WeifenLuo.WinFormsUI.Docking;

namespace MapleShark2.UI {
    public partial class StructureForm : DockContent {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ScriptManager scriptManager;
        private readonly Stack<StructureNode> subNodes = new Stack<StructureNode>();

        private MaplePacket packet;
        private MainForm MainForm => ParentForm as MainForm;
        public TreeView Tree => tree;

        public StructureForm() {
            InitializeComponent();

            scriptManager = new ScriptManager(this);
        }

        public void ApplyTheme() {
            BackColor = Config.Instance.Theme.DockSuiteTheme.ColorPalette.MainWindowActive.Background;
            ThemeApplier.ApplyTheme(Config.Instance.Theme, Controls);
        }

        public void ParseMaplePacket(MaplePacket packet) {
            tree.Nodes.Clear();
            subNodes.Clear();
            packet.Reset(); // Seek back to beginning
            this.packet = packet;

            ScriptEngine engine = scriptManager.GetEngine(packet.Locale, packet.Version);
            try {
                string scriptPath = Helpers.GetScriptPath(packet.Locale, packet.Version, packet.Outbound, packet.Opcode);
                if (!File.Exists(scriptPath)) {
                    return;
                }

                ScriptSource script = engine.CreateScriptSourceFromFile(scriptPath);
                // TODO: Compile scripts for reuse? "script.Compile();"
                script.Execute();
            } catch (Exception ex) {
                var exceptionOperations = engine.GetService<ExceptionOperations>();
                string message = exceptionOperations.FormatException(ex);
                logger.Error(message);
                MessageBox.Show(ex.Message, "Script Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (this.packet.Available > 0) {
                CurrentNodes.Add(new StructureNode("Undefined", this.packet.GetReadSegment(this.packet.Available)));
                this.packet.Skip(this.packet.Available);
            }
        }

        private TreeNodeCollection CurrentNodes => subNodes.Count > 0 ? subNodes.Peek().Nodes : tree.Nodes;

        private void mTree_AfterSelect(object pSender, TreeViewEventArgs pArgs) {
            if (!(pArgs.Node is StructureNode node)) {
                MainForm.DataForm.ClearHexBoxSelection();
                MainForm.PropertyForm.Properties.SelectedObject = null;
                return;
            }

            MainForm.DataForm.SelectHexBoxRange(node.Data);
            MainForm.PropertyForm.Properties.SelectedObject = new StructureSegment(node.Data, MainForm.Locale);
        }

        private void mTree_KeyDown(object sender, KeyEventArgs args) {
            MainForm.CopyPacketHex(args);
        }

        // Scripting functions
        public T Add<T>(string name) where T : struct {
            int size = Unsafe.SizeOf<T>();
            CurrentNodes.Add(new StructureNode(name, packet.GetReadSegment(size)));
            return packet.Read<T>();
        }

        public byte[] AddField(string name, int length) {
            CurrentNodes.Add(new StructureNode(name, packet.GetReadSegment(length)));
            return packet.Read(length);
        }

        public void StartNode(string name) {
            var node = new StructureNode(name, packet.GetReadSegment(0));
            if (subNodes.Count > 0) subNodes.Peek().Nodes.Add(node);
            else tree.Nodes.Add(node);
            subNodes.Push(node);
        }

        public void EndNode(bool expand) {
            if (subNodes.Count > 0) {
                StructureNode node = subNodes.Pop();
                int length = packet.Position - node.Data.Offset;
                node.UpdateData(packet.GetSegment(node.Data.Offset, length));
                if (expand) node.Expand();
            }
        }

        public int Remaining() {
            return packet.Available;
        }

        public void Log(string message, string level) {
            LogLevel logLevel;
            try {
                logLevel = LogLevel.FromString(level);
            } catch {
                logLevel = LogLevel.Info;
            }

            logger.Log(logLevel, message);
        }
    }
}