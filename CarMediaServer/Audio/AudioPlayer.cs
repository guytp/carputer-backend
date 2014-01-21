using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace CarMediaServer
{
	/// <summary>
	/// The audio player class is responsible for playing MP3s and managing their current playlist.
	/// </summary>
	public class AudioPlayer
	{
		#region Declarations
		/// <summary>
		/// Defines the playlist currently in the player if in non shuffled mode.
		/// </summary>
		public List<AudioFile> _playlist = new List<AudioFile>();

		/// <summary>
		/// Defines the playlist order to use for shuffled playlists.
		/// </summary>
		public List<int> _shuffledPlaylistOrder = new List<int>();

		/// <summary>
		/// Defines the current position within the playlist.
		/// </summary>
		public int _playlistLocation = 0;

		/// <summary>
		/// Defines the process that is used to currently playback MP3s.
		/// </summary>
		public Process _process;

		/// <summary>
		/// Defines whether or not a stop command has been sent manually.
		/// </summary>
		private bool _expectingStop = false;

		/// <summary>
		/// Defines an object to use for thread locking the queue so that we only send commands when
		/// the last one has been processed.
		/// </summary>
		private object _locker = new object();

		/// <summary>
		/// Defines the expected response prefix in order to mark as ready for next command.
		/// </summary>
		private string _expectedResponsePrefix;

		/// <summary>
		/// Defines whether or not we are ready for the next command.
		/// </summary>
		private bool _readyForNextCommand;

		/// <summary>
		/// Defines whether we need to retry the last command.
		/// </summary>
		private bool _retryLastCommand;

		/// <summary>
		/// Defines whether we are shuffling tracks at random.
		/// </summary>
		private bool _isShuffle;

		/// <summary>
		/// Defines whether we are repeating tracks at random.
		/// </summary>
		private bool _isRepeat;
		#endregion

		#region Properties
		/// <summary>
		/// Gets the current playlist for this player.
		/// </summary>
		public AudioFile[] Playlist { get { lock (_playlist) return _playlist.ToArray(); } }

		/// <summary>
		/// Gets the current playlist for this player.
		/// </summary>
		public int PlaylistLocation{ get { lock (_playlist) return _playlistLocation; } }

		/// <summary>
		/// Gets whehter or not MPG123 is running.
		/// </summary>
		public bool IsAudioPlayerProcessRunning { get { return _process != null && _process.StartTime.Year > 2010 && !_process.HasExited;} }

		/// <summary>
		/// Defines whether or not a track is currently playing/paused (true) or not (false).
		/// </summary>
		public bool IsPlaying { get; private set; }

		/// <summary>
		/// Defines whether or not a track is currently paused.
		/// </summary>
		public bool IsPaused { get; private set; }

		/// <summary>
		/// Defines the progress in seconds through the current track.
		/// </summary>
		public int Progress { get; private set; }

		/// <summary>
		/// Defines the total duration in seconds of the current track.
		/// </summary>
		public int Duration { get; private set; }
		#endregion

		/// <summary>
		/// Crate a new instnace of this class.
		/// </summary>
		public AudioPlayer()
		{
			// Start-up the process monitoring and writing threads
			new Thread(ProcessDataReadingThread) { IsBackground = true, Name = "Mpg123 Parser" }.Start();
			new Thread(ProcessCommandWritingThread) { IsBackground = true, Name = "Mpg123 Writer" }.Start();
			new Thread(StatusBroadcastThread) { IsBackground = true, Name = "Audio Player Status Broadcaster" }.Start();
		}

		#region Playlist Control
		/// <summary>
		/// Adds to an audio file to the current playlist.
		/// </summary>
		/// <param name='audioFile'>
		/// The file to add to the playlist.
		/// </param>
		public void PlaylistAdd(AudioFile audioFile)
		{
			PlaylistAdd(new[] { audioFile });
		}
		
		/// <summary>
		/// Adds audio files to the current playlist.
		/// </summary>
		/// <param name='audioFiles'>
		/// The files to add to the playlist.
		/// </param>
		public void PlaylistAdd(IEnumerable<AudioFile> audioFiles)
		{
			bool requiresPlay;
			lock (_playlist)
			{
				requiresPlay = _playlist.Count == 0;
				_playlist.AddRange(audioFiles);
				int lookingForIndex = _isShuffle ? _shuffledPlaylistOrder[_playlistLocation] : _playlistLocation;
				_shuffledPlaylistOrder.Clear();
				Random random = new Random();
				while(_shuffledPlaylistOrder.Count < _playlist.Count)
				{
					int next = random.Next(0, _playlist.Count);
					if (_shuffledPlaylistOrder.Contains(next))
						continue;
					if ((_isShuffle) && (next == lookingForIndex))
						_playlistLocation = _shuffledPlaylistOrder.Count;
					_shuffledPlaylistOrder.Add(next);
				}
			}
			Logger.Info("Added " + audioFiles.Count() + " to MP3 playback queue");
			if (requiresPlay)
				Play();
		}

		/// <summary>
		/// Clears the current playlist.
		/// </summary>
		public void PlaylistClear()
		{
			bool requiresStop;
			lock (_playlist)
			{
				requiresStop = _playlist.Count > 0 || IsPlaying;
				_playlist.Clear();
				_shuffledPlaylistOrder.Clear();
				_playlistLocation = 0;
			}
			Logger.Info("cleared playlist queue");
			if (requiresStop)
				Stop();
		}
		#endregion

		#region Playback Control
		/// <summary>
		/// Play the current playlist and launches the MP3 player if it is not already running
		/// in a background process.  This does nothing if there is no playlist.
		/// </summary>
		public void Play()
		{
			// Get a handle to the playlist and return if we don't have one
			AudioFile[] playlist = Playlist;
			if (playlist.Length == 0)
				return;

			// Determine current location in the playlist and get a handle to that item
			int playlistLocation = _isShuffle ? _shuffledPlaylistOrder[PlaylistLocation] : PlaylistLocation;
			AudioFile playbackFile = playlist[playlistLocation];
			SendProcessLoadFileAndPlay(Controller.MountManager.GetMountedPath(playbackFile.DeviceUuid) + playbackFile.RelativePath);
		}

		/// <summary>
		/// Stop the playback if in progress and also terminates the MP3 player if it is running in
		/// the background.
		/// </summary>
		public void Stop()
		{
			if (IsPlaying)
				SendProcessStop();
		}

		/// <summary>
		/// Pause playback from the audio player.
		/// </summary>
		public void TogglePlayPause()
		{
			if (IsPlaying)
				SendProcessTogglePlayPause();
			else
			{
				//StopAudioPlayerProcess();
				Play();
			}
		}

		/// <summary>
		/// Toggle the shuffle state.
		/// </summary>
		public void ToggleShuffle()
		{
			lock (_playlist)
			{
				if (_isShuffle)
					_playlistLocation = _shuffledPlaylistOrder[_playlistLocation];
				else
				{
					for (int i = 0; i < _shuffledPlaylistOrder.Count; i++)
					{
						if (_shuffledPlaylistOrder[i] == _playlistLocation)
						{
							_playlistLocation = i;
							break;
						}
					}
				}
				_isShuffle = !_isShuffle;
			}
		}

		/// <summary>
		/// Toggles the repeat state.
		/// </summary>
		public void ToggleRepeat()
		{
			_isRepeat = !_isRepeat;
		}

		/// <summary>
		/// Plays the specified file in playlist.
		/// </summary>
		public void PlayItemInPlaylist(int position)
		{
			bool sendPlay = false;
			lock (_playlist)
			{
				if (((position < 0) || (position >= _playlist.Count)) && (!_isRepeat))
					return;
				else if (position < 0)
					_playlistLocation = _playlist.Count - 1;
				else if (position >= _playlist.Count)
					_playlistLocation = 0;
				else
					_playlistLocation = position;
				sendPlay = true;
			}
			if (sendPlay)
				Play();
		}

		/// <summary>
		/// Plays the next file in playlist or if there is not one does nothing.
		/// </summary>
		public void PlayNextInPlaylist()
		{
			bool sendPlay = false;
			lock (_playlist)
			{
				if (_playlistLocation >= _playlist.Count - 1)
					if (_isRepeat)
						_playlistLocation = 0;
					else
						return;
				else
					_playlistLocation++;
				sendPlay = true;
			}
			if (sendPlay)
				Play();
		}

		/// <summary>
		/// Plays the previous file in playlist or if there is not one does nothing.
		/// </summary>
		public void PlayPreviousInPlaylist()
		{
			bool sendPlay = false;
			lock (_playlist)
			{
				if (_playlistLocation <= 0)
					if (_isRepeat)
						_playlistLocation = _playlist.Count - 1;
					else
						return;
				else
					_playlistLocation--;
				sendPlay = true;
			}
			if (sendPlay)
				Play();
		}

		/// <summary>
		/// Jumps to specified offset in seconds within the track or return if nothing is playing.
		/// </summary>
		/// <param name='offset'>
		/// The offset in seconds to jump to.
		/// </param>
		public void JumpToSecondsOffset(int offset)
		{
			// Return if not playing
			if (!IsPlaying)
				return;

			SendProcessJumpToOffset(offset);
		}
		#endregion

		#region Mpg123 Remote Control
		/// <summary>
		/// Starts the MPG123 process and attaches to it.
		/// </summary>
		private void StartAudioPlayerProcess()
		{
			// If existing process try to kill it
			if (_process != null)
				StopAudioPlayerProcess(true);

			// Setup defaults
			Logger.Info("Starting MPG123 process");
			_expectingStop = false;
			_readyForNextCommand = false;
			_expectedResponsePrefix = "@R";
			IsPlaying = false;
			IsPaused = false;
			Duration = 0;
			Progress = 0;

			// Create the process
			ProcessStartInfo processStartInfo = new ProcessStartInfo
			{
				Arguments = "-R",
				CreateNoWindow = true,
				RedirectStandardInput = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				WindowStyle = ProcessWindowStyle.Hidden,
				FileName = "mpg123"
			};
			Process process = new Process();
			process.StartInfo = processStartInfo;
			process.EnableRaisingEvents = true;
			process.OutputDataReceived += HandleOutputDataReceived;
			process.Start();
			process.BeginOutputReadLine();
			_process = process;
		}

		/// <summary>
		/// Stops the MPG123 process and detaches from it.
		/// </summary>
		private void StopAudioPlayerProcess(bool force = false)
		{
			// Return if not running
			if (((!force) && (!IsAudioPlayerProcessRunning)) || (_process == null))
				return;

			// Forcibly kill the process
			Logger.Info("Stopping MPG123 process");
			_process.OutputDataReceived -= HandleOutputDataReceived;
			_process.EnableRaisingEvents = false;
			_process.CancelOutputRead();
			try
			{
				_process.Kill();
			}
			catch
			{
			}
			try
			{
				_process.StandardOutput.Close();
				_process.StandardOutput.Dispose();
			}
			catch
			{
			}
			try
			{
				_process.StandardInput.Close();
				_process.StandardInput.Dispose();
			}
			catch
			{
			}
			try
			{
				_process.Dispose();
			}
			catch
			{
			}
			_process = null;
		}

		/// <summary>
		/// Sends MPG123 a request to load and play a file.
		/// </summary>
		/// <param name='file'>
		/// The full path of the file to load.
		/// </param>
		private void SendProcessLoadFileAndPlay(string file)
		{
			// Start process if not running
			if (!IsAudioPlayerProcessRunning)
				StartAudioPlayerProcess();

			// Write to the process the load and start playing command
			Logger.Info("Sending MPG123 load for " + file);
			OutputToProcess("S");
			if (File.Exists(file))
				OutputToProcess("L " + file);
			else
			{
				Logger.Error("Skipping load for this song as unable to access file in playlist: " + file);
				PlayNextInPlaylist();
			}
		}

		/// <summary>
		/// Sends MPG123 a request to jump to the specified offset in seconds.
		/// </summary>
		/// <param name='offset'>
		/// The offset in seconds to jump to.
		/// </param>
		private void SendProcessJumpToOffset (int offset)
		{
			// Start process if not running
			if (!IsAudioPlayerProcessRunning)
				StartAudioPlayerProcess();

			// Write to the process
			Logger.Info("Sending MPG123 JUMP");
			OutputToProcess("J " + offset + "s");

		}

		/// <summary>
		/// Sends MPG123 a request to stop playingback.
		/// </summary>
		private void SendProcessStop()
		{
			// Start process if not running
			if (!IsAudioPlayerProcessRunning)
				StartAudioPlayerProcess();

			// Write to the process the load and start playing command
			Logger.Info("Sending MPG123 stop");
			OutputToProcess("S");
		}

		/// <summary>
		/// Sends MPG123 a request to toggle play/pause.
		/// </summary>
		private void SendProcessTogglePlayPause()
		{
			// Start process if not running
			if (!IsAudioPlayerProcessRunning)
				StartAudioPlayerProcess();

			// Write to the process the load and start playing command
			Logger.Info("Sending MPG123 play/pause");
			OutputToProcess("P");
		}

		private List<string> _inputQueue = new List<string>();

		private List<string> _outputQueue = new List<string>();

		/// <summary>
		/// Adds a command to the output queue to send to the process.
		/// </summary>
		/// <param name='command'>
		/// The command to send.
		/// </param>
		private void OutputToProcess(string command)
		{
			lock (_outputQueue)
			{
				// Remove any other jump commands
				if (command.StartsWith("J "))
				{
					string[] jumpCommands = _outputQueue.Where(cmd => cmd.StartsWith("J ")).ToArray();
					foreach (string jumpCommand in jumpCommands)
						_outputQueue.Remove(jumpCommand);
				}
				_outputQueue.Add(command);
			}
		}

		private void ProcessCommandWritingThread ()
		{
			while (true) 
			{
				// Get a handle to all commands
				string[] queue;
				lock (_outputQueue) {
					queue = _outputQueue.ToArray ();
					_outputQueue.Clear ();
				}
				if (queue.Length < 1) {
					Thread.Sleep (50);
					continue;
				}

				// Determine index of the final jump command as we only send 1
				int indexOfLastJump = -1;
				for (int i = 0; i < queue.Length; i++)
					if (queue[i].StartsWith("J "))
						indexOfLastJump = i;

				// Loop and attempt to write the command
				int iCommand = -1;
				foreach (string command in queue) {
					while (true)
					{
						// Wait for process to be available
						iCommand++;
						while ((!IsAudioPlayerProcessRunning) || (!_readyForNextCommand)) {
							Thread.Sleep (50);
						}

						// If have a jump command and it is not the last one, continue
						if ((command.StartsWith("J ")) && (iCommand != indexOfLastJump))
							continue;

						// Register if expecting stop
						if (command == "S")
							_expectingStop = true;

						// Write the data
						Logger.Info ("Sending mpg123 " + command);
						string[] parts = command.Split(new char[] { ' ' });
						lock (_locker)
						{
							_expectedResponsePrefix = parts[0] == "S" ? "@P" : parts[0] == "L" ? "@S" : ("@" + parts[0]).ToUpper();
							_readyForNextCommand = false;
							_retryLastCommand = false;
						}
						_process.StandardInput.WriteLine(command);

						// If we become ready for next command and retry last command is false we break out
						// otherwise we do the same command again
						while (!_readyForNextCommand)
							Thread.Sleep(50);
						if (!_retryLastCommand)
							break;
					}
				}
			}
		}

		/// <summary>
		/// Handles the output data received from MPG123 and act accordingly.
		/// </summary>
		/// <param name='sender'>
		/// The event sender.
		/// </param>
		/// <param name='e'>
		/// The event arguments.
		/// </param>
		private void HandleOutputDataReceived (object sender, DataReceivedEventArgs e)
		{
			lock (_inputQueue)
				_inputQueue.Add(e.Data);
		}

		/// <summary>
		/// This method processes the queue of data we have read from the mpg123 process.
		/// </summary>
		private void ProcessDataReadingThread()
		{
			// Read an item from the queue
			while (true) {
				string[] allItems;
				lock (_inputQueue)
				{
					allItems = _inputQueue.ToArray();
					_inputQueue.Clear();
				}
				if (allItems.Length == 0)
				{
					Thread.Sleep(10);
					continue;
				}

				foreach (string data in allItems)
				{
					lock (_locker)
					{
						if ((!_readyForNextCommand) && ((string.IsNullOrEmpty(_expectedResponsePrefix)) || (data.StartsWith(_expectedResponsePrefix))))
						    _readyForNextCommand = true;
					}
					// Parse out components
					string[] components = data.Split (new char[] { ' ' }, 2);
					if (components [0] == "@R") {
						Logger.Info ("Sending MPG123 maximum volume");
						OutputToProcess("V 100");
					} else if ((components [0] == "@P") && (components [1] == "0")) {
						// Setup values
						IsPlaying = false;
						IsPaused = false;
						Duration = 0;
						Progress = 0;

						// If we didn't manually send a stop, start the next track
						if (!_expectingStop)
							PlayNextInPlaylist ();
						else
							_expectingStop = false;
					} else if ((components [0] == "@P") && ((components [1] == "1") || (components [1] == "2"))) {
						IsPlaying = true;
						IsPaused = (components [1] == "1");
					} else if (components [0] == "@E")
					{
						// On error we retry the last one
						Logger.Error("Error response received from MPG123: " + components[1]);
						lock (_locker)
						{
							_retryLastCommand = true;
							_readyForNextCommand = true;
						}
					}
					else if (components [0] == "@F") {
						// Parse out time information
						string[] currentlyPlayingInformation = components [1].Split (new char[] { ' ' });
						double progress;
						double remaining;
						if (!double.TryParse (currentlyPlayingInformation [2], out progress))
							progress = 0;
						if (!double.TryParse (currentlyPlayingInformation [3], out remaining))
							remaining = 0;
						double total = progress + remaining;

						// Store these as information
						Duration = (int)Math.Round (total);
						Progress = (int)Math.Round (progress);
					} else if ((components [0] != "@S") && (components [0] != "@I") && (components [0] != "@V"))
						Logger.Warn ("Unknown data from mpg123" + data);

					if (components [0] != "@F")
						Logger.Info(data);
				}
			}
		}
		#endregion

		#region Helper Methods
		/// <summary>
		/// Updates the status of this object and if it has changed since it was last called
		/// send out a network notification to all connected clients.
		/// </summary>
		private void StatusBroadcastThread ()
		{
			while (true)
			{
				// Determine current state
				AudioFile[] playlist = Playlist;
				int[] playlistIntegers = new int[playlist.Length];
				for (int i = 0; i < playlistIntegers.Length; i++)
					playlistIntegers [i] = playlist [i].AudioFileId;
				AudioStatusNotification statusNotification = new AudioStatusNotification
				{
					Duration = Duration,
					IsPaused = IsPaused,
					IsPlaying = IsPlaying,
					Playlist = playlistIntegers,
					PlaylistPosition = _isShuffle ? _shuffledPlaylistOrder[PlaylistLocation] : PlaylistLocation,
					Position = Progress,
					IsRepeatAll = _isRepeat,
					IsShuffle = _isShuffle,
					CanMoveNext = _isRepeat || _playlistLocation < _playlist.Count - 1,
					CanMovePrevious = _isRepeat || _playlistLocation > 0 || Progress >= 5,
				};

				// Set the new status and send it out
				if (Controller.NotificationNetworkServer == null)
				{
					Thread.Sleep(500);
					continue;
				}
				Logger.Debug("Sending status of item " + statusNotification.PlaylistPosition + " at " + statusNotification.Position + " " + statusNotification.Duration);
				Controller.NotificationNetworkServer.SendNotification(statusNotification);

				// Wait for next loop
				Thread.Sleep(200);
			}
		}
		#endregion
	}
}