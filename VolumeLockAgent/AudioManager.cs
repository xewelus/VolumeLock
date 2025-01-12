using System;
using System.Data;
using System.Runtime.InteropServices;

namespace VideoPlayerController
{
	/// <summary>
	/// Controls audio using the Windows CoreAudio API
	/// from: http://stackoverflow.com/questions/14306048/controling-volume-mixer
	/// and: http://netcoreaudio.codeplex.com/
	/// </summary>
	public static class AudioManager
	{
		#region Master Volume Manipulation

		/// <summary>
		/// Gets the current master volume in scalar values (percentage)
		/// </summary>
		/// <returns>-1 in case of an error, if successful the value will be between 0 and 100</returns>
		public static float GetMasterVolume()
		{
			IAudioEndpointVolume masterVol = null;
			try
			{
				masterVol = GetMasterVolumeObject();
				if (masterVol == null)
					return -1;

				float volumeLevel;
				masterVol.GetMasterVolumeLevelScalar(out volumeLevel);
				return volumeLevel * 100;
			}
			finally
			{
				if (masterVol != null)
					Marshal.ReleaseComObject(masterVol);
			}
		}

		/// <summary>
		/// Gets the mute state of the master volume. 
		/// While the volume can be muted the <see cref="GetMasterVolume"/> will still return the pre-muted volume value.
		/// </summary>
		/// <returns>false if not muted, true if volume is muted</returns>
		public static bool GetMasterVolumeMute()
		{
			IAudioEndpointVolume masterVol = null;
			try
			{
				masterVol = GetMasterVolumeObject();
				if (masterVol == null)
					return false;

				bool isMuted;
				masterVol.GetMute(out isMuted);
				return isMuted;
			}
			finally
			{
				if (masterVol != null)
					Marshal.ReleaseComObject(masterVol);
			}
		}

		/// <summary>
		/// Sets the master volume to a specific level
		/// </summary>
		/// <param name="newLevel">Value between 0 and 100 indicating the desired scalar value of the volume</param>
		public static void SetMasterVolume(float newLevel)
		{
			IAudioEndpointVolume masterVol = null;
			try
			{
				masterVol = GetMasterVolumeObject();
				if (masterVol == null)
					return;

				masterVol.SetMasterVolumeLevelScalar(newLevel / 100, Guid.Empty);
			}
			finally
			{
				if (masterVol != null)
					Marshal.ReleaseComObject(masterVol);
			}
		}

		/// <summary>
		/// Increments or decrements the current volume level by the <see cref="stepAmount"/>.
		/// </summary>
		/// <param name="stepAmount">Value between -100 and 100 indicating the desired step amount. Use negative numbers to decrease
		/// the volume and positive numbers to increase it.</param>
		/// <returns>the new volume level assigned</returns>
		public static float StepMasterVolume(float stepAmount)
		{
			IAudioEndpointVolume masterVol = null;
			try
			{
				masterVol = GetMasterVolumeObject();
				if (masterVol == null)
					return -1;

				float stepAmountScaled = stepAmount / 100;

				// Get the level
				float volumeLevel;
				masterVol.GetMasterVolumeLevelScalar(out volumeLevel);

				// Calculate the new level
				float newLevel = volumeLevel + stepAmountScaled;
				newLevel = Math.Min(1, newLevel);
				newLevel = Math.Max(0, newLevel);

				masterVol.SetMasterVolumeLevelScalar(newLevel, Guid.Empty);

				// Return the new volume level that was set
				return newLevel * 100;
			}
			finally
			{
				if (masterVol != null)
					Marshal.ReleaseComObject(masterVol);
			}
		}

		/// <summary>
		/// Mute or unmute the master volume
		/// </summary>
		/// <param name="isMuted">true to mute the master volume, false to unmute</param>
		public static void SetMasterVolumeMute(bool isMuted)
		{
			IAudioEndpointVolume masterVol = null;
			try
			{
				masterVol = GetMasterVolumeObject();
				if (masterVol == null)
					return;

				masterVol.SetMute(isMuted, Guid.Empty);
			}
			finally
			{
				if (masterVol != null)
					Marshal.ReleaseComObject(masterVol);
			}
		}

		/// <summary>
		/// Switches between the master volume mute states depending on the current state
		/// </summary>
		/// <returns>the current mute state, true if the volume was muted, false if unmuted</returns>
		public static bool ToggleMasterVolumeMute()
		{
			IAudioEndpointVolume masterVol = null;
			try
			{
				masterVol = GetMasterVolumeObject();
				if (masterVol == null)
					return false;

				bool isMuted;
				masterVol.GetMute(out isMuted);
				masterVol.SetMute(!isMuted, Guid.Empty);

				return !isMuted;
			}
			finally
			{
				if (masterVol != null)
					Marshal.ReleaseComObject(masterVol);
			}
		}

		private static IAudioEndpointVolume GetMasterVolumeObject()
		{
			IMMDeviceEnumerator deviceEnumerator = null;
			IMMDevice speakers = null;
			try
			{
				deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
				deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

				Guid IID_IAudioEndpointVolume = typeof(IAudioEndpointVolume).GUID;
				object o;
				speakers.Activate(ref IID_IAudioEndpointVolume, 0, IntPtr.Zero, out o);
				IAudioEndpointVolume masterVol = (IAudioEndpointVolume)o;

				return masterVol;
			}
			finally
			{
				if (speakers != null) Marshal.ReleaseComObject(speakers);
				if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
			}
		}

		#endregion

		#region Individual Application Volume Manipulation

		public static float? GetApplicationVolume(int pid)
		{
			ISimpleAudioVolume volume = GetVolumeObject(pid);
			if (volume == null)
				return null;

			float level;
			volume.GetMasterVolume(out level);
			Marshal.ReleaseComObject(volume);
			return level * 100;
		}

		public static bool? GetApplicationMute(int pid)
		{
			ISimpleAudioVolume volume = GetVolumeObject(pid);
			if (volume == null)
				return null;

			bool mute;
			volume.GetMute(out mute);
			Marshal.ReleaseComObject(volume);
			return mute;
		}

		public static void SetApplicationVolume(int pid, float level)
		{
			ISimpleAudioVolume volume = GetVolumeObject(pid);
			if (volume == null)
				return;

			Guid guid = Guid.Empty;
			volume.SetMasterVolume(level / 100, ref guid);
			Marshal.ReleaseComObject(volume);
		}

		public static void SetApplicationMute(int pid, bool mute)
		{
			ISimpleAudioVolume volume = GetVolumeObject(pid);
			if (volume == null)
				return;

			Guid guid = Guid.Empty;
			volume.SetMute(mute, ref guid);
			Marshal.ReleaseComObject(volume);
		}

		public static ISimpleAudioVolume GetVolumeObject(int pid)
		{
			IMMDeviceEnumerator deviceEnumerator = null;
			IAudioSessionEnumerator sessionEnumerator = null;
			IAudioSessionManager2 mgr = null;
			IMMDevice speakers = null;
			try
			{
				// get the speakers (1st render + multimedia) device
				deviceEnumerator = (IMMDeviceEnumerator)(new MMDeviceEnumerator());
				deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out speakers);

				// activate the session manager. we need the enumerator
				Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
				object o;
				speakers.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out o);
				mgr = (IAudioSessionManager2)o;

				// enumerate sessions for on this device
				mgr.GetSessionEnumerator(out sessionEnumerator);
				int count;
				sessionEnumerator.GetCount(out count);

				// search for an audio session with the required process-id
				ISimpleAudioVolume volumeControl = null;
				for (int i = 0; i < count; ++i)
				{
					IAudioSessionControl2 ctl = null;
					try
					{
						sessionEnumerator.GetSession(i, out ctl);

						// NOTE: we could also use the app name from ctl.GetDisplayName()
						int cpid;
						ctl.GetProcessId(out cpid);

						if (cpid == pid)
						{
							volumeControl = ctl as ISimpleAudioVolume;
							break;
						}
					}
					finally
					{
						if (ctl != null) Marshal.ReleaseComObject(ctl);
					}
				}

				return volumeControl;
			}
			finally
			{
				if (sessionEnumerator != null) Marshal.ReleaseComObject(sessionEnumerator);
				if (mgr != null) Marshal.ReleaseComObject(mgr);
				if (speakers != null) Marshal.ReleaseComObject(speakers);
				if (deviceEnumerator != null) Marshal.ReleaseComObject(deviceEnumerator);
			}
		}

		#endregion

	}

	#region Abstracted COM interfaces from Windows CoreAudio API

	[ComImport]
	[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
	public class MMDeviceEnumerator
	{
	}

	public enum EDataFlow
	{
		eRender,
		eCapture,
		eAll,
		EDataFlow_enum_count
	}

	public enum ERole
	{
		eConsole,
		eMultimedia,
		eCommunications,
		ERole_enum_count
	}

	/// <summary>
	/// Device State
	/// </summary>
	[Flags]
	public enum DeviceState
	{
		/// <summary>
		/// DEVICE_STATE_ACTIVE
		/// </summary>
		Active = 0x00000001,
		/// <summary>
		/// DEVICE_STATE_DISABLED
		/// </summary>
		Disabled = 0x00000002,
		/// <summary>
		/// DEVICE_STATE_NOTPRESENT 
		/// </summary>
		NotPresent = 0x00000004,
		/// <summary>
		/// DEVICE_STATE_UNPLUGGED
		/// </summary>
		Unplugged = 0x00000008,
		/// <summary>
		/// DEVICE_STATEMASK_ALL
		/// </summary>
		All = 0x0000000F
	}

	[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IMMDeviceEnumerator
	{
		int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState stateMask, out IMMDeviceCollection devices);

		int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);

		int GetDevice(string id, out IMMDevice deviceName);

		// the rest is not implemented
	}

	[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface IMMDeviceCollection
	{
		int GetCount(out int numDevices);
		int Item(int deviceNumber, out IMMDevice device);
	}

	[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface MMDeviceEnumeratorComObject
	{
	}

	[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IMMDevice
	{
		[PreserveSig]
		int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);

		[PreserveSig]
		int OpenPropertyStore(StorageAccessMode stgmAccess, out IPropertyStore properties);

		[PreserveSig]
		int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

		[PreserveSig]
		int GetState(out DeviceState state);
	}

	/// <summary>
	/// MMDevice STGM enumeration
	/// </summary>
	public enum StorageAccessMode
	{
		Read,
		Write,
		ReadWrite
	}

	/// <summary>
	/// is defined in propsys.h
	/// </summary>
	[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IPropertyStore
	{
		int GetCount(out int propCount);
		int GetAt(int property, out PropertyKey key);
		int GetValue(ref PropertyKey key, out PropVariant value);
		int SetValue(ref PropertyKey key, ref PropVariant value);
		int Commit();
	}

	/// <summary>
	/// from Propidl.h.
	/// http://msdn.microsoft.com/en-us/library/aa380072(VS.85).aspx
	/// contains a union so we have to do an explicit layout
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct PropVariant
	{
		[FieldOffset(0)] short vt;
		[FieldOffset(2)] short wReserved1;
		[FieldOffset(4)] short wReserved2;
		[FieldOffset(6)] short wReserved3;
		[FieldOffset(8)] sbyte cVal;
		[FieldOffset(8)] byte bVal;
		[FieldOffset(8)] short iVal;
		[FieldOffset(8)] ushort uiVal;
		[FieldOffset(8)] int lVal;
		[FieldOffset(8)] uint ulVal;
		[FieldOffset(8)] int intVal;
		[FieldOffset(8)] uint uintVal;
		[FieldOffset(8)] long hVal;
		[FieldOffset(8)] long uhVal;
		[FieldOffset(8)] float fltVal;
		[FieldOffset(8)] double dblVal;
		[FieldOffset(8)] bool boolVal;
		[FieldOffset(8)] int scode;
		//CY cyVal;
		[FieldOffset(8)] DateTime date;
		[FieldOffset(8)] System.Runtime.InteropServices.ComTypes.FILETIME filetime;
		//CLSID* puuid;
		//CLIPDATA* pclipdata;
		//BSTR bstrVal;
		//BSTRBLOB bstrblobVal;
		[FieldOffset(8)] Blob blobVal;
		//LPSTR pszVal;
		[FieldOffset(8)] IntPtr pwszVal; //LPWSTR 
		
		/// <summary>
		/// Helper method to gets blob data
		/// </summary>
		byte[] GetBlob()
		{
			byte[] Result = new byte[blobVal.Length];
			Marshal.Copy(blobVal.Data, Result, 0, Result.Length);
			return Result;
		}

		/// <summary>
		/// Property value
		/// </summary>
		public object Value
		{
			get
			{
				VarEnum ve = (VarEnum)vt;
				switch (ve)
				{
					case VarEnum.VT_I1:
						return bVal;
					case VarEnum.VT_I2:
						return iVal;
					case VarEnum.VT_I4:
						return lVal;
					case VarEnum.VT_I8:
						return hVal;
					case VarEnum.VT_INT:
						return iVal;
					case VarEnum.VT_UI4:
						return ulVal;
					case VarEnum.VT_LPWSTR:
						return Marshal.PtrToStringUni(pwszVal);
					case VarEnum.VT_BLOB:
						return GetBlob();
				}
				throw new NotImplementedException("PropVariant " + ve.ToString());
			}
		}

		public override string ToString()
		{
			VarEnum ve = (VarEnum)vt;
			if (ve == VarEnum.VT_LPWSTR)
			{
				return Marshal.PtrToStringUni(pwszVal);
			}
			else
			{
				return ve.ToString();
			}
		}
	}

	internal struct Blob
	{
		public int Length;
		public IntPtr Data;

		//Code Should Compile at warning level4 without any warnings, 
		//However this struct will give us Warning CS0649: Field [Fieldname] 
		//is never assigned to, and will always have its default value
		//You can disable CS0649 in the project options but that will disable
		//the warning for the whole project, it's a nice warning and we do want 
		//it in other places so we make a nice dummy function to keep the compiler
		//happy.
		private void FixCS0649()
		{
			Length = 0;
			Data = IntPtr.Zero;
		}
	}

	/// <summary>
	/// PROPERTYKEY is defined in wtypes.h
	/// </summary>
	public struct PropertyKey
	{
		/// <summary>
		/// Format ID
		/// </summary>
		public Guid formatId;
		/// <summary>
		/// Property ID
		/// </summary>
		public int propertyId;

		public override string ToString()
		{
			return $"PropertyKey {this.propertyId} {this.formatId}";
		}
	}

	[Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioSessionManager2
	{
		int NotImpl1();
		int NotImpl2();

		[PreserveSig]
		int GetSessionEnumerator(out IAudioSessionEnumerator SessionEnum);

		// the rest is not implemented
	}

	[Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioSessionEnumerator
	{
		[PreserveSig]
		int GetCount(out int SessionCount);

		[PreserveSig]
		int GetSession(int SessionCount, out IAudioSessionControl2 Session);
	}

	[Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISimpleAudioVolume
	{
		[PreserveSig]
		int SetMasterVolume(float fLevel, ref Guid EventContext);

		[PreserveSig]
		int GetMasterVolume(out float pfLevel);

		[PreserveSig]
		int SetMute(bool bMute, ref Guid EventContext);

		[PreserveSig]
		int GetMute(out bool pbMute);
	}

	[Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioSessionControl2
	{
		// IAudioSessionControl
		[PreserveSig]
		int NotImpl0();

		[PreserveSig]
		int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

		[PreserveSig]
		int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

		[PreserveSig]
		int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

		[PreserveSig]
		int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string Value, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

		[PreserveSig]
		int GetGroupingParam(out Guid pRetVal);

		[PreserveSig]
		int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid Override, [MarshalAs(UnmanagedType.LPStruct)] Guid EventContext);

		[PreserveSig]
		int NotImpl1();

		[PreserveSig]
		int NotImpl2();

		// IAudioSessionControl2
		[PreserveSig]
		int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

		[PreserveSig]
		int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string pRetVal);

		[PreserveSig]
		int GetProcessId(out int pRetVal);

		[PreserveSig]
		int IsSystemSoundsSession();

		[PreserveSig]
		int SetDuckingPreference(bool optOut);
	}

	// http://netcoreaudio.codeplex.com/SourceControl/latest#trunk/Code/CoreAudio/Interfaces/IAudioEndpointVolume.cs
	[Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IAudioEndpointVolume
	{
		[PreserveSig]
		int NotImpl1();

		[PreserveSig]
		int NotImpl2();

		/// <summary>
		/// Gets a count of the channels in the audio stream.
		/// </summary>
		/// <param name="channelCount">The number of channels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetChannelCount(
			[Out][MarshalAs(UnmanagedType.U4)] out UInt32 channelCount);

		/// <summary>
		/// Sets the master volume level of the audio stream, in decibels.
		/// </summary>
		/// <param name="level">The new master volume level in decibels.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetMasterVolumeLevel(
			[In][MarshalAs(UnmanagedType.R4)] float level,
			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Sets the master volume level, expressed as a normalized, audio-tapered value.
		/// </summary>
		/// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetMasterVolumeLevelScalar(
			[In][MarshalAs(UnmanagedType.R4)] float level,
			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Gets the master volume level of the audio stream, in decibels.
		/// </summary>
		/// <param name="level">The volume level in decibels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetMasterVolumeLevel(
			[Out][MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Gets the master volume level, expressed as a normalized, audio-tapered value.
		/// </summary>
		/// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetMasterVolumeLevelScalar(
			[Out][MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Sets the volume level, in decibels, of the specified channel of the audio stream.
		/// </summary>
		/// <param name="channelNumber">The channel number.</param>
		/// <param name="level">The new volume level in decibels.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetChannelVolumeLevel(
			[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
			[In][MarshalAs(UnmanagedType.R4)] float level,
			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Sets the normalized, audio-tapered volume level of the specified channel in the audio stream.
		/// </summary>
		/// <param name="channelNumber">The channel number.</param>
		/// <param name="level">The new master volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetChannelVolumeLevelScalar(
			[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
			[In][MarshalAs(UnmanagedType.R4)] float level,
			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Gets the volume level, in decibels, of the specified channel in the audio stream.
		/// </summary>
		/// <param name="channelNumber">The zero-based channel number.</param>
		/// <param name="level">The volume level in decibels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetChannelVolumeLevel(
			[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
			[Out][MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Gets the normalized, audio-tapered volume level of the specified channel of the audio stream.
		/// </summary>
		/// <param name="channelNumber">The zero-based channel number.</param>
		/// <param name="level">The volume level expressed as a normalized value between 0.0 and 1.0.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetChannelVolumeLevelScalar(
			[In][MarshalAs(UnmanagedType.U4)] UInt32 channelNumber,
			[Out][MarshalAs(UnmanagedType.R4)] out float level);

		/// <summary>
		/// Sets the muting state of the audio stream.
		/// </summary>
		/// <param name="isMuted">True to mute the stream, or false to unmute the stream.</param>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int SetMute(
			[In][MarshalAs(UnmanagedType.Bool)] Boolean isMuted,
			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Gets the muting state of the audio stream.
		/// </summary>
		/// <param name="isMuted">The muting state. True if the stream is muted, false otherwise.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetMute(
			[Out][MarshalAs(UnmanagedType.Bool)] out Boolean isMuted);

		/// <summary>
		/// Gets information about the current step in the volume range.
		/// </summary>
		/// <param name="step">The current zero-based step index.</param>
		/// <param name="stepCount">The total number of steps in the volume range.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetVolumeStepInfo(
			[Out][MarshalAs(UnmanagedType.U4)] out UInt32 step,
			[Out][MarshalAs(UnmanagedType.U4)] out UInt32 stepCount);

		/// <summary>
		/// Increases the volume level by one step.
		/// </summary>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int VolumeStepUp(
			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Decreases the volume level by one step.
		/// </summary>
		/// <param name="eventContext">A user context value that is passed to the notification callback.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int VolumeStepDown(
			[In][MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

		/// <summary>
		/// Queries the audio endpoint device for its hardware-supported functions.
		/// </summary>
		/// <param name="hardwareSupportMask">A hardware support mask that indicates the capabilities of the endpoint.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int QueryHardwareSupport(
			[Out][MarshalAs(UnmanagedType.U4)] out UInt32 hardwareSupportMask);

		/// <summary>
		/// Gets the volume range of the audio stream, in decibels.
		/// </summary>
		/// <param name="volumeMin">The minimum volume level in decibels.</param>
		/// <param name="volumeMax">The maximum volume level in decibels.</param>
		/// <param name="volumeStep">The volume increment level in decibels.</param>
		/// <returns>An HRESULT code indicating whether the operation passed of failed.</returns>
		[PreserveSig]
		int GetVolumeRange(
			[Out][MarshalAs(UnmanagedType.R4)] out float volumeMin,
			[Out][MarshalAs(UnmanagedType.R4)] out float volumeMax,
			[Out][MarshalAs(UnmanagedType.R4)] out float volumeStep);
	}

	#endregion
}