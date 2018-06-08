using System;
using System.Text;
using System.Text.RegularExpressions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using StackExchange.Redis;

namespace VowelConsRater
{
    class Program
    {
        struct VCcount
        {
            public string id;
            public float vowel;
            public float consonant;
        }

        private static void SetTextRankById(string id, float textRank, string hostName)
        {
            IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(hostName);
            IDatabase database = redisChannel.GetDatabase();
            database.StringSet(id, textRank);
        }

        private static float TextRankCalc(float consonant, float vowel)
        {
            if (consonant == 0)
            {
                return vowel;
            }

            return vowel / consonant;
        }

        private static void RabbitListener()
        {
            string hostName = "localhost";
            string exchangeName = "vowel-consonant-counter";

            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = hostName
            };
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: exchangeName,
                                    type: ExchangeType.Direct);

            string queueName = "rank-task";
            channel.QueueDeclare(queueName, false, false, false, null);
            channel.QueueBind(queue: queueName,
                                exchange: exchangeName,
                                routingKey: "text-rank-task");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                string msg = Encoding.UTF8.GetString(body);
                var argArr = Regex.Split(msg, ":");
                VCcount data;
                if (argArr.Length == 4 && argArr[0] == "VCCmessage")
                {
                    data.id = argArr[1];
                    data.vowel = float.Parse(argArr[2]);
                    data.consonant = float.Parse(argArr[3]);

                    float result = TextRankCalc(data.consonant, data.vowel);

                    Console.WriteLine("result:" + result + " and id: " + data.id);

                    SetTextRankById(data.id, result, hostName);
                }
            };
            channel.BasicConsume(queue: queueName,
                                autoAck: true,
                                consumer: consumer);
            Console.ReadLine();
        }

        static void Main(string[] args)
        {
            RabbitListener();
        }
    }
}
