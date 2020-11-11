using System;

namespace MQTTnet.Server
{
    [Obsolete("Please use _Endpoints_ instead. This will be removed soon.")]
    public class MqttServerTcpEndpointOptions : MqttServerTcpEndpointBaseOptions
    {
        public MqttServerTcpEndpointOptions()
        {
            IsEnabled = true;
            Port = 1883;
        }
    }
}
