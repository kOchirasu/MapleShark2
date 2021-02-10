using System;
using System.Windows.Forms;
using MapleShark2.Tools;
using SharpPcap.LibPcap;

namespace MapleShark2.UI.Child {
    public sealed partial class SetupForm : Form {
        private class DeviceEntry {
            public string Name;
            public PcapDevice Device;

            public override string ToString() => Name;
        }

        public SetupForm() {
            InitializeComponent();
            Text = "MapleShark " + Program.AssemblyVersion;

            bool configured = false;
            int activeConnection = 0;
            foreach (LibPcapLiveDevice device in LibPcapLiveDeviceList.Instance) {
                // Active devices must be up and running
                if (!device.IsUp() || !device.IsRunning()) {
                    continue;
                }

                // Loopback devices do not require "connection"
                if (!device.IsLoopback()) {
                    // Skip any device that is not connected or not used for networking (NdisWan).
                    if (!device.IsConnected() || device.Addresses.Count == 0) {
                        continue;
                    }
                }

                string description = device.Interface.Description;
                string friendlyName = device.Interface.FriendlyName ?? description;
                int index = mInterfaceCombo.Items.Add(new DeviceEntry {
                    Name = friendlyName,
                    Device = device,
                });

                if (device.IsConnected()) {
                    activeConnection = index;
                }

                // Load selected device from config
                if (!configured && device.Interface.Name == Config.Instance.Interface) {
                    mInterfaceCombo.SelectedIndex = index;
                    configured = true;
                }
            }

            if (!configured) {
                mInterfaceCombo.SelectedIndex = activeConnection;
            }

            mLowPortNumeric.Value = Config.Instance.LowPort;
            mHighPortNumeric.Value = Config.Instance.HighPort;
        }

        private void mInterfaceCombo_SelectedIndexChanged(object pSender, EventArgs pArgs) {
            mOKButton.Enabled = mInterfaceCombo.SelectedIndex >= 0;
        }

        private void mLowPortNumeric_ValueChanged(object pSender, EventArgs pArgs) {
            if (mLowPortNumeric.Value > mHighPortNumeric.Value) mLowPortNumeric.Value = mHighPortNumeric.Value;
        }

        private void mHighPortNumeric_ValueChanged(object pSender, EventArgs pArgs) {
            if (mHighPortNumeric.Value < mLowPortNumeric.Value) mHighPortNumeric.Value = mLowPortNumeric.Value;
        }

        private void mOKButton_Click(object pSender, EventArgs pArgs) {
            var entry = (DeviceEntry) mInterfaceCombo.SelectedItem;
            Config.Instance.Interface = entry.Device.Name;
            Config.Instance.LowPort = (ushort) mLowPortNumeric.Value;
            Config.Instance.HighPort = (ushort) mHighPortNumeric.Value;
            Config.Instance.Save();

            DialogResult = DialogResult.OK;
        }

        private void SetupForm_Load(object sender, EventArgs e) { }
    }
}