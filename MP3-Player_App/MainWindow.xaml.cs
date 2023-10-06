using System;
using System.Windows;
using NAudio.Wave;
using NAudio.Lame;
using System.Windows.Threading;
using System.Windows.Controls;
using NAudio.WaveFormRenderer;
using System.Media;

namespace MP3_Player_App
{
    public partial class MainWindow : Window
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFileReader;
        private bool isPlaying = false;
        private string filePath = "D:\\Downloads\\Danzig - Mother .mp3";
        private float volume = 1.0f;
        private DispatcherTimer timer;
        public float Volume
        {
            get { return volume; }
            set
            {
                if (value < 0.0f)
                    volume = 0.0f;
                else if (value > 1.0f)
                    volume = 1.0f;
                else
                    volume = value;

                if (audioFileReader != null)
                {
                    audioFileReader.Volume = volume;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Volume = 0.75f;
            TrackPositionSlider.Minimum = 0;
            TrackPositionSlider.Value = 0; // Устанавливаем начальное значение в начало трека.

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TrackPositionSlider.Minimum = 0;
            TrackPositionSlider.Maximum = audioFileReader.TotalTime.TotalSeconds;

            InitializeTimer();
        }


        private void BitrateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (audioFileReader != null)
            {
                TrackPositionSlider.Value = audioFileReader.CurrentTime.TotalSeconds;
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                if (outputDevice != null)
                {
                    outputDevice.Pause();
                    isPlaying = false;
                }
            }
            else
            {
                if (outputDevice == null)
                {
                    outputDevice = new WaveOutEvent();
                    audioFileReader = new AudioFileReader(filePath);
                    outputDevice.Init(audioFileReader);
                    TrackPositionSlider.Maximum = audioFileReader.TotalTime.TotalSeconds; // Обновляем максимальное значение ползунка при загрузке трека.
                }
                outputDevice.Play();
                isPlaying = true;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                outputDevice = null;
                isPlaying = false;
            }
        }

        private void TrackPositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double newPosition = TrackPositionSlider.Value;
            audioFileReader.CurrentTime = TimeSpan.FromSeconds(newPosition);
        }

    }
}
