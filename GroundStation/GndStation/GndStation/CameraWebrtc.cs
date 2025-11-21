using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GndStation
{
    public partial class Form1 : Form
    {
        TcpListener listener;
        Process pythonProcess;
        


        private async void buttonrtc_Click(object sender, EventArgs e)
        {
            await StartPythonAndServerAsync();
        }

        private async Task StartPythonAndServerAsync()
        {
            // --- Start TCP server ---
            listener = new TcpListener(IPAddress.Loopback, 9999);
            listener.Start();

            // --- Launch Python ---
            pythonProcess = new Process();
            pythonProcess.StartInfo.FileName = @"C:\Users\cenni\Desktop\Drone_stationv5\Utilities_Code\Scripts\python.exe";
            pythonProcess.StartInfo.Arguments = @"C:\Users\cenni\Desktop\Drone_stationv5\Utilities_Code\webrtc.py";
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.Start();

            // --- Accept connection from Python script ---
            var client = await listener.AcceptTcpClientAsync();
            var stream = client.GetStream();
            Console.WriteLine("Start");

            // --- Read frames ---
            await Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        // Read frame (4 bytes)
                        byte[] lengthBytes = new byte[4];
                        int read = stream.Read(lengthBytes, 0, 4);
                        if (read == 0) break;
                        Console.WriteLine("Recibo frame");

                        int length = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(lengthBytes, 0));

                        // Read JPEG
                        byte[] buffer = new byte[length];
                        int offset = 0;

                        while (offset < length)
                        {
                            int r = stream.Read(buffer, offset, length - offset);
                            if (r == 0) break;
                            offset += r;
                        }

                        // Convert to Bitmap
                        var ms = new MemoryStream(buffer);
                        {
                            Image img = Image.FromStream(ms);

                            // Show in PictureBox
                            _pb.Invoke(new Action(() =>
                            {
                                _pb.Image?.Dispose();
                                _pb.Image = new Bitmap(img);
                            }));
                        }
                        

                    }
                    catch
                    {
                        break;
                    }
                }
            });
        }

    }
}
