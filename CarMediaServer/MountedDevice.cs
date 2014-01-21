using System;

namespace CarMediaServer
{
	/// <summary>
	/// This class represents a simple mounted device and provides details about it.
	/// </summary>
	public class MountedDevice
	{
		#region Properties
		/// <summary>
		/// Gets the real name of this device.
		/// </summary>
		public string Device { get; private set; }
		
		/// <summary>
		/// Gets the path that this device is mounted at.
		/// </summary>
		public string MountPath { get; private set; }

		/// <summary>
		/// Gets the UUID for this device.
		/// </summary>
		public string Uuid { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Create a new instance of this class.
		/// </summary>
		/// <param name='device'>
		/// The real name of this device.
		/// </param>
		/// <param name='mountPath'>
		/// The path that this device is mounted at.
		/// </param>
		/// <param name='uuid'>
		/// The UUID for this device.
		/// </param>
		public MountedDevice(string device, string mountPath, string uuid)
		{
			Device = device;
			MountPath = mountPath;
			Uuid = uuid;
		}
		#endregion
	}
}