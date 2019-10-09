using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ServiceBlock.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceBlock.Messaging.Clients
{
    class RabbitMq : EventClient
    {

        private readonly IConnection connection;
        private readonly IModel channel;


        public RabbitMq(IConfiguration config)
        {
            var factory = new ConnectionFactory() { Uri = new Uri(config.GetConnectionString("AMQP")) };
            connection = factory.CreateConnection();

            channel = connection.CreateModel();


            channel.ExchangeDeclare(Block.Name, ExchangeType.Fanout);
            channel.QueueDeclare(Block.Name, true, false, false);



            foreach (var service in SubscriptionServiceNames)
            {
                channel.QueueBind(Block.Name, service, Block.Name);
            }

            var consumer = new EventingBasicConsumer(channel);

            consumer.Received += (ch, ea) =>
                {
                    string getHeader(string name) => Encoding.UTF8.GetString((byte[])ea.BasicProperties.Headers[name]);
                    var body = ea.Body;
                    var jsonString = Encoding.UTF8.GetString(body);
                    var type = Type.GetType(getHeader(nameof(Type)));
                    var eventTypeTry = Enum.TryParse(getHeader(nameof(ResourceEventType)), out ResourceEventType eventType);

                    var resource = JsonConvert.DeserializeObject(jsonString, type);

                    OnMessageReceived(new ResourceEventArgs(eventType, resource));

                    channel.BasicAck(ea.DeliveryTag, false);
                };

            String consumerTag = channel.BasicConsume(Block.Name, false, consumer);

        }

        public override void Publish<T>(ResourceEventType type, T payload)
        {
            byte[] messageBodyBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload));

            IBasicProperties props = channel.CreateBasicProperties();
            props.ContentType = "application/json";
            props.DeliveryMode = 2;
            props.Headers = new Dictionary<string, object>{
                {nameof(ResourceEventType), type.ToString()},
                {nameof(Type), typeof(T).AssemblyQualifiedName}
            };

            channel.BasicPublish(Block.Name,
                            Block.Name, props,
                            messageBodyBytes);
        }

        ~RabbitMq()
        {
            channel.Close();
            connection.Close();
        }
    }
}