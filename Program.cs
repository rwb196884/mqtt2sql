using Microsoft.Extensions.Configuration;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Mqtt2Sql
{
    public static class Program
    {
        private static MqttFactory _F;
        private static IManagedMqttClient _C;
        private static Database _Db;

        private static MqttClientTlsOptions MqttClientTlsOptions = new MqttClientTlsOptions
        {
            UseTls = false,
            IgnoreCertificateChainErrors = true,
            IgnoreCertificateRevocationErrors = true,
            AllowUntrustedCertificates = true
        };

        private static MqttClientOptions GetOptions(string clientName)
        {
            return new MqttClientOptions
            {
                ClientId = clientName,
                ProtocolVersion = MqttProtocolVersion.V311,
                ChannelOptions = new MqttClientTcpOptions
                {
                    Server = "192.168.1.31",
                    Port = 1883,
                    TlsOptions = MqttClientTlsOptions,
                },
                CleanSession = true, // Don't store up messages while we're offline.
                KeepAlivePeriod = TimeSpan.FromSeconds(5),
            };
        }

        public static void Main(string[] args)
        {
            bool go = true;

            while (go)
            {
                try
                {
                    _F = new MqttFactory();
                    _Db = new Database();

                    IConfiguration cfg = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                    .AddJsonFile("appsettings.json", false)
                    .Build();

                    string mattServerAddress = cfg["MattServerAddress"];
                    string mqttClientName = cfg["MqttClientName"];
                    List<string> topics = cfg.GetSection("Topics").GetChildren().Select(z => z.Value).ToList();

                    // Receiver.
                    _C = _F.CreateManagedMqttClient();
                    //_C.UseApplicationMessageReceivedHandler(HandleReceivedApplicationMessage);
                    _C.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
                    _C.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconnected);
                    //_C.ApplicationMessageProcessedHandler = new ApplicationMessageProcessedHandlerDelegate(MessageProcessedHandler);
                    _C.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(SubscriberMessageReceivedHandler);
                    _C.StartAsync(new ManagedMqttClientOptions { ClientOptions = GetOptions("ClientPublisher") }).Wait();
                    _C.SubscribeAsync(topics.Select(z => new MqttTopicFilter { Topic = z })).Wait();

                    while (!_C.IsConnected)
                    {
                        Console.WriteLine("Waiting for connections.");
                        Thread.Sleep(1000);
                    }

                    Console.WriteLine("Entering main loop; press any key to stop and quit.");
                    while (go)
                    {
                        Thread.Sleep(13000);
                        if (Console.KeyAvailable)
                        {
                            go = false;
                            break;
                        }
                    }
                    Console.WriteLine("Stopping.");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Bummer: " + e.Message);
                }
                finally
                {
                    try
                    {
                        _Db.SaveChanges();
                        _C.StopAsync().Wait();
                    }
                    catch (Exception f)
                    {
                        Console.WriteLine("Error stopping: " + f.Message);
                    }
                    _Db.Dispose();
                    _C.Dispose();
                }
            }
        }

        private static void SubscriberMessageReceivedHandler(MqttApplicationMessageReceivedEventArgs args)
        {
            MqttMessage m = new MqttMessage()
            {
                Topic = args.ApplicationMessage.Topic,
                Timestamp = DateTime.UtcNow,
                Payload = args.ApplicationMessage.ConvertPayloadToString()
            };

            Console.WriteLine($"Timestamp: {m.Timestamp:HH:mm:ss} | Topic: {m.Topic} | Payload: {m.Payload} | QoS: {args.ApplicationMessage.QualityOfServiceLevel}");
            _Db.Messages.Add(m);
        }

        private static void OnConnected(MqttClientConnectedEventArgs args)
        {
            Console.WriteLine("Connected");
        }
        private static void OnDisconnected(MqttClientDisconnectedEventArgs args)
        {
            Console.WriteLine("Disconnected");
        }
    }
}