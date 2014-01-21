using System;
using Newtonsoft.Json;

namespace CarMediaServer
{
	/// <summary>
	/// This class represents a single audio file that can be played.
	/// </summary>
	[DataObject("AudioFile", "Artist ASC, Album ASC, TrackNumber ASC", "AudioFileId")]
	public class AudioFile : DbObject
	{
		#region Properties
		/// <summary>
		/// Gets or sets the primary key for this object stored in the database.
		/// </summary>
        [DataPropertyMapping]
		public int AudioFileId { get; set; }

		/// <summary>
		/// Gets or sets the artist for this audio file.
		/// </summary>
        [DataPropertyMapping]
		public string Artist { get; set; }
		
		/// <summary>
		/// Gets or sets the title of this audio file.
		/// </summary>
        [DataPropertyMapping]
		public string Title { get; set; }
		
		/// <summary>
		/// Gets or sets the track number of this audio file.
		/// </summary>
        [DataPropertyMapping]
		public int? TrackNumber { get; set; }
		
		/// <summary>
		/// Gets or sets the album name for this audio file.
		/// </summary>
        [DataPropertyMapping]
		public string Album { get; set; }
		
		/// <summary>
		/// Gets or sets the duration of this audio file.
		/// </summary>
		[JsonIgnore]
		public TimeSpan? Duration { get; set; }
		
		/// <summary>
		/// Gets or sets the duration of this audio file.
		/// </summary>
        [DataPropertyMapping("Duration")]
		public int? DurationSeconds {
			get {
				return !Duration.HasValue ? null : (int?)(int)Duration.Value.TotalSeconds;
			}
			set {
				Duration = !value.HasValue ? null : (TimeSpan?)TimeSpan.FromSeconds (value.Value);
			}
		}

		/// <summary>
		/// Gets or sets the device this audio file is contained on.
		/// </summary>
        [DataPropertyMapping]
		[JsonIgnore]
		public string DeviceUuid { get; set; }

		/// <summary>
		/// Gets or sets the relative path within this device that the file is located.
		/// </summary>
        [DataPropertyMapping]
		[JsonIgnore]
		public string RelativePath { get; set; }

		/// <summary>
		/// Gets or sets the time this file was last seen.
		/// </summary>
        [DataPropertyMapping]
		[JsonIgnore]
		public DateTime LastSeen { get; set; }
		#endregion

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents the current <see cref="CarMediaServer.AudioFile"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents the current <see cref="CarMediaServer.AudioFile"/>.
		/// </returns>
		public override string ToString ()
		{
			return string.Format (
				"--------------------------------------------------------------------------------" + Environment.NewLine +
				"Artist        = {0}" + Environment.NewLine +
				"Title         = {1}" + Environment.NewLine +
				"Track         = {2}" + Environment.NewLine +
				"Album         = {3}" + Environment.NewLine +
				"Duration      = {4}" + Environment.NewLine +
				"DeviceUuid    = {5}" + Environment.NewLine +
				"RelativePath  = {6}" + Environment.NewLine +
				"LastSeen      = {7}" + Environment.NewLine +
				"--------------------------------------------------------------------------------", Artist, Title, TrackNumber, Album, Duration, DeviceUuid, RelativePath, LastSeen);
		}
	}
}