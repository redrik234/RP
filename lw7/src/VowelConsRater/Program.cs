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

        static IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect("localhost");
        static IDatabase database = redisChannel.GetDatabase();

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

        private static void SetTextRankById(string id, float textRank)
        {
            SetDB(id);
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

        private static void SendRankToExchange(string contextId, float rank, string exchangeName, IModel model)
        {
            model.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Fanout);
            string msg = "TextRankCalculated:" + contextId + ":" + rank;
            var body = Encoding.UTF8.GetBytes(msg);
            Console.WriteLine("Message -> " + msg);
            model.BasicPublish(exchangeName, "", null, body);
        }

        private static void RabbitListener()
        {
            string hostName = "localhost";
            string exchangeName = "vowel-consonant-counter";
            string outputExcangeName = "text-rank-calc";

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

                    SetTextRankById(data.id, result);
                    SendRankToExchange(data.id, result, outputExcangeName, channel);
                    Console.WriteLine("result:" + result + " and id: " + data.id);
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
