using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
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
			IMMDeviceEnumerator deviceEnumerator = null;
			IMMDeviceCollection devices = null;
			Dictionary<IMMDevice, Dictionary<PropertyKey, PropVariant>> list = new Dictionary<IMMDevice, Dictionary<PropertyKey, PropVariant>>();
			try
			{
				deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
				deviceEnumerator.EnumAudioEndpoints(EDataFlow.eAll, DeviceState.Active, out devices);

				int numDevices;
				devices.GetCount(out numDevices);

				for (int i = 0; i < numDevices; i++)
				{
					IMMDevice device;
					devices.Item(i, out device);

					IPropertyStore propertyStore;
					device.OpenPropertyStore(StorageAccessMode.Read, out propertyStore);

					int propCount;
					propertyStore.GetCount(out propCount);

					Dictionary<PropertyKey, PropVariant> dic = new Dictionary<PropertyKey, PropVariant>();
					for (int propIndex = 0; propIndex < propCount; propIndex++)
					{
						PropertyKey propertyKey;
						propertyStore.GetAt(propIndex, out propertyKey);

						PropVariant propVariant;
						propertyStore.GetValue(ref propertyKey, out propVariant);

						dic[propertyKey] = propVariant;
					}

					list.Add(device, dic);
				}
			}
			finally
			{
				list.Keys.ToList().ForEach(device => Marshal.ReleaseComObject(device));
				if (devices != null) Marshal.ReleaseComObject(devices);
				if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
			}

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
