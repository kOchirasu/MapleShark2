using SharpPcap.LibPcap;

namespace MapleShark2.Tools {
    public enum PcapConnectionStatus : byte {
        Unknown = 0x00,
        Connected = 0x10,
        Disconnected = 0x20,
        NotApplicable = 0x30,
    }

    public static class LibPcapLiveDeviceExtensions {
        private const uint PCAP_IF_LOOPBACK = 0x00000001;
        private const uint PCAP_IF_UP = 0x00000002;
        private const uint PCAP_IF_RUNNING = 0x00000004;
        private const uint PCAP_IF_WIRELESS = 0x00000008;
        private const uint PCAP_IF_CONNECTION_STATUS = 0x00000030;

        public static bool IsLoopback(this LibPcapLiveDevice device) {
            return (device.Flags & PCAP_IF_LOOPBACK) != 0;
        }

        public static bool IsUp(this LibPcapLiveDevice device) {
            return (device.Flags & PCAP_IF_UP) != 0;
        }

        public static bool IsRunning(this LibPcapLiveDevice device) {
            return (device.Flags & PCAP_IF_RUNNING) != 0;
        }

        public static bool IsWireless(this LibPcapLiveDevice device) {
            return (device.Flags & PCAP_IF_WIRELESS) != 0;
        }

        public static bool IsConnected(this LibPcapLiveDevice device) {
            return device.GetConnectionStatus() == PcapConnectionStatus.Connected;
        }

        public static PcapConnectionStatus GetConnectionStatus(this LibPcapLiveDevice device) {
            return (PcapConnectionStatus) (device.Flags & PCAP_IF_CONNECTION_STATUS);
        }

        public static bool IsActive(this LibPcapLiveDevice device) {
            // Active devices must be up and running
            if (!device.IsUp() || !device.IsRunning()) {
                return false;
            }

            // Loopback devices do not require "connection"
            if (!device.IsLoopback()) {
                // Skip any device that is not connected or not used for networking (NdisWan).
                if (!device.IsConnected() || device.Addresses.Count == 0) {
                    return false;
                }
            }

            return true;
        }
    }
}