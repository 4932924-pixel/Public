using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace NetSdrClientApp.Networking
{
    public class TcpClientWrapper : ITcpClient, IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;

        public bool Connected => _tcpClient != null && _tcpClient.Connected && _stream != null;
        public event EventHandler<byte[]>? MessageReceived;

        public TcpClientWrapper(string host, int port)
        {
            _host = host;
            _port = port;
        }

        public void Connect()
        {
            if (Connected) return;
            _tcpClient = new TcpClient();
            try
            {
                _cts = new CancellationTokenSource();
                _tcpClient.Connect(_host, _port);
                _stream = _tcpClient.GetStream();
                _ = StartListeningAsync();
            }
            catch (Exception) { /* Log error */ }
        }

        public void Disconnect()
        {
            if (Connected)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _stream?.Close();
                _tcpClient?.Close();
                _cts = null;
                _tcpClient = null;
                _stream = null;
            }
        }

        public async Task SendMessageAsync(byte[] data)
        {
            if (Connected && _stream != null)
                await _stream.WriteAsync(data.AsMemory(), _cts?.Token ?? CancellationToken.None);
        }

        public async Task SendMessageAsync(string str)
        {
            var data = Encoding.UTF8.GetBytes(str);
            await SendMessageAsync(data);
        }

        private async Task StartListeningAsync()
        {
            if (_stream == null || _cts == null) return;
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    byte[] buffer = new byte[8194];
                    int bytesRead = await _stream.ReadAsync(buffer.AsMemory(), _cts.Token);
                    if (bytesRead > 0)
                        MessageReceived?.Invoke(this, buffer.AsSpan(0, bytesRead).ToArray());
                }
            }
            catch (OperationCanceledException) { }
        }

        public void Dispose()
        {
            _cts?.Dispose();
            _tcpClient?.Dispose();
            _stream?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
