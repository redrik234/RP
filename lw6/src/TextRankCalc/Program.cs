using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace TextRankCalc
{
    class Program
    {
        private static string hostName = "localhost";
        private static string inputExchangeName = "backend-api";
        private static string outputExchangeName = "text-rank-tasks";

        private static void AddToQueue(string id, string exchangeName, IModel model)
        {
            model.ExchangeDeclare(exchangeName, ExchangeType.Direct);
            var body = Encoding.UTF8.GetBytes(id);

            model.BasicPublish(
                exchange: exchangeName,
                routingKey: "text-rank-task",
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
                    string id = Encoding.UTF8.GetString(body); 
                    if (!String.IsNullOrEmpty(id))
                    {
                        AddToQueue(id, outputExchangeName, channel);
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
