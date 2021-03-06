﻿using System;
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
        private static IDatabase database;
        private static int retryCount = 4;
        private static int sleep = 600; //ms

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

        private IActionResult GetRank(string id)
        {
            string value = null;
            bool isFloat = false;

            for (int i = 0; i <= retryCount; ++i)
            {
                value = database.StringGet(id);

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
            else
            {
                return Ok("Превышен лимит проверок текста. Для получения доступа пожертвуйте биткоины(деньги то же принимаются) на развитие ресурса:)");
            }
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            return GetRank(id);
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
            string message = "TextCreated:" + id;
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(exchange: "backend-api",
                        routingKey: "",
                        basicProperties: null,
                        body: body);
        }

        [HttpGet("statistics")]
        public IActionResult GetStatistics()
        {
            database = redisChannel.GetDatabase();
            string value = database.StringGet("statistics");
          Console.WriteLine(value);
            if (!String.IsNullOrEmpty(value))
            {
                return Ok(value.ToString());
            }
            return new NotFoundResult();
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

            SetDB(id);
            database.StringSet(id, data);

            AddIdToQueue(id);
            return id;
        }

    }
}
