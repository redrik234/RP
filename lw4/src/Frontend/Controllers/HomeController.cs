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
        private static string FRONTENTD_URL = "http://127.0.0.1:5000/";
        private static string BACKEND_URL = "http://127.0.0.1:5001/";

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

        private async Task<string> Get(string url)
        {            
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));                        

            var response = await httpClient.GetAsync(url);            
            string value = "";
            if (response.IsSuccessStatusCode)
            {
                value = await response.Content.ReadAsStringAsync();
            }
            else
            {
                value = response.StatusCode.ToString();
            }            

            return value;
        }

        [HttpPost]
        public IActionResult Upload([FromForm] string data)
        {
            //TODO: send data in POST request to backend and read returned id value from response
            string result = "";
            string url = BACKEND_URL + "/api/values";
            if(!String.IsNullOrEmpty(data))
            {
                result = Post(url, data).Result;
            }
            string Uri = FRONTENTD_URL + "Home/ShowRank?=" + result;
            return Redirect(Uri);
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


        public IActionResult ShowRank(string id)
        {
            string url = "api/values/";
            string value = Get(url + id).Result;
            ViewData["Msg"] = value;
            return View();
        }
    }
}
