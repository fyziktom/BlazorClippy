using IBM.Cloud.SDK.Core.Authentication.Bearer;
using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2;
using IBM.Watson.Assistant.v2.Model;
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

        public async Task<(bool, string)> SendMessage(string message, string assistantId, string sessionId)
        {
            if (Service == null)
                return (false, "Please initiate the Service.");

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
                            return (true, res.TextResponse);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Cannot parse the response object. " + ex.Message);
                        return (false, ex.Message);
                    }
                }
                return (false, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot connect to assistant service: " + ex.Message);
                return (false, ex.Message);
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
    }
}
