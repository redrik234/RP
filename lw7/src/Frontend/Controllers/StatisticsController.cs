using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Frontend.Controllers
{
    public class StatisticsController : Controller
    {
        private static string URL_BACKEND = "http://127.0.0.1:5000/";

        public IActionResult Index()
        {
            Task<string> statistics = GetStatistics(URL_BACKEND + "api/values/statistics");
            var data = statistics.Result.Split(":");

            IList<string> result = new List<string>()
            {
                "Количество обработанных текстов: " + data[0],
                "Количество текстов с высокой оценкой (выше 0.5): " + data[2],
                "Средняя оценка: " + data[1]
            };

            ViewData["Msg"] = result;

            return View();
        }

        private async Task<string> GetStatistics(string url)
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
    }
}