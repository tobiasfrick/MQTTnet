using System.Security.Authentication;
using MQTTnet.Certificates;

namespace MQTTnet.Server.Endpoints
{
    public sealed class MqttServerTcpEndpointTlsOptions
    {
        public ICertificateProvider CertificateProvider { get; set; }

        public bool ClientCertificateRequired { get; set; }

        public bool CheckCertificateRevocation { get; set; }

        public SslProtocols SslProtocol { get; set; } = SslProtocols.Tls12;

#if !WINDOWS_UWP
        public System.Net.Security.RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
#endif
    }
}