using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Frontend.Models;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;

namespace Frontend.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload([FromForm] string data)
        {
            //TODO: send data in POST request to backend and read returned id value from response
            string result = "";
            string url = "http://127.0.0.1:5000/api/values";
            if(!String.IsNullOrEmpty(data))
            {
                result = Post(url, data).Result;
            }
            return Ok(result);
        }

        private async Task<string> Post(string url, string data)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            var encoding = "application/x-www-form-urlencoded";
            data = ("=" + data);
            var content = new StringContent(data, Encoding.UTF8, encoding);
            var response = await client.PostAsync(url, content);
            var id = await response.Content.ReadAsStringAsync();
            return id;
        }
    }
}
