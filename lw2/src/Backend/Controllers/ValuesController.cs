using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        static readonly ConcurrentDictionary<string, string> _data = new ConcurrentDictionary<string, string>();

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(string id)
        {
            string value = "";
            _data.TryGetValue(id, out value);
            return value;
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
            _data[id] = value;
            return id;
        }

    }
}
