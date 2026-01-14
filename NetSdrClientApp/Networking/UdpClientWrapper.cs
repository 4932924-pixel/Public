using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSdrClientApp.Networking
{
    public class UdpClientWrapper : IUdpClient, IDisposable
    {
        private readonly IPEndPoint _localEndPoint; // ДОДАНО: readonly
        private CancellationTokenSource? _cts;
        private UdpClient? _udpClient;

        public event EventHandler<byte[]>? MessageReceived;

        public UdpClientWrapper(int port)
        {
            _localEndPoint = new IPEndPoint(IPAddress.Any, port);
        }

        public async Task StartListeningAsync()
        {
            _cts = new CancellationTokenSource();
            Console.WriteLine("Start listening for UDP messages...");

            try
            {
                _udpClient = new UdpClient(_localEndPoint);
                while (!_cts.Token.IsCancellationRequested)
                {
                    // ВИПРАВЛЕНО: використання токена для коректної зупинки
                    UdpReceiveResult result = await _udpClient.ReceiveAsync(_cts.Token);
                    
                    var handler = MessageReceived;
                    handler?.Invoke(this, result.Buffer);

                    Console.WriteLine($"Received from {result.RemoteEndPoint}");
                }
            }
            catch (OperationCanceledException)
            {
                // Очікувана зупинка
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error receiving message: {ex.Message}");
            }
            finally
            {
                StopListening();
            }
        }

        public void StopListening()
        {
            try
            {
                _cts?.Cancel();
                _udpClient?.Close();
                Console.WriteLine("Stopped listening for UDP messages.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while stopping: {ex.Message}");
            }
        }

        public void Exit()
        {
            Dispose(); // ВИПРАВЛЕНО: замість дублювання викликаємо очищення
        }

        public override int GetHashCode()
        {
            // ВИПРАВЛЕНО: SonarCloud рекомендує використовувати стабільні методи хешування
            var payload = $"{nameof(UdpClientWrapper)}|{_localEndPoint.Address}|{_localEndPoint.Port}";

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(payload));

            return BitConverter.ToInt32(hash, 0);
        }

        // ПРАВИЛЬНА РЕАЛІЗАЦІЯ IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Повідомляємо GC, що об'єкт очищено
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _udpClient?.Dispose(); // Виправляє Blocker
            }
        }
    }
}
