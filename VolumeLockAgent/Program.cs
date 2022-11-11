using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoPlayerController;
using VolumeLockAgent.Properties;

namespace VolumeLockAgent
{
	internal static class Program
	{
		private static bool needStop;
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			using (Container components = new Container())
			{
				NotifyIcon notifyIcon = new NotifyIcon(components);

				notifyIcon.Text = "VolumeLock";
				notifyIcon.Icon = Resources.icon;

				ContextMenuStrip cmSysTray = new ContextMenuStrip(components);
				notifyIcon.ContextMenuStrip = cmSysTray;
				notifyIcon.Visible = true;

				ToolStripMenuItem closeItem = new ToolStripMenuItem(
					text: "Close",
					image: null,
					onClick: (sender, args) =>
					         {
						         needStop = true;
								 Application.Exit();
					         });
				cmSysTray.Items.Add(closeItem);

				float needVolume = 64f;

				Task.Run(
					async () =>
					{
						try
						{
							Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

							while (!needStop)
							{
								float volume = AudioManager.GetMasterVolume();

								if (Math.Abs(needVolume - volume) > float.Epsilon)
								{
									AudioManager.SetMasterVolume(needVolume);
								}

								await Task.Delay(1000);
							}
						}
						finally
						{
							Thread.CurrentThread.Priority = ThreadPriority.Normal;
						}
					});

				Application.Run();

				needStop = true;
			}
		}
	}
}
