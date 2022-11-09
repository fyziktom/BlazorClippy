using BlazorClippyWatson.Analzyer;
using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin.Protocol;
using Newtonsoft.Json;
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
    public class QuestionResponseDto
    {
        public string answer { get; set; } = string.Empty;
        public WatsonMessageRequestRecord? MessageRecord { get; set; }
    }
    public class WatsonService
    {
        public WatsonService(HttpClient client, string url)
        {
            httpClient = client;
            WatsonApiUrl = url;
        }

        private readonly HttpClient httpClient;
        /// <summary>
        /// BlazorClippy.Demo.Server API Url (bridge to Watson Assistant cloud service).
        /// </summary>
        public string WatsonApiUrl { get; set; } = "https://localhost:7008/api";
        /// <summary>
        /// Messages records handler. It keeps history of conversation
        /// </summary>
        public WatsonMessageRecordsHandler MessageRecordHandler { get; set; } = new WatsonMessageRecordsHandler();
        /// <summary>
        /// Load watson instance
        /// </summary>
        /// <param name="watsonApiUrl"></param>
        /// <returns></returns>
        public async Task<(bool,string)> LoadWatson(string watsonApiUrl)
        {
            var res = await httpClient.GetStringAsync(watsonApiUrl + "/LoadWatson");
            if (!string.IsNullOrEmpty(res))
                return (true, res);
            else
                return (false, string.Empty);
        }

        /// <summary>
        /// Start watson session. The server will start instance of Watson Assistant if it was not loaded yet.
        /// You do not need to call LoadWatson separatelly.
        /// </summary>
        /// <param name="watsonApiUrl"></param>
        /// <returns></returns>
        public async Task<(bool,string)> StartWatsonSession(string watsonApiUrl)
        {
            var res = await httpClient.GetStringAsync(watsonApiUrl + "/StartWatsonSession");
            if (!string.IsNullOrEmpty(res))
                return (true, res);
            else
                return (false, string.Empty);
        }
        /// <summary>
        /// Ask watson specific question
        /// </summary>
        /// <param name="inputquestion"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public async Task<(bool,QuestionResponseDto?)> AskWatson(string inputquestion, string sessionId, WatsonAssistantAnalyzer? analyzer = null)
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
                try
                {
                    var result = JsonConvert.DeserializeObject<QuestionResponseDto>(resp);

                    var response = string.Empty;
                    var final = string.Empty;
                    var mainstring = string.Empty;

                    if (result != null && result.MessageRecord != null)
                    {
                        var r = result.MessageRecord.Response?.Result?.Output?.Generic?.FirstOrDefault();
                        if (r != null)
                            response = r.Text != null ? r.Text : string.Empty;

                        if (analyzer != null)
                        {
                            if (response.Contains(AnswerRulesHelpers.RuleStart) && response.Contains(AnswerRulesHelpers.RuleEnd))
                            {
                                var rules = AnswerRulesHelpers.ParseRules(response);
                                final = rules.Item1;
                                mainstring = rules.Item1;

                                foreach (var rule in rules.Item2)
                                {
                                    if (rule.Value.Type == AnswerRuleType.Condition)
                                    {
                                        foreach (var answer in analyzer.GetStringFromRule(rule.Value))
                                        {
                                            final += " " + answer.Item2;
                                        }
                                    }
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(final) && !string.IsNullOrEmpty(mainstring))
                            final = mainstring;
                        if (string.IsNullOrEmpty(final) && string.IsNullOrEmpty(mainstring))
                            final = response;

                        result.MessageRecord.Response?.Result?.Output?.Generic.Clear();
                        result.MessageRecord.Response?.Result?.Output?.Generic.Add(new RuntimeResponseGenericRuntimeResponseTypeText()
                        {
                            ResponseType = "text",
                            Text = final
                        });

                        MessageRecordHandler.SaveResponseToRecord(recordId, result.MessageRecord.Response);
                        return (true, result);
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("cannot parse the response of question request.");
                    return (false, null);
                }
            }
            else
            {
                Console.WriteLine("Cannot get answer for the question:" + inputquestion);
                return (false, null);
            }
            return (false, null);
        }
        /// <summary>
        /// Translate text
        /// </summary>
        /// <param name="text"></param>
        /// <param name="translateModel"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Synthetize text to voice
        /// </summary>
        /// <param name="text"></param>
        /// <param name="voice"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Get message history
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public IEnumerable<WatsonMessageRequestRecord> GetMessageHistory(string sessionId, bool descending = false)
        {
            return MessageRecordHandler.GetMessageHistory(sessionId, descending);
        }
        /// <summary>
        /// Get message by ID
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public WatsonMessageRequestRecord GetMessageById(string recordId)
        {
            return MessageRecordHandler.GetMessageRecordById(recordId);
        }

        /// <summary>
        /// Get all messages intents
        /// </summary>
        /// <returns></returns>
        public IEnumerable<List<RuntimeIntent>> GetMessagesIntents(string sessionId)
        {
            return MessageRecordHandler.GetMessagesIntents(sessionId);
        }
        /// <summary>
        /// Get all messages entities:values
        /// </summary>
        /// <returns></returns>
        public IEnumerable<List<RuntimeEntity>> GetMessagesEntities(string sessionId)
        {
            return MessageRecordHandler.GetMessagesEntities(sessionId);
        }
        /// <summary>
        /// Export message history
        /// </summary>
        /// <returns></returns>
        public string ExportMessageHistory()
        {
            return JsonConvert.SerializeObject(MessageRecordHandler.MessageRecords, Formatting.Indented);
        }
        /// <summary>
        /// Import message history
        /// </summary>
        /// <param name="importData"></param>
        public void ImportMessageHistory(string importData)
        {
            var import = JsonConvert.DeserializeObject<Dictionary<string, WatsonMessageRequestRecord>>(importData);
            if (import != null)
            {
                MessageRecordHandler.MessageRecords.Clear();
                foreach (var msg in import)
                    MessageRecordHandler.MessageRecords.TryAdd(msg.Key, msg.Value);
            }
        }
    }
}
