using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Collections.Concurrent;
using StackExchange.Redis;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private static IConnectionMultiplexer redisChannel = ConnectionMultiplexer.Connect("localhost");
        IDatabase database = redisChannel.GetDatabase();
        

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(string id)
        {
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
        public string Post([FromForm]string value)
        {
            //Debug.Assert(value != null, "String is null");
            if(value == null)
            {
                return "string is null";
            }
            var id = Guid.NewGuid().ToString();
            database.StringSet(id, value);
            AddIdToQueue(id);
            return id;
        }

    }
}
