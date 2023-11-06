using System;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Un4seen.Bass.Misc;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace MP3_Player_App
{
    public partial class MainWindow : Window
    {
        AudioPlayer audioPlayer;
        Visuals visuals = new Visuals();
        private BitmapImage bitmapImage;
        public float Volume
        {
            get { return audioPlayer.Volume; }
            set { audioPlayer.Volume = value; }
        }
        public string SongTitle
        {
            get {
                if (audioPlayer.TagInfo.title != null)
                    return audioPlayer.TagInfo.title;
                else
                    return "";
            }
        }
        public BitmapImage BitmapVisualImage
        {
            get
            {
                return bitmapImage;
            }
        }
        public string Artist
        {
            get {
                if (audioPlayer.TagInfo.artist != null)
                    return audioPlayer.TagInfo.artist;
                else
                    return "";
            }
        }
        public bool IsSelected
        {
            get
            {
                return audioPlayer.IsSelected;
            }
        }
        private bool isUpdatingPosition = false;
        private DispatcherTimer timer;

        public MainWindow()
        {
            bitmapImage = new BitmapImage();
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
                SongTitleText.Text = SongTitle;
                CurrentTimeText.Text = string.Format("{0:D2}:{1:D2}", (int)audioPlayer.CurrentTime.TotalMinutes, audioPlayer.CurrentTime.Seconds);
                TotalTimeText.Text = string.Format("{0:D2}:{1:D2}", (int)audioPlayer.TotalTime.TotalMinutes, audioPlayer.TotalTime.Seconds);
                ControlPanel.IsEnabled = true;
            }
            else
            {
                ControlPanel.IsEnabled = false;
            }
            if(audioPlayer.IsPlaying)
            {
                Bitmap imgVis = visuals.CreateSpectrumLine(audioPlayer.stream, 600, 300, 
                    System.Drawing.Color.Yellow, System.Drawing.Color.Red,
                        System.Drawing.Color.Empty, 1, 1, false, false, false);
                if(imgVis != null)
                    AudioVisualizer.Source = ConvertBitmapToBitmapImage(imgVis);
            }
            else
            {
                AudioVisualizer.Source = null;
            }

        }
        public BitmapImage ConvertBitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Bmp); // Сохраняем битмап в поток с форматом BMP
                memoryStream.Position = 0; // Сбрасываем позицию потока в начало

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
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
            if (String.IsNullOrEmpty(audioPlayer.TagInfo.bitrate.ToString()))
            {
                return "Битрейт трека: " + audioPlayer.TagInfo.bitrate + " kbps";
            }
            return "Не удалось получить битрейт";
        }

        private async void InitializeTimer()
        {
            await Task.Run(() =>
            {
                try { 
                    while (true)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (IsSelected)
                            {
                                TrackSlider.Value = audioPlayer.CurrentTime.TotalSeconds;
                                TrackSlider.Maximum = audioPlayer.TotalTime.TotalSeconds;
                            }
                        });
                    }
                }catch (Exception ex) { Debug.WriteLine(ex.ToString()); }
            });
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (audioPlayer.IsPlaying)
            {
                audioPlayer.Pause();
                audioPlayer.IsPlaying = false;
            }
            else
            {
                audioPlayer.Play();
                audioPlayer.IsPlaying = true;
            }
        }

        private void TrackSlider_DragStarted(object sender, MouseButtonEventArgs e)
        {
            isUpdatingPosition = true;
        }

        private void TrackSlider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            isUpdatingPosition = false;
        }
        private void TrackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double newPosition = TrackSlider.Value;
            if (isUpdatingPosition)
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
            audioPlayer.Previous();
            BitrateTextBlock.Text = UpdateTrackBitrate(audioPlayer.selectedFilePath);
            TrackListBox.SelectedIndex = audioPlayer.SongIndex;
        }
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            audioPlayer.Next();
            BitrateTextBlock.Text = UpdateTrackBitrate(audioPlayer.selectedFilePath);
            TrackListBox.SelectedIndex = audioPlayer.SongIndex;

        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            audioPlayer.Volume = (float)VolumeSlider.Value;
        }
        private async void TrackListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TrackSlider.IsEnabled = true;
            audioPlayer.Stop();
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
            BitrateTextBlock.Visibility = Visibility.Visible;
            BitrateTextBlock.Text = UpdateTrackBitrate(audioPlayer.selectedFilePath);
        }

    }
}
