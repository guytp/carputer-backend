using System;

namespace CarMediaServer
{
	/// <summary>
	/// These event arguments indicate a mounted device that has been involved in an event.
	/// </summary>
	public class MountedDeviceEventArgs : EventArgs
	{
		#region Properties
		/// <summary>
		/// Gets the device that these event arguments represent.
		/// </summary>
		public MountedDevice Device { get; private set; }
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new instance fo this class.
		/// </summary>
		/// <param name='device'>
		/// The device that these event arguments represent.
		/// </param>
		public MountedDeviceEventArgs (MountedDevice device)
		{
			Device = device;
		}
		#endregion
	}
}