using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextProcessingLimiter
{
    class Program
    {
        static IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect("localhost");
        static IDatabase database;
        static int maxAvailableTextCount = 4;

        private static void SendMSgToQueue(string contextId, string data, string exchangeName, IModel model)
        {
            model.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);
            string msg = "ProcessingAccepted:" + contextId + ":" + data;
            var body = Encoding.UTF8.GetBytes(msg);
            model.BasicPublish(exchangeName, "", null, body);
        }

        private static void RabbitListener()
        {
            string hostName = "localhost";
            string inputExchangeName = "backend-api";
            string successExchangeName = "text-success-marker";
            string outputExchangeName = "processing-limiter";

            int availableTextCount = maxAvailableTextCount;

            Console.WriteLine("Max availabile text count: " + maxAvailableTextCount);

            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = hostName
            };

            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();

            channel.ExchangeDeclare(inputExchangeName, ExchangeType.Fanout);
            channel.ExchangeDeclare(successExchangeName, ExchangeType.Fanout);

            string inputQueueName = channel.QueueDeclare().QueueName;
            channel.QueueBind(queue: inputQueueName
                                    , exchange: inputExchangeName
                                    , routingKey: "");

            string successQueue = channel.QueueDeclare().QueueName;
            channel.QueueBind(queue: successQueue
                                    , exchange: successExchangeName
                                    , routingKey: "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                string msg = Encoding.UTF8.GetString(body);

                var data = msg.Split(':');

                if (data.Length == 3 && data[0] == "TextSuccessMarked" && data[2] == "false")
                {
                    if (availableTextCount < maxAvailableTextCount)
                    {
                        ++availableTextCount;
                        Console.WriteLine("Available text count + 1. Total count:" + availableTextCount);
                    }
                }

                if (data.Length == 2 && data[0] == "TextCreated")
                {
                    if (availableTextCount >= 0)
                    {
                        Console.WriteLine("Available text count: " + availableTextCount);
                        --availableTextCount;
                        SendMSgToQueue(data[1], "true", outputExchangeName, channel);
                    }
                    else
                    {
                        Console.WriteLine("Limit is reached");
                        SendMSgToQueue(data[1], "false", outputExchangeName, channel);
                    }
                }
            };
            channel.BasicConsume(queue: inputQueueName,
                                autoAck: true,
                                consumer: consumer);
            channel.BasicConsume(queue: successQueue,
                                autoAck: true,
                                consumer: consumer);
            Console.ReadLine();
        }

        public static void Main(string[] args)
        {
            RabbitListener();
        }
    }
}
