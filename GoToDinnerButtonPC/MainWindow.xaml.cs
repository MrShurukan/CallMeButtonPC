using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GoToDinnerButtonPC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow main;

        public MainWindow()
        {
            InitializeComponent();
            main = this;

            main.WindowState = WindowState.Minimized;
            main.WindowStyle = WindowStyle.None;

            Task.Run(async () =>
            {
                using var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri("ws://localhost:8080"), CancellationToken.None);

                await SendMessage(ws, GetArraySegmentFromString("ilya"));

                while (ws.State == WebSocketState.Open)
                {
                    ArraySegment<byte> bytesReceived = new ArraySegment<byte>(new byte[1024]);
                    WebSocketReceiveResult result = await ws.ReceiveAsync(bytesReceived, CancellationToken.None);
                    var response = Encoding.UTF8.GetString(bytesReceived.Array, 0, result.Count);
                    ServerResponse responseObject = JsonConvert.DeserializeObject<ServerResponse>(response);

                    System.Media.SoundPlayer sirenPlayer = new System.Media.SoundPlayer("siren.wav");

                    if (responseObject.eventName == "request")
                    {
                        var requestMessage = JsonConvert.DeserializeObject<RequestMessage>(responseObject.data);
                        await SendMessage(ws, GetArraySegmentFromString("got_request"));

                        MainWindow.main.Dispatcher.Invoke(() =>
                        {
                            var window = MainWindow.main;
                            if (!window.IsVisible)
                            {
                                window.Show();
                            }

                            if (window.WindowState == WindowState.Minimized)
                            {
                                window.WindowState = WindowState.Normal;
                            }

                            window.WindowState = WindowState.Maximized;
                            window.WindowStyle = WindowStyle.None;

                            window.CallerLabel.Content = requestMessage.callerName;
                            window.RequestLabel.Content = requestMessage.buttonPressed;

                            window.Activate();
                            window.Topmost = true;  // important
                            window.Topmost = false; // important
                            window.Focus();         // important

                            sirenPlayer.Play();
                        });
                    }
                    else if (responseObject.eventName == "request_done")
                    {
                        MainWindow.main.Dispatcher.Invoke(() =>
                        {
                            var window = MainWindow.main;
                            window.Hide();

                            window.WindowState = WindowState.Normal;
                            window.WindowState = WindowState.Minimized;
                        });

                        sirenPlayer.Stop();
                    }
                }
            });
        }

        private static async Task SendMessage(ClientWebSocket ws, ArraySegment<byte> arraySegment)
        {
            await ws.SendAsync(arraySegment, System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private static ArraySegment<byte> GetArraySegmentFromString(string message)
        {
            return new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }

    public record ServerResponse(string eventName, string data);
    public record RequestMessage(string callerName, string buttonPressed);
}
