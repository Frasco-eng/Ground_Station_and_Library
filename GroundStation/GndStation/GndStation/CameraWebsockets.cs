using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GndStation
{
    partial class Form1
    {
        private readonly Uri _wsUri = new Uri("ws://10.42.0.1:8765/video"); 
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private PictureBox _pb;

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _cts = new CancellationTokenSource();
            await ConnectAndReceiveAsync(_cts.Token);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cts?.Cancel();
            _ws?.Dispose();
            base.OnFormClosing(e);
        }

        private async Task ConnectAndReceiveAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                _ws?.Dispose();
                _ws = new ClientWebSocket();

                try
                {
                    await _ws.ConnectAsync(_wsUri, ct);

                    var buffer = new byte[64 * 1024]; // chunk
                    while (_ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                    {
                        using (var ms = new MemoryStream())
                        {
                            WebSocketReceiveResult result;
                            do
                            {
                                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                                if (result.MessageType == WebSocketMessageType.Close)
                                {
                                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ct);
                                    break;
                                }
                                if (result.MessageType == WebSocketMessageType.Binary)
                                {
                                    ms.Write(buffer, 0, result.Count);
                                }
                                else
                                {
                                    // Ignora messaggi di testo (o usali per heartbeat)
                                }
                            } while (!result.EndOfMessage && _ws.State == WebSocketState.Open);

                            if (ms.Length > 0)
                                ShowFrame(ms);
                        }
                    }
                }
                catch
                {
                    // silenzioso: tenteremo il reconnect
                }

                // backoff semplice per reconnect
                await Task.Delay(1000, ct);
            }
        }

        private void ShowFrame(MemoryStream ms)
        {
            try
            {
                ms.Position = 0;
                using (var bmp = new Bitmap(ms))
                {
                    // Cloniamo l’immagine per non lasciare stream aperti
                    var frame = new Bitmap(bmp);
                    if (_pb.InvokeRequired)
                    {
                        _pb.BeginInvoke(new Action(() =>
                        {
                            var old = _pb.Image;
                            _pb.Image = frame;
                            old?.Dispose();
                        }));
                    }
                    else
                    {
                        var old = _pb.Image;
                        _pb.Image = frame;
                        old?.Dispose();
                    }
                }
            }
            catch
            {
                // frame corrotto: ignora
            }
        }
    }

    internal static class ControlExtensions
    {
        // Abilita DoubleBuffered anche su PictureBox
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop?.SetValue(control, enable, null);
        }
    }
}

// WS_CAM REMEMBER!!!!!!!!!!!!!!!!!!!!!!!!!