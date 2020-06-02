using System;
using System.IO;
using System.Net;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace RockRadioUA
{
    public class MainWindow : Window
    {
        Radio radio;

        bool play = true;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif


            try
            {
                radio = new Radio("https://rockradioua.online:8433/rockradio256");

                radio.OnStatusUpdate += (s) =>
                {
                    Dispatcher.UIThread.InvokeAsync(() => this.FindControl<TextBlock>("TrackLabel").Text = s);
                };

                radio.OnTitleAndArtistUpdate += (s) =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        var strs = s.Split(" - ");

                        this.FindControl<TextBlock>("TrackLabel").Text = strs[1];
                        this.FindControl<TextBlock>("ArtistLabel").Text = strs[0];

                        var img = new WebClient().DownloadData("https://rockradioua.online/rockradioua_artwork.png");
                        this.FindControl<Image>("Cover").Source = new Bitmap(new MemoryStream(img));
                    });
                };

                radio.Start();
            }
            catch (Exception e)
            {
                this.FindControl<TextBlock>("TrackLabel").Text = e.Message;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            this.Icon = new WindowIcon(new MemoryStream(AppResources.Logo));
            this.FindControl<Image>("Cover").Source = new Bitmap(new MemoryStream(AppResources.Logo));
            this.FindControl<Image>("TitleIcon").Source = new Bitmap(new MemoryStream(AppResources.Logo));
            this.FindControl<Image>("ExitBtn").Source = new Bitmap(new MemoryStream(AppResources.Exit));
            this.FindControl<Image>("MinimizeBtn").Source = new Bitmap(new MemoryStream(AppResources.Minimize));
            this.FindControl<Image>("PlaybackControl").Source = new Bitmap(new MemoryStream(AppResources.Stop));
        }
        
        private void PlaybackControl_MouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
        {
            play = !play;
            
            if (play)
            {
                radio.Start();

                ((Image)sender).Source = new Bitmap(new MemoryStream(AppResources.StopActive));
            }
            else
            {
                radio.Stop();

                ((Image)sender).Source = new Bitmap(new MemoryStream(AppResources.PlayActive));

                this.FindControl<TextBlock>("TrackLabel").Text = "Відтворення призупинено";
                this.FindControl<TextBlock>("ArtistLabel").Text = string.Empty;

                this.FindControl<Image>("Cover").Source = new Bitmap(new MemoryStream(AppResources.Logo));
            }
        }
        
        private void PlaybackControl_MouseEnter(object sender, PointerEventArgs e)
        {
            if (play)
            {
                ((Image)sender).Source = new Bitmap(new MemoryStream(AppResources.StopActive));
            }
            else
            {
                ((Image)sender).Source = new Bitmap(new MemoryStream(AppResources.PlayActive));
            }
        }

        private void PlaybackControl_MouseLeave(object sender, PointerEventArgs e)
        {
            if (play)
            {
                ((Image)sender).Source = new Bitmap(new MemoryStream(AppResources.Stop));
            }
            else
            {
                ((Image)sender).Source = new Bitmap(new MemoryStream(AppResources.Play));
            }
        }

        private void ExitBtn_MouseEnter(object sender, PointerEventArgs e)
        {
            ((Image)sender).Source = new Bitmap(new MemoryStream(AppResources.ExitActive));
        }

        private void ExitBtn_MouseLeave(object sender, PointerEventArgs e)
        {
            ((Image)sender).Source = new Bitmap(new MemoryStream(AppResources.Exit));
        }

        private void ExitBtn_MouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
        {
            this.Close();
        }

        private void MinimizeBtn_MouseEnter(object sender, PointerEventArgs e)
        {
            ((Image)sender).Source = new Bitmap(new MemoryStream(AppResources.MinimizeActive));
        }

        private void MinimizeBtn_MouseLeave(object sender, PointerEventArgs e)
        {
            ((Image)sender).Source = new Bitmap(new MemoryStream(AppResources.Minimize));
        }

        private void MinimizeBtn_MouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            this.BeginMoveDrag(e);
        }

        private void TitleIcon_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            this.BeginMoveDrag(e);
        }
        
        private void TitleText_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            this.BeginMoveDrag(e);
        }
    }
}
