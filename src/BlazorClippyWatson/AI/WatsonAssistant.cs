using IBM.Cloud.SDK.Core.Authentication.Bearer;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2;
using IBM.Watson.Assistant.v2.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.AI
{
    public class WatsonAssistant
    {
        private string assistantVersion { get; set; } = "2021-11-27";
        public AssistantService? Service { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string AssistantId { get; set; } = string.Empty;
        public DateTime LastQuestionAsked { get; set; } = DateTime.UtcNow;
        public WatsonMessageRecordsHandler MessageRecordHandler { get; set; } = new WatsonMessageRecordsHandler();

        public async Task<(bool, (string, WatsonMessageRequestRecord?))> SendMessage(string message, string assistantId, string sessionId)
        {
            if (Service == null)
                return (false, ("Please initiate the Service.", null));

            try
            {
                LastQuestionAsked = DateTime.UtcNow;

                var result = Service.Message(
                assistantId: assistantId,
                sessionId: sessionId,
                input: new MessageInput()
                {
                    Text = message
                }
                );

                var recordId = MessageRecordHandler.AddRecord(sessionId, message);
                MessageRecordHandler.MarkRecordAsProcessing(recordId);
                if (!string.IsNullOrEmpty(recordId))
                {
                    try
                    {
                        MessageRecordHandler.SaveResponseToRecord(recordId, result);
                        var res = MessageRecordHandler.GetMessageRecordById(recordId);
                        if (res != null)
                            return (true, (res.TextResponse, res));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Cannot parse the response object. " + ex.Message);
                        return (false, (ex.Message,null));
                    }
                }
                return (false, (string.Empty, null));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot connect to assistant service: " + ex.Message);
                return (false, (ex.Message, null));
            }
        }

        public async Task<(bool, string)> CreateSession(string assistantId)
        {
            if (Service == null)
                return (false, "Please initiate the Service.");

            var result = Service.CreateSession(
                assistantId: assistantId
                );

            Console.WriteLine(result.Response);

            var sessionId = result.Result.SessionId;
            return (true, sessionId);
        }

        public async Task<(bool, string)> InitAssistantService(string apikey, string apiurlbase, string instanceId)
        {
            try
            {
                var url = apiurlbase + "/instances/" + instanceId;
                IamAuthenticator authenticator = new IamAuthenticator(
                    apikey: apikey);
                Service = new AssistantService(assistantVersion, authenticator);
                Service.SetServiceUrl(url);

                return (true, "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot connect to assistant service: " + ex.Message);
                return (false, ex.Message);
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

        public IEnumerable<List<RuntimeIntent>> GetMessagesIntents()
        {
            return MessageRecordHandler.GetMessagesIntents(SessionId);
        }
        public IEnumerable<List<RuntimeEntity>> GetMessagesEntities()
        {
            return MessageRecordHandler.GetMessagesEntities(SessionId);
        }

        public string ExportMessageHistory()
        {
            return JsonConvert.SerializeObject(MessageRecordHandler.MessageRecords, Formatting.Indented);
        }

        public void ImportMessageHistory(string importData)
        {
            var import = JsonConvert.DeserializeObject<Dictionary<string, WatsonMessageRequestRecord>>(importData);
            if (import != null)
            {
                MessageRecordHandler.MessageRecords.Clear();
                foreach(var msg in import)
                    MessageRecordHandler.MessageRecords.TryAdd(msg.Key, msg.Value);
            }
        }
    }
}
