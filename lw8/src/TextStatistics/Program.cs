using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextStatistics
{
    class Program
    {
        struct Statistics
        {
            public int textCount;
            public float avgRank;
            public int highRankPart;
            public float totalRank;

            public Statistics(int textCount, float avgRank, int highRankPart, float totalRank)
            {
                this.textCount = textCount;
                this.avgRank = avgRank;
                this.highRankPart = highRankPart;
                this.totalRank = totalRank;
            }
        }

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

        private static void InitStatistics(ref Statistics statistics)
        {
            database = redisChannel.GetDatabase();
            string result = database.StringGet("statistics");
            if (!String.IsNullOrEmpty(result))
            {
                var data = result.Split(':');
                if (data.Length == 4)
                {
                    statistics.textCount = int.Parse(data[0]);
                    statistics.avgRank = float.Parse(data[1]);
                    statistics.highRankPart = int.Parse(data[2]);
                    statistics.totalRank = float.Parse(data[3]);
                }
            }
        }

        private static void UpdateStatistics(float newRank, string status, ref Statistics statistics)
        {
            ++statistics.textCount;
            if (status == "true")
            {
                ++statistics.highRankPart;
            }
            statistics.totalRank += newRank;
            statistics.avgRank = statistics.totalRank / statistics.textCount;
        }

        private static void RabbitListener()
        {
            string hostName = "localhost";
            string exchangeName = "text-success-marker";

            Statistics statistics = new Statistics(0, 0.0f, 0, 0.0f);
            InitStatistics(ref statistics);

            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = hostName
            };
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();

            channel.ExchangeDeclare(exchange: exchangeName,
                                    type: ExchangeType.Fanout);

            string queueName = channel.QueueDeclare().QueueName;
            //channel.QueueDeclare(queueName, false, false, false, null);
            channel.QueueBind(queue: queueName,
                                exchange: exchangeName,
                                routingKey: "");

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                string msg = Encoding.UTF8.GetString(body);
                string[] data = msg.Split(':');

                if (data.Length == 3 && data[0] == "TextSuccessMarked")
                {
                    var value = GetValueByKey(data[1]);
                    UpdateStatistics(float.Parse(value), data[2], ref statistics);
                    string info = statistics.textCount + ":"
                                + statistics.avgRank + ":"
                                + statistics.highRankPart + ":"
                                + statistics.totalRank;
                    Console.WriteLine("Message -> " + info);
                    database = redisChannel.GetDatabase();
                    database.StringSet("statistics", info);
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
