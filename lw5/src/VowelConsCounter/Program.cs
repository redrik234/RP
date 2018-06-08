using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.MessagePatterns;
using StackExchange.Redis;
using System.Text;
using System.Text.RegularExpressions;

namespace VowelConsCounter
{
    struct VowelConsCount
    {
        private string id;
        private string vowel;
        private string consonant;

        public VowelConsCount(string i, string v, string c)
        {
            id = i;
            vowel = v;
            consonant = c;
        }

        public string getId()
        {
            return id;
        }

        public string getVowel()
        {
            return vowel;
        }

        public string getConsonant()
        {
            return consonant;
        }
    }

    class Program
    {
        private const string inputExchangeName = "text-rank-tasks";

        private static string REG_EN_VOWEL = @"[aeyuio]";
        private static string REG_EN_CONSONANT = @"[bcdfghjklmnpqrstvwxz]";
        private static string REG_RUS_VOWEL = @"[аеёиоуыэюя]";
        private static string REG_RUS_CONSONANT = @"[бвгджзклмнпрстфхцчшщ]";

        private static string GetValueByKeyAndHostName(string id, string hostName)
        {
            IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(hostName);
            IDatabase database = redisChannel.GetDatabase();
            string value = "";
            value = database.StringGet(id);
            return value;
        }

        private static VowelConsCount VowelConsCounter(string id, string text)
        {
            int vowel = 0;
            int consonant = 0;

            vowel += Regex.Matches(text, REG_EN_VOWEL, RegexOptions.IgnoreCase).Count;
            vowel += Regex.Matches(text, REG_RUS_VOWEL, RegexOptions.IgnoreCase).Count;

            consonant += Regex.Matches(text, REG_EN_CONSONANT, RegexOptions.IgnoreCase).Count;
            consonant += Regex.Matches(text, REG_RUS_CONSONANT, RegexOptions.IgnoreCase).Count;
            return new VowelConsCount(id, vowel.ToString(), consonant.ToString());
        }
        
        private static void AddToQueue(VowelConsCount count, IModel model)
        {
            model.ExchangeDeclare("vowel-consonant-counter", ExchangeType.Direct);
            string message =  "VCCmessage:" + count.getId() + ":" + count.getVowel() + ":" + count.getConsonant();
            var body = Encoding.UTF8.GetBytes(message);

            model.BasicPublish(
                exchange: "vowel-consonant-counter",
                routingKey: "text-rank-task",
                basicProperties: null,
                body: body
            );
        }

        private static void RabbitListener()
        {
            string hostName = "localhost";

            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = hostName
            };
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();
            
            channel.ExchangeDeclare(exchange: inputExchangeName, 
                                    type: ExchangeType.Direct);

            string queueName = "count-task";
            channel.QueueDeclare(queueName, false, false, false, null);
            channel.QueueBind(queue: queueName,
                                exchange: inputExchangeName,
                                routingKey: "text-rank-task");

             var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body;
                    string id = Encoding.UTF8.GetString(body);                    
                    string value = GetValueByKeyAndHostName(id, hostName);

                    VowelConsCount count = VowelConsCounter(id, value);

                    AddToQueue(count, channel);

                    Console.WriteLine("Id:" + count.getId() + " count: " + count.getVowel() + "/" + count.getConsonant());
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
