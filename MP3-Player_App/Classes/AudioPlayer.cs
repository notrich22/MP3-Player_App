using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Input;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.Bass.AddOn.Tags;
using Un4seen.Bass.Misc;

namespace MP3_Player_App
{
    public class AudioPlayer
    {
        private int streamHandle;
        private string[] audioFileExtensions = { ".mp3", ".wav", ".flac" };
        private string currentDir;
        private TAG_INFO tagInfo;
        private bool isPlaying = false;
        private float volume = 0.75f;
        private List<string> filePaths;
        private int currentIndex = -1;

        public event EventHandler<bool> PlaybackStateChanged;
        public List<string> Playlist
        {
            get { return filePaths; }
            private set { filePaths = value; }
        }
        public int stream
        {
            get { return streamHandle; }
        }
        private TimeSpan pauseTime;
        public TimeSpan CurrentTime
        {
            get
            {
                if (!IsPlaying)
                {
                    return pauseTime;
                }
                if (streamHandle != 0 && Bass.BASS_ChannelIsActive(streamHandle) == BASSActive.BASS_ACTIVE_PLAYING)
                {
                    long position = Bass.BASS_ChannelGetPosition(streamHandle);
                    double seconds = Bass.BASS_ChannelBytes2Seconds(streamHandle, position);
                    return TimeSpan.FromSeconds(seconds);
                }
                return TimeSpan.Zero;
            }
            set
            {
                if (streamHandle != 0)
                {
                    long bytes = Bass.BASS_ChannelSeconds2Bytes(streamHandle, value.TotalSeconds);
                    Bass.BASS_ChannelSetPosition(streamHandle, bytes);
                }
            }
        }

        public TimeSpan TotalTime
        {
            get
            {
                if (IsSelected)
                {
                    long length = Bass.BASS_ChannelGetLength(streamHandle);
                    double seconds = Bass.BASS_ChannelBytes2Seconds(streamHandle, length);
                    return TimeSpan.FromSeconds(seconds);
                }
                return TimeSpan.Zero;
            }
        }

        public bool IsPlaying
        {
            get
            {
                return isPlaying;
            }
            set
            {
                isPlaying = value;
            }
        }

        public int SongIndex
        {
            get { return currentIndex; }
        }

        public bool IsSelected
        {
            get
            {
                return currentIndex >= 0 && currentIndex < Playlist.Count;
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
                    if (streamHandle != 0)
                    {
                        Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, volume);
                    }
                }
            }
        }

        public string selectedFilePath { get; set; }

        public TAG_INFO TagInfo
        {
            get
            {
                if (IsSelected)
                {
                    return tagInfo;
                }
                return null;
            }
        }


        public AudioPlayer()
        {
            BassNet.Registration("aleksvasilyev22@gmail.com", "2X351225152222");
            if (!Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero))
            {
                throw new ApplicationException("BASS initialization failed");
            }

            Playlist = new List<string>();
            string baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Music");
            if (!Directory.Exists(baseFolder))
            {
                Directory.CreateDirectory(baseFolder);
            }
            tagInfo = new TAG_INFO();
            AddFolderToPlaylist(baseFolder);
            if (Playlist.Count > 0)
            {
                currentIndex = 0;
                selectedFilePath = Playlist[currentIndex];
            }
        }

        public void OpenSelectedFile()
        {
            if (IsSelected)
            {
                CloseStream();

                streamHandle = Bass.BASS_StreamCreateFile(selectedFilePath, 0, 0, BASSFlag.BASS_DEFAULT);
                if (streamHandle != 0)
                {
                    Bass.BASS_ChannelSetAttribute(streamHandle, BASSAttribute.BASS_ATTRIB_VOL, volume);
                    tagInfo = BassTags.BASS_TAG_GetFromFile(selectedFilePath);
                }
            }
        }

        public void AddFolderToPlaylist(string folderPath)
        {
            foreach (string filePath in Directory.GetFiles(folderPath))
            {
                if (audioFileExtensions.Contains(Path.GetExtension(filePath), StringComparer.OrdinalIgnoreCase))
                {
                    if (!Playlist.Contains(filePath, StringComparer.OrdinalIgnoreCase))
                    {
                        Playlist.Add(filePath);
                    }
                }
            }
        }

        public void Play()
        {
            if (IsSelected)
            {
                if (streamHandle != 0)
                {
                    Bass.BASS_ChannelPlay(streamHandle, false);
                    isPlaying = true;
                }
            }
        }

        public void Pause()
        {
            if (IsSelected)
            {
                if (streamHandle != 0)
                {
                    long position = Bass.BASS_ChannelGetPosition(streamHandle);
                    double seconds = Bass.BASS_ChannelBytes2Seconds(streamHandle, position);
                    pauseTime = TimeSpan.FromSeconds(seconds);
                    Bass.BASS_ChannelPause(streamHandle);
                    isPlaying = false;
                }
            }
        }

        public void Stop()
        {
            if (IsSelected)
            {
                CloseStream();
                OpenSelectedFile();
                isPlaying = false;
                pauseTime = TimeSpan.Zero;
            }
        }

        public void Next()
        {
            if (currentIndex < Playlist.Count - 1)
            {
                currentIndex++;
                selectedFilePath = Playlist[currentIndex];
                OpenSelectedFile();
            }
        }

        public void Previous()
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                selectedFilePath = Playlist[currentIndex];
                OpenSelectedFile();
            }
        }

        private void CloseStream()
        {
            if (streamHandle != 0)
            {
                Bass.BASS_ChannelStop(streamHandle);
                Bass.BASS_StreamFree(streamHandle);
                streamHandle = 0;
            }
        }
    }
}
