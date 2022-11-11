using System;
using System.ComponentModel;
using System.Windows.Forms;
using VolumeLockAgent.Properties;
using static VolumeLockAgent.WinAPI;

namespace VolumeLockAgent
{
	internal static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Container components = new Container();

			NotifyIcon notifyIcon = new NotifyIcon(components);
			notifyIcon.Text = "VolumeLock";
			notifyIcon.Icon = Resources.icon;

			ContextMenuStrip cmSysTray = new ContextMenuStrip(components);
			notifyIcon.ContextMenuStrip = cmSysTray;
			notifyIcon.Visible = true;

			MixerInfo info = WinAPI.GetMixerControls();

			VOLUME volume = WinAPI.GetVolume(info);

			Application.Run();
		}
	}
}
