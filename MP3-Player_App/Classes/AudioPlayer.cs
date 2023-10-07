using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace MP3_Player_App
{
    public class AudioPlayer
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFileReader;
        string[] audioFileExtensions = { ".mp3", ".wav", ".flac" };
        private string songTitle;
        private string artist;

        private bool isPlaying = false;
        private float volume = 0.75f;

        private List<string> filePaths;

        public event EventHandler<bool> PlaybackStateChanged;
        public List<string> Playlist { 
            get { return filePaths; }
            private set { filePaths = value; }
        }
        public TimeSpan CurrentTime { 
            get
            {
                if (audioFileReader != null && outputDevice != null && outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    return audioFileReader.CurrentTime;
                }
                else { return TimeSpan.Zero; }
            }
            set
            {
                if (audioFileReader != null)
                {
                    audioFileReader.CurrentTime = value;
                }
            }
        }
        public bool IsPlaying { 
            get {
                return isPlaying;
            }
            set
            {
                isPlaying = value;
            }
        }
        public bool IsSelected
        {
            get
            {
                return !String.IsNullOrEmpty(selectedFilePath);
            }
        }
        public float Volume
        {
            get { return volume; }
            set
            {
                if (volume != value)
                {
                    volume = value;
                    if (audioFileReader != null)
                    {
                        audioFileReader.Volume = volume;
                    }
                }
            }
        }
        public string selectedFilePath { get; set; }
        public string SongTitle
        {
            get {
                if (IsSelected)
                {
                    using (var mp3File = new Mp3FileReader(selectedFilePath))
                    {
                        if (mp3File != null && mp3File.Id3v2Tag != null)
                        {
                            // Пытаемся получить название песни из метаданных
                            if (!string.IsNullOrEmpty(mp3File.Id3v2Tag.ToString()))
                            {
                                return mp3File.Id3v2Tag.ToString();
                            }
                        }
                        return Path.GetFileNameWithoutExtension(selectedFilePath);
                    }
                }
                return "";
            }
            
        }
        public string Artist
        {
            get { return artist; }
            
        }
        public AudioPlayer()
        {
            outputDevice = new WaveOutEvent();
            Playlist = new List<string>();
            AddFolderToPlaylist(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Music"));
            if(Playlist.Count > 0) {
                //selectedFilePath = Playlist[0];
            }
            OpenSelectedFile();
        }

        public double getTrackTotalTime()
        {
            if (IsSelected) { 
                return audioFileReader.TotalTime.TotalSeconds;
            }
            return TimeSpan.Zero.TotalSeconds;
        }
        public int? getTrackBitrate()
        {
            using (var reader = new Mp3FileReader(selectedFilePath))
            {
                if (reader != null)
                {
                    return reader.Mp3WaveFormat.AverageBytesPerSecond * 8 / 1000 + 1;
                }
            }
            return null;
        }
        public void OpenSelectedFile()
        {
            if(selectedFilePath != null) { 
                audioFileReader = new AudioFileReader(selectedFilePath);
                audioFileReader.Volume = Volume;
                outputDevice.Init(audioFileReader);
            }
        }
        public void AddFolderToPlaylist(string folderPath)
        {
            foreach (string filePath in Directory.GetFiles(folderPath)) {
                if (audioFileExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase))
                {
                    Playlist.Add(filePath);
                }
            }
        }

        public void Play()
        {
            if (outputDevice != null && audioFileReader != null)
            {
                outputDevice.Play();
                isPlaying = true;
                PlaybackStateChanged?.Invoke(this, true);
            }
        }

        public void Pause()
        {
            if (outputDevice != null)
            {
                outputDevice.Pause();
                isPlaying = false;
                PlaybackStateChanged?.Invoke(this, false);
            }
        }

        public void Stop()
        {
            if (outputDevice != null && isPlaying)
            {
                outputDevice.Stop();
                isPlaying = false;
                PlaybackStateChanged?.Invoke(this, false);
            }
        }


        public async Task PlayAsync()
        {
            await Task.Run(() => Play());
        }

        public async Task PauseAsync()
        {
            await Task.Run(() => Pause());
        }

        public async Task StopAsync()
        {
            await Task.Run(() => Stop());
        }
    }
}
