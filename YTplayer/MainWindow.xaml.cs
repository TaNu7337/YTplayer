using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NAudio.Wave;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace YTplayer
{
    public partial class MainWindow : Window
    {
        private List<string> urlList = new List<string>();
        private int currentIndex = -1;
        private WaveOutEvent outputDevice;
        private MediaFoundationReader audioFile;
        private DispatcherTimer timer;
        private string tempFilePath;

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
        }

        private void AddToList_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text;
            if (!string.IsNullOrWhiteSpace(url))
            {
                urlList.Add(url);
                UrlListBox.Items.Add(url);
                UrlTextBox.Clear();
            }
        }

        private async void Play_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex == -1 && urlList.Any())
            {
                currentIndex = 0;
            }

            if (currentIndex >= 0 && currentIndex < urlList.Count)
            {
                await PlayAudio(urlList[currentIndex]);
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (outputDevice != null)
            {
                if (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    outputDevice.Pause();
                }
                else if (outputDevice.PlaybackState == PlaybackState.Paused)
                {
                    outputDevice.Play();
                }
            }
        }

        private async void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex > 0)
            {
                currentIndex--;
                await PlayAudio(urlList[currentIndex]);
            }
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex < urlList.Count - 1)
            {
                currentIndex++;
                await PlayAudio(urlList[currentIndex]);
            }
        }

        private async Task PlayAudio(string url)
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(url);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var stream = await youtube.Videos.Streams.GetAsync(audioStreamInfo);

            if (outputDevice != null)
            {
                outputDevice.Stop();
                outputDevice.Dispose();
                audioFile.Dispose();
            }

            // 一時ファイルにストリームを書き込む
            tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                await stream.CopyToAsync(fileStream);
            }

            audioFile = new MediaFoundationReader(tempFilePath);
            outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);
            outputDevice.Play();

            PositionSlider.Maximum = audioFile.TotalTime.TotalSeconds;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (audioFile != null && audioFile.CurrentTime.TotalSeconds <= PositionSlider.Maximum)
            {
                PositionSlider.Value = audioFile.CurrentTime.TotalSeconds;
            }
        }

        private void PositionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (audioFile != null && Math.Abs(audioFile.CurrentTime.TotalSeconds - e.NewValue) > 1)
            {
                audioFile.CurrentTime = TimeSpan.FromSeconds(e.NewValue);
            }
        }
    }
}