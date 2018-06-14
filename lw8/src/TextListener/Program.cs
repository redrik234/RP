using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Text;

namespace TextListener
{
    public class Program
    {
        static IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect("localhost");
        static IDatabase database;

        private static int CalculateDbId(string id)
        {
            int h = 0;
            foreach (char sym in id)
            {
                h += sym;
            }

            return h % 16;
        }

        private static void SetDB(string contextId)
        {
            var id = CalculateDbId(contextId);
            database = redisChannel.GetDatabase(id);
            Console.WriteLine("ContextId : " + contextId + " | Redis database: " + id);
        }

        private static string GetValueByKey(string id)
        {
            SetDB(id);
            string value = "";
            value = database.StringGet(id);
            return value;
        }

        private static void RabbitListener()
        {
            string hostName = "localhost";
            string exchangeName = "backend-api";

            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = hostName
            };
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();
            string queueName = channel.QueueDeclare().QueueName;
            channel.ExchangeDeclare(exchange: exchangeName, type: "fanout");
            channel.QueueBind(queue: queueName,
                                exchange: exchangeName,
                                routingKey: "");

             var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    string msg = Encoding.UTF8.GetString(body);

                    var data = msg.Split(':');
                    if (data.Length == 2 && data[0] == "TextCreated")
                    {
                        string text = GetValueByKey(data[1]);
                        Console.WriteLine(data[1] + " : " + text);
                    }
                };
                channel.BasicConsume(queue: queueName,
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