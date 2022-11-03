using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BlazorClippy.AI
{
    public class WatsonService
    {
        public WatsonService(HttpClient client, string url)
        {
            httpClient = client;
            WatsonApiUrl = url;
        }

        private readonly HttpClient httpClient;
        public string WatsonApiUrl { get; set; } = "https://localhost:7008/api";
        public WatsonMessageRecordsHandler MessageRecordHandler { get; set; } = new WatsonMessageRecordsHandler();
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

            var recordId = MessageRecordHandler.AddRecord(sessionId, inputquestion);

            var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage res = await httpClient.PostAsync(WatsonApiUrl + "/AskWatson", content);
            var resp = await res.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(resp))
            {
                MessageRecordHandler.SaveResponseToRecord(recordId, resp);
                return (true, resp);
            }
            else
            {
                Console.WriteLine("Cannot get answer for the question:" + inputquestion);
                return (false, string.Empty);
            }
        }

        public async Task<(bool, string)> Translate(string text, string translateModel = "cs-en")
        {
            var data = new
            {
                text = text,
                translateModel = translateModel
            };

            var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage res = await httpClient.PostAsync(WatsonApiUrl + "/Translate", content);
            var resp = await res.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(resp))
            {
                return (true, resp);
            }
            else
            {
                Console.WriteLine("Cannot translate text:" + text);
                return (false, string.Empty);
            }
        }

        private byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public async Task<(bool, string)> Synthetise(string text, string voice = "en-US_LisaV3Voice")
        {
            var data = new
            {
                text = text,
                voice = voice
            };

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("audio/wav"));

            var content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(data), System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage res = await httpClient.PostAsync(WatsonApiUrl + "/Syntethise", content);

            //await res.Content.LoadIntoBufferAsync();
            var resp = await res.Content.ReadAsStreamAsync();
            
            if (resp != null && resp.Length > 0)
            {
                var rdata = ReadFully(resp);
                using (MemoryStream ms = new MemoryStream())
                {
                    //byte[] rdata;
                    //resp.Seek(0, SeekOrigin.Begin);

                    //await resp.CopyToAsync(ms);
                    //rdata = ms.ToArray();
                    var result = "data:audio/wav;base64," + Convert.ToBase64String(rdata);
                    Console.WriteLine($"Received {rdata.Length} bytes:\n");
                    //Console.WriteLine(result);
                    return (true, result);
                }
            }
            else
            {
                Console.WriteLine("Cannot synthetise the text:" + text);
                return (false, string.Empty);
            }
        }

        public IEnumerable<WatsonMessageRequestRecord> GetMessageHistory(string sessionId)
        {
            return MessageRecordHandler.GetMessageHistory(sessionId);
        }
        public WatsonMessageRequestRecord GetMessageById(string recordId)
        {
            return MessageRecordHandler.GetMessageRecordById(recordId);
        }
    }
}
