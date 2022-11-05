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
        /// <summary>
        /// Assistant version
        /// </summary>
        private string assistantVersion { get; set; } = "2021-11-27";
        /// <summary>
        /// Watson Assistant service
        /// </summary>
        public AssistantService? Service { get; set; }
        /// <summary>
        /// Actual session Id
        /// </summary>
        public string SessionId { get; set; } = string.Empty;
        /// <summary>
        /// Assistant Id
        /// </summary>
        public string AssistantId { get; set; } = string.Empty;
        /// <summary>
        /// DateTime stamp of last asked question
        /// </summary>
        public DateTime LastQuestionAsked { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Message records handler with history of conversation
        /// </summary>
        public WatsonMessageRecordsHandler MessageRecordHandler { get; set; } = new WatsonMessageRecordsHandler();
        /// <summary>
        /// Send message to Watson Assistant cloud service
        /// </summary>
        /// <param name="message"></param>
        /// <param name="assistantId"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
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
        /// <summary>
        /// Create session with Watson Assistant cloud service
        /// </summary>
        /// <param name="assistantId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Initialize the Watson Assistant cloud service
        /// </summary>
        /// <param name="apikey"></param>
        /// <param name="apiurlbase"></param>
        /// <param name="instanceId"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get history of conversation
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public IEnumerable<WatsonMessageRequestRecord> GetMessageHistory(string sessionId)
        {
            return MessageRecordHandler.GetMessageHistory(sessionId);
        }
        /// <summary>
        /// Get specific message record by message Id
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
        public IEnumerable<List<RuntimeIntent>> GetMessagesIntents()
        {
            return MessageRecordHandler.GetMessagesIntents(SessionId);
        }
        /// <summary>
        /// Get all messages entities:values
        /// </summary>
        /// <returns></returns>
        public IEnumerable<List<RuntimeEntity>> GetMessagesEntities()
        {
            return MessageRecordHandler.GetMessagesEntities(SessionId);
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
                foreach(var msg in import)
                    MessageRecordHandler.MessageRecords.TryAdd(msg.Key, msg.Value);
            }
        }
    }
}
