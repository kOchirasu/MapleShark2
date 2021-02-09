using System.Windows.Forms;

namespace MapleShark2.UI.Control {
    public class StructureNode : TreeNode {
        public readonly byte[] Buffer;
        public readonly int Cursor;
        public int Length;

        public StructureNode(string pDisplay, byte[] pBuffer, int pCursor, int pLength) : base(pDisplay) {
            Buffer = pBuffer;
            Cursor = pCursor;
            Length = pLength;
        }
    }
}