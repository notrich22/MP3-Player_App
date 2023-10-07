using System;
using System.Windows;
using NAudio.Wave;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;
using System.Windows.Controls;
using System.Drawing;
using NAudio.Wave;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NAudio.Dsp;
using System.Windows.Data;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Timers;
using System.Diagnostics;

namespace MP3_Player_App
{
    public partial class MainWindow : Window
    {
        AudioPlayer audioPlayer;
        public float Volume
        {
            get { return audioPlayer.Volume; }
            set { audioPlayer.Volume = value; }
        }
        public string SongTitle
        {
            get { return audioPlayer.SongTitle; }
        }
        public string Artist
        {
            get { return audioPlayer.Artist; }
        }
        public bool IsSelected
        {
            get
            {
                return audioPlayer.IsSelected;
            }
        }
        private DispatcherTimer timer;

        public MainWindow()
        {
            
            audioPlayer = new AudioPlayer();
            InitializeComponent();
            DataContext = this;
            UpdateListBox();
            TrackSlider.Minimum = 0;
            InitializeTimer();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.01);
            timer.Tick += async (sender, e) =>
            {
                await UpdateGUIAsync(sender, e);
            };

            timer.Start();
        }
        private async Task UpdateGUIAsync(object sender, EventArgs e)
        {
            if (IsSelected)
            {
                ControlPanel.IsEnabled = true;
            }
            else
            {
                ControlPanel.IsEnabled = false;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TrackSlider.Minimum = 0;
        }
        private void UpdateListBox()
        {
            TrackListBox.Items.Clear();
            foreach (var item in audioPlayer.Playlist)
            {
                TrackListBox.Items.Add(Path.GetFileName(item));
            }

        }

        private string UpdateTrackBitrate(string filePath)
        {
            if (audioPlayer.getTrackBitrate() != null)
            {
                return "Битрейт трека: " + audioPlayer.getTrackBitrate() + " kbps";
            }
            return "Не удалось получить битрейт";
        }

        private async void InitializeTimer()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (IsSelected)
                        {
                            TrackSlider.Value = audioPlayer.CurrentTime.TotalSeconds;
                            TrackSlider.Maximum = audioPlayer.getTrackTotalTime();
                        }
                    });
                }
            });
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (audioPlayer.IsPlaying)
            {
                await audioPlayer.PauseAsync();
                audioPlayer.IsPlaying = false;
            }
            else
            {
                
                BitrateTextBlock.Visibility = Visibility.Visible;
                BitrateTextBlock.Text = UpdateTrackBitrate(audioPlayer.selectedFilePath);
                await audioPlayer.PlayAsync();
                audioPlayer.IsPlaying = true;
            }
        }


        private void TrackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double newPosition = TrackSlider.Value;
            audioPlayer.CurrentTime = TimeSpan.FromSeconds(newPosition);
        }

        private void SelectDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "Выберите папку с аудиофайлами";
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

                DialogResult result = folderBrowserDialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(folderBrowserDialog.SelectedPath))
                {
                    string selectedFolder = folderBrowserDialog.SelectedPath;
                    audioPlayer.AddFolderToPlaylist(selectedFolder);
                    UpdateListBox();
                }
            }
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            audioPlayer.Stop();
        }
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            audioPlayer.Volume = (float)VolumeSlider.Value;
        }
        private async void TrackListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TrackSlider.IsEnabled = true;
            await audioPlayer.StopAsync();
            if (TrackListBox.SelectedItem != null)
            {
                foreach (var filePath in audioPlayer.Playlist)
                {
                    if (Path.GetFileName(filePath) == TrackListBox.SelectedItem.ToString()) {
                        audioPlayer.selectedFilePath = filePath;
                        audioPlayer.OpenSelectedFile();
                    }
                }
            }
        }

    }
}
