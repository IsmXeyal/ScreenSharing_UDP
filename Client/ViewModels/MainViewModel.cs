using Client.Command;
using Client.Services;
using Client.Views;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client.ViewModels;

public class MainViewModel : NotificationService
{
    public MainView? MainView { get; set; }
    public ICommand? StartCommand { get; set; }

    private UdpClient client;
    private readonly IPAddress clientIP;
    private readonly int clientPort;
    private readonly IPEndPoint remoteEP;
    private bool isStart = false;
    private bool isFirst = true;
    private bool isStop = false;

    public MainViewModel(MainView mainView)
    {
        MainView = mainView;
        client = new UdpClient();
        clientIP = IPAddress.Parse("127.0.0.1");
        clientPort = 27001;
        remoteEP = new IPEndPoint(clientIP, clientPort);

        StartCommand = new RelayCommand(
            // UI thread donmasin deye async olur.
            async action =>
            {
                if (!isStart)
                {
                    isStart = true;
                    UpdateButtonUI("Stop", Brushes.DarkRed);

                    await StartReceivingImages();
                }
                else
                {
                    isStart = false;
                    UpdateButtonUI("Start", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF035203")));
                    isStop = true;
                }
            },
            pre => true);
    }

    private void UpdateButtonUI(string content, Brush background)
    {
        MainView!.mybtn.Content = content;
        MainView.mybtn.Background = background;
    }

    private async Task StartReceivingImages()
    {
        var maxLen = ushort.MaxValue - 29;
        var len = 0;
        var buffer = new byte[maxLen];

        // Client bir defe connect olsun
        if(isFirst)
        {
            await client.SendAsync(buffer, buffer.Length, remoteEP);
            isFirst = false;
        }
        
        var myImage = new List<byte>();
        try
        {
            while (true)
            {
                if (isStop)
                {
                    // Server Ekrani helede screen edir, sadece client tutmur.
                    isStop = false;
                    break;
                }
                do
                {
                    try
                    {
                        var result = await client.ReceiveAsync();
                        buffer = result.Buffer;
                        len = buffer.Length;
                        myImage.AddRange(buffer);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                } while (len == maxLen);

                var image = await ByteToImageAsync(myImage.ToArray());

                if (image is not null)
                    MainView!.ImageShare.Source = image;

                myImage.Clear();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private static async Task<BitmapImage?> ByteToImageAsync(byte[]? imageData)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = new MemoryStream(imageData!);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();

        await Task.Delay(1);
        return image;
    }
}
