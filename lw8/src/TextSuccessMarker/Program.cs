using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextSuccessMarker
{
    class Program
    {
        private static string hostName = "localhost";
        private static string inputExchangeName = "text-rank-calc";
        private static string outputExchangeName = "text-success-marker";
        private const float minSuccessRank = 0.5f;

        private static void AddMsgToQueue(string id, string data, string exchangeName, IModel model)
        {
            model.ExchangeDeclare(exchangeName, ExchangeType.Fanout);
            string msg = "TextSuccessMarked:" + id + ":" + data;
            var body = Encoding.UTF8.GetBytes(msg);

            model.BasicPublish(
                exchange: exchangeName,
                routingKey: "",
                basicProperties: null,
                body: body
            );
        }

        private static void RabbitListener()
        {
            try
            {
                ConnectionFactory factory = new ConnectionFactory();
                factory.HostName = hostName;

                IConnection connection = factory.CreateConnection();
                IModel channel = connection.CreateModel();
                string queueName = channel.QueueDeclare().QueueName;
                channel.ExchangeDeclare(exchange: inputExchangeName, type: "fanout");
                channel.QueueBind(queue: queueName,
                                    exchange: inputExchangeName,
                                    routingKey: "");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    string msg = Encoding.UTF8.GetString(body);

                    var data = msg.Split(':');
                    if (data.Length == 3 && data[0] == "TextRankCalculated")
                    {
                        Console.WriteLine("Received msg -> " + msg);

                        float rank = float.Parse(data[2]);
                        if (rank > minSuccessRank)
                        {
                            AddMsgToQueue(data[1], "true", outputExchangeName, channel);
                        }
                        else
                        {
                            AddMsgToQueue(data[1], "false", outputExchangeName, channel);
                        }
                    }
                };
                channel.BasicConsume(queue: queueName,
                                    autoAck: true,
                                    consumer: consumer);
                Console.ReadLine();
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
                Environment.Exit(1);
            }
        }

        static void Main(string[] args)
        {
            RabbitListener();
        }
    }
}
