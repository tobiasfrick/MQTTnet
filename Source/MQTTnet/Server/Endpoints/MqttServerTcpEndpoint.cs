#if !WINDOWS_UWP
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Diagnostics;
using MQTTnet.Implementations;

namespace MQTTnet.Server.Endpoints
{
    public sealed class MqttServerTcpEndpoint : IMqttServerEndpoint
    {
        IMqttServerOptions _options;
        CrossPlatformSocket _socket;
        IMqttNetScopedLogger _logger;
        IPEndPoint _localEndPoint;

        public string Id { get; set; }
        
        public int Port { get; set; } = 1883;

        public int ConnectionBacklog { get; set; } = 10;

        public bool NoDelay { get; set; } = true;

#if WINDOWS_UWP
        public int BufferSize { get; set; } = 4096;
#endif

        public IPAddress BoundAddress { get; set; } = IPAddress.Any;

        /// <summary>
        /// This requires admin permissions on Linux.
        /// </summary>
        public bool ReuseAddress { get; set; }

        /// <summary>
        /// Leaving this _null_ will use a dual mode socket.
        /// </summary>
        public AddressFamily? AddressFamily { get; set; }

        public MqttServerTcpEndpointTlsOptions TlsOptions { get; set; }

        public void Open(IMqttServerOptions options, IMqttNetLogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            _logger = logger.CreateScopedLogger(nameof(MqttServerTcpEndpoint));

            _options = options ?? throw new ArgumentNullException(nameof(options));

            _localEndPoint = new IPEndPoint(BoundAddress, Port);

            _socket = !AddressFamily.HasValue ? new CrossPlatformSocket() : new CrossPlatformSocket(AddressFamily.Value);
            _socket.Bind(_localEndPoint);
            _socket.NoDelay = NoDelay;
            _socket.ReuseAddress = ReuseAddress;
            _socket.Listen(ConnectionBacklog);

            _logger.Info($"Starting TCP listener for {_localEndPoint} TLS={TlsOptions != null}.");
        }

        public async Task<IMqttChannel> AcceptAsync(CancellationToken cancellationToken)
        {
            Stream stream = null;
            X509Certificate2 clientCertificate = null;

            try
            {
                var client = await _socket.AcceptAsync().ConfigureAwait(false);
                if (client == null)
                {
                    return null;
                }

                client.NoDelay = NoDelay;

                var remoteEndPoint = client.RemoteEndPoint.ToString();
                stream = client.GetStream();
                

                var tlsOptions = TlsOptions;
                var tlsCertificate = tlsOptions?.CertificateProvider?.GetCertificate();
                if (tlsCertificate != null)
                {
                    var sslStream = new SslStream(stream, false, tlsOptions.RemoteCertificateValidationCallback);

                    await sslStream.AuthenticateAsServerAsync(
                        tlsCertificate,
                        tlsOptions.ClientCertificateRequired,
                        tlsOptions.SslProtocol,
                        tlsOptions.CheckCertificateRevocation).ConfigureAwait(false);

                    stream = sslStream;

                    clientCertificate = sslStream.RemoteCertificate as X509Certificate2;

                    if (clientCertificate == null && sslStream.RemoteCertificate != null)
                    {
                        clientCertificate = new X509Certificate2(sslStream.RemoteCertificate.Export(X509ContentType.Cert));
                    }
                }

                return new MqttTcpChannel(stream, remoteEndPoint, clientCertificate);
            }
            catch (Exception exception)
            {
                stream?.Dispose();

#if NETSTANDARD1_3 || NETSTANDARD2_0 || NET461 || NET472
                clientCertificate?.Dispose();
#endif

                if (exception is SocketException socketException)
                {
                    if (socketException.SocketErrorCode == SocketError.ConnectionAborted ||
                        socketException.SocketErrorCode == SocketError.OperationAborted)
                    {
                        return null;
                    }
                }

                _logger.Error(exception, $"Error while accepting connection at TCP listener {_localEndPoint} TLS={TlsOptions != null}.");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }

            return null;
        }

        public void Dispose()
        {
            _socket?.Dispose();
        }
    }
}
#endif