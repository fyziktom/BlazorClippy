using IBM.Cloud.SDK.Core.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippy.AI
{
    public class WatsonService
    {
        public WatsonService(HttpClient client)
        {
            httpClient = client;
        }

        private readonly HttpClient httpClient;
        public string WatsonApiUrl { get; set; } = "https://blazorclippydemoserver20221101171158.azurewebsites.net/api";

        public async Task<(bool,string)> LoadWatson(string watsonApiUrl)
        {
            var res = await httpClient.GetStringAsync(watsonApiUrl + "/LoadWatson");
            if (!string.IsNullOrEmpty(res))
                return (true, res);
            else
                return (false, string.Empty);
        }

        public async Task<(bool,string)> StartWatsonSession(string watsonApiUrl)
        {
            var res = await httpClient.GetStringAsync(watsonApiUrl + "/StartWatsonSession");
            if (!string.IsNullOrEmpty(res))
                return (true, res);
            else
                return (false, string.Empty);
        }

        public async Task<(bool,string)> AskWatson(string inputquestion, string sessionId)
        {
            var data = new
            {
                sessionId = sessionId,
                text = inputquestion
            };

            var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage res = await httpClient.PostAsync(WatsonApiUrl + "/AskWatson", content);
            var resp = await res.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(resp))
                return (true, resp);
            else
            {
                Console.WriteLine("Cannot get answer for the question:" + inputquestion);
                return (false, string.Empty);
            }
        }
    }
}
