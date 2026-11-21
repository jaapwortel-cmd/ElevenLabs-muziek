using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Wave;
namespace ElevenLabsMusicGenerator
{
    internal static class AudioDevicesForm
    {
        internal static ComboBox DeviceList(int selected)
        {
            var list = new PrefixComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top, AccessibleName = L.T("Geluidskaart voor afspelen", "Playback device") };
            list.Items.Add(L.T("Standaard van Windows", "System default"));
            for (int i = 0; i < WaveOut.DeviceCount; i++) list.Items.Add(WaveOut.GetCapabilities(i).ProductName);
            list.SelectedIndex = selected >= -1 && selected < WaveOut.DeviceCount ? selected + 1 : 0;
            return list;
        }
    }
}
