using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using Sanford.Multimedia;
using Sanford.Multimedia.Midi;

namespace SignalRSelfHost
{
	public delegate void ChannelMsg(string cmd, string channel, string data1, string data2);

	public class SanfordMidiTracker
	{
		private InputDevice inDevice = null;
		public event ChannelMsg ChannelMsg;

		public SanfordMidiTracker()
		{
			Console.WriteLine("Number of Midi devices: " + InputDevice.DeviceCount);

			if (InputDevice.DeviceCount > 0)
			{
				try
				{
					inDevice = new InputDevice(0);
					inDevice.ChannelMessageReceived += HandleChannelMessageReceived;
					inDevice.Error += new EventHandler<ErrorEventArgs>(inDevice_Error);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Exception: " + ex.Message);
				}
			}
		}

		public void StartListening()
		{
			if (inDevice != null)
			{
				inDevice.StartRecording();
			}
		}

		public void StopListening()
		{
			if (inDevice != null)
			{
				inDevice.StopRecording();
				inDevice.Reset();
			}
		}

		private void inDevice_Error(object sender, ErrorEventArgs e)
		{
			Console.WriteLine("Device error: " + e.Error.Message);
		}

		private void HandleChannelMessageReceived(object sender, ChannelMessageEventArgs e)
		{
			if (ChannelMsg != null)
			{
				ChannelMsg(
					e.Message.Command.ToString(),
					e.Message.MidiChannel.ToString(),
					e.Message.Data1.ToString(),
					e.Message.Data2.ToString()
					);
			}
		}
	}
}
