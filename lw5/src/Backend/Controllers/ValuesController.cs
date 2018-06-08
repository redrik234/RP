using System;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using RabbitMQ.Client;
using System.Text;
using System.Threading;
using System.Globalization;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private static IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect("localhost");
        IDatabase database = redisChannel.GetDatabase();
        private static int retryCount = 4;
        private static int sleep = 600; //ms


        private IActionResult GetRank(string id)
        {
            string value = null;
            bool isFloat = false;

            for (int i = 0; i <= retryCount; ++i)
            {
                value = database.StringGet(id);
                Console.WriteLine("value: " + value);
                value = value.Replace('.', ',');
                if (value != null && Double.TryParse(value, out double res))
                {
                    isFloat = true;
                    break;
                }
                Thread.Sleep(sleep);
            }

            if (!String.IsNullOrEmpty(value) && isFloat)
            {
                return Ok(value);
            }

            return new NotFoundResult();
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            return GetRank(id);
        }

        private static string GetValueByKeyAndHostName(string id, string hostName)
        {
            IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect(hostName);
            IDatabase database = redisChannel.GetDatabase();
            string value = "";
            value = database.StringGet(id);
            return value;
        }
        private static void AddIdToQueue(string id)
        {
            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = "localhost"
            };
            IConnection connection = factory.CreateConnection();
            IModel channel = connection.CreateModel();
            channel.ExchangeDeclare(exchange: "backend-api", type: "fanout");
            string message = id;
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "backend-api",
                        routingKey: "",
                        basicProperties: null,
                        body: body);
        }

        // POST api/values
        [HttpPost]
        public string Post([FromForm]string data)
        {
            //Debug.Assert(value != null, "String is null");
            if(data == null)
            {
                return "string is null";
            }
            var id = Guid.NewGuid().ToString();
            database.StringSet(id, data);
            AddIdToQueue(id);
            return id;
        }

    }
}
