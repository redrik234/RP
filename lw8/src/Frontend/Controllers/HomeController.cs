using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public async Task<IActionResult> Upload(string data)
        {
            var httpClient = new HttpClient();
            var dictionary = new Dictionary<string, string>()
            {
                { "data", data ?? "" },
            };

            var content = new FormUrlEncodedContent(dictionary);
            var response = await httpClient.PostAsync("http://127.0.0.1:5000/api/values", content);
            var result = await response.Content.ReadAsStringAsync();

            return new RedirectResult("http://127.0.0.1:5001/Home/ShowRank?=" + result);
        }

        public IActionResult ShowRank(string id)
        {
            string url = "http://127.0.0.1:5000/api/values/";
            string value = Get(url + id).Result;
            ViewData["Msg"] = value;
            return View();
        }
    }
}
