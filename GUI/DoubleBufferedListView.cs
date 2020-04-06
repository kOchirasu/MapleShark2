using System.Windows.Forms;
using MapleShark.Tools;

namespace MapleShark
{
    public sealed class DoubleBufferedListView : ListView
    {
        public DoubleBufferedListView() {
            DoubleBuffered = true;
        }

        public new int VirtualListSize {
            get => base.VirtualListSize;
            set => this.SetVirtualListSizeWithoutRefresh(value);
        }
    }
}