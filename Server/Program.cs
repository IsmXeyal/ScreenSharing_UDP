using System.Net;
using System.Net.Sockets;
// Ekrani screen etmek ucun istifade olunan kitabxana
using System.Drawing;
using System.Drawing.Imaging;

namespace Server;

public class Program
{
    public static async Task Main()
    {
        var ip = IPAddress.Parse("127.0.0.1");
        var port = 27001;

        var listenerEP = new IPEndPoint(ip, port);
        var listener = new UdpClient(listenerEP);

        Console.WriteLine($"{listenerEP} Listener...");

        // We could have several clients...
        while (true)
        {
            // We don't know with who we gonna share our screen, when client start that time we'll know. For taking EndPoint from
            // client firstly client must send us request.
            var result = await listener.ReceiveAsync();
            Console.WriteLine($"{result.RemoteEndPoint} was Connected...");

            // Each client must be in seperate thread that they couldn't create obstacle for each other.
            _ = Task.Run( async () =>
            {
                // Each client must keep its EndPoint in its stack.
                var clientEP = result.RemoteEndPoint;

                // Now server must take screenshots(till we end) non-stop and it must send it to the client
                // Dayanmadan ekrani screen edib, daha sonra byte cevirib gondermelidir ve bu prosesler yeniden davam etmelidir.
                while (true)
                {
                    // 1) Take ScreenShot
                    var imageScreen = await TakeScreenShotAsync();

                    // 2) Image to Byte
                    var imageBytes = await ImageToByteAsync(imageScreen);

                    // 3) Send to Client
                    if (imageBytes != null)
                    {
                        var chunks = imageBytes.Chunk(ushort.MaxValue - 29);

                        foreach (var chunk in chunks)
                        {
                            await listener.SendAsync(chunk, chunk.Length, clientEP);
                        }
                    }
                }
            });
        }
    }

    private static async Task<Image?> TakeScreenShotAsync()
    {
        try
        {
            // Ekran screen olunub bitmap-in icine yuklenir.
            Bitmap? bitmap = new Bitmap(width: 1920, height: 1080);

            // Ekranin screen olunmasi
            using Graphics graphics = Graphics.FromImage(bitmap);

            //Ekran screen olunanda hardan basliyacaq
            // Ilk ikisi ekrani hardan screen etdiyini
            // 3 ve 4 ise bitmap-in icinde hardan yazacagini deyir ve en sonuncu ise hara qeder oldugunu deyir.
            graphics.CopyFromScreen(0, 0, 0, 0, bitmap.Size);

            return bitmap;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error capturing screen: {ex.Message}");
            return new Bitmap(1, 1);
        }
    }

    private static async Task<byte[]?> ImageToByteAsync(Image? image)
    {
        // Sekil birbasa byte[] cevrile bilmez(Sekilin olcusunu bilmeden byte[] yaratmaq olmur).
        // Sekili stream-e qoyub sonra byte[] cevirmeliyik.
        // MemoryStream ile Data Ramda saxlanilir.
        using MemoryStream ms = new MemoryStream();
        await Task.Run(() => image.Save(ms, ImageFormat.Jpeg));

        return ms.ToArray();
    }
}
