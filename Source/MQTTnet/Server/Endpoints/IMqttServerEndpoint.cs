using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Channel;
using MQTTnet.Diagnostics;

namespace MQTTnet.Server.Endpoints
{
    public interface IMqttServerEndpoint : IDisposable
    {
        string Id { get; }

        void Open(IMqttServerOptions options, IMqttNetLogger logger);

        Task<IMqttChannel> AcceptAsync(CancellationToken cancellationToken);
    }
}
