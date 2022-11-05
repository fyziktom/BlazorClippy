using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BlazorClippyWatson.AI
{
    public class WatsonMessageRecordsHandler
    {
        /// <summary>
        /// All recorded message dictionary
        /// </summary>
        public ConcurrentDictionary<string, WatsonMessageRequestRecord> MessageRecords { get; set; } = new ConcurrentDictionary<string, WatsonMessageRequestRecord>();

        /// <summary>
        /// Create empty message object. The function will init the Watson object with intent/entities lists.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="message"></param>
        /// <param name="intents">You can provide list of intents which will be added to message Watson object lists</param>
        /// <param name="entities">You can provide dictionary of entities:values which will be added to message Watson object lists</param>
        /// <returns></returns>
        public WatsonMessageRequestRecord GetEmptyMessageDto(string sessionId, string message = "", List<string>? intents = null, Dictionary<string,string>? entities = null)
        {
            var msg = new WatsonMessageRequestRecord(sessionId, message);
            msg.Response = new IBM.Cloud.SDK.Core.Http.DetailedResponse<MessageResponse>();
            msg.Response.Result = new MessageResponse();
            msg.Response.Result.Output = new MessageOutput();
            msg.Response.Result.Output.Generic = new List<RuntimeResponseGeneric>();
            var g = new RuntimeResponseGenericRuntimeResponseTypeText();
            g.Text = message;
            g.ResponseType = "text";
            msg.Response.Result.Output.Generic.Add((RuntimeResponseGeneric)g);
            
            msg.Response.Result.Output.Intents = new List<RuntimeIntent>();
            msg.Response.Result.Output.Entities = new List<RuntimeEntity>();

            if (intents != null)
                foreach (var i in intents)
                    msg.Response.Result.Output.Intents.Add(new RuntimeIntent()
                    {
                        Intent = i
                    });
            
            if (entities != null)
                foreach (var e in entities)
                    msg.Response.Result.Output.Entities.Add(new RuntimeEntity()
                    {
                        Entity = e.Key,
                        Value = e.Value
                    });

            return msg;
        }
        /// <summary>
        /// Add message record
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="question"></param>
        /// <returns>record Id</returns>
        public string AddRecord(string sessionId, string question)
        {
            var wrr = new WatsonMessageRequestRecord(sessionId, question);
            if (!MessageRecords.ContainsKey(sessionId))
            {
                MessageRecords.TryAdd(wrr.Id, wrr);
                return wrr.Id;
            }
            return string.Empty;
        }
        /// <summary>
        /// Mar record as processing
        /// </summary>
        /// <param name="id"></param>
        public void MarkRecordAsProcessing(string id)
        {
            if (MessageRecords.TryGetValue(id, out var wrr))
                wrr.Processing = true;
        }
        /// <summary>
        /// Mark record as finished. You can set the success status here
        /// </summary>
        /// <param name="id"></param>
        /// <param name="success"></param>
        public void MarkRecordAsFinished(string id, bool success)
        {
            if (MessageRecords.TryGetValue(id, out var wrr))
            {
                wrr.Processing = false;
                wrr.Finished = true;
                wrr.Success = success;
            }
        }
        /// <summary>
        /// Save Watson object response to message to message record
        /// </summary>
        /// <param name="id"></param>
        /// <param name="response"></param>
        public void SaveResponseToRecord(string id, DetailedResponse<MessageResponse> response)
        {
            if (MessageRecords.TryGetValue(id, out var wrr))
            {
                wrr.Response = response;
                wrr.Processing = false;
                wrr.Finished = true;

                if (response != null)
                    wrr.Success = true;
                else
                    wrr.Success = false;
            }
        }
        /// <summary>
        /// Save text response to the message record
        /// </summary>
        /// <param name="id"></param>
        /// <param name="response"></param>
        public void SaveResponseToRecord(string id, string response)
        {
            if (MessageRecords.TryGetValue(id, out var wrr))
            {
                wrr.TextResponse = response;
                wrr.Processing = false;
                wrr.Finished = true;

                if (!string.IsNullOrEmpty(response))
                    wrr.Success = true;
                else
                    wrr.Success = false;
            }
        }

        /// <summary>
        /// Get message history
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="descending"></param>
        /// <returns></returns>
        public IEnumerable<WatsonMessageRequestRecord> GetMessageHistory(string sessionId, bool descending = false)
        {
            if (descending)
                return MessageRecords.Where(mr => mr.Key.Contains(sessionId))
                                     .Select(mr => mr.Value)
                                     .OrderByDescending(mr => mr.Timestamp);
            else
                return MessageRecords.Where(mr => mr.Key.Contains(sessionId))
                                .Select(mr => mr.Value)
                                .OrderBy(mr => mr.Timestamp);
        }
        /// <summary>
        /// Find message by Id. It means Id of record which was recevied as result when the record was added.
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public WatsonMessageRequestRecord GetMessageRecordById(string recordId)
        {
            if (MessageRecords.TryGetValue(recordId, out var wrr))
                return wrr;
            else
                return null;
        }
        /// <summary>
        /// Get list of lists of intents across all messages
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public IEnumerable<List<RuntimeIntent>> GetMessagesIntents(string sessionId)
        {
            return MessageRecords.Where(mr => mr.Key.Contains(sessionId))
                                 .Select(mr => mr.Value)
                                 .OrderByDescending(mr => mr.Timestamp)
                                 .Where(mr => mr.Response != null)
                                 .Select(mr => mr.Response)
                                 .Where(r => r.Result.Output != null)
                                 .Select(r => r.Result.Output)
                                 .Where(r => r.Intents != null)
                                 .Select(r => r.Intents)
                                 .AsEnumerable();
        }
        /// <summary>
        /// Get messages based on input intents
        /// </summary>
        /// <param name="intents"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public IEnumerable<WatsonMessageRequestRecord> GetMessagesByIntents(List<RuntimeIntent> intents, string sessionId)
        {
            var msgs = MessageRecords.Where(mr => mr.Key.Contains(sessionId))
                                     .Select(mr => mr.Value)
                                     .OrderByDescending(mr => mr.Timestamp);

            var intentsNames = intents.Where(i => i.Intent != null).Select(i => i.Intent);

            foreach(var msg in msgs)
            {
                if (msg.Response != null && msg.Response.Result != null && msg.Response.Result.Output != null)
                {
                    if (msg.Response.Result.Output.Intents != null)
                    {
                        foreach(var intent in msg.Response.Result.Output.Intents)
                        {
                            if (intentsNames.Contains(intent.Intent))
                                yield return msg.Clone();
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Get Messages based on entities
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public IEnumerable<WatsonMessageRequestRecord> GetMessagesByEntities(List<RuntimeEntity> entities, string sessionId)
        {
            var msgs = MessageRecords.Where(mr => mr.Key.Contains(sessionId))
                                     .Select(mr => mr.Value)
                                     .OrderByDescending(mr => mr.Timestamp);

            var entitiesNames = entities.Where(i => i.Entity != null).Select(i => i.Entity);

            foreach (var msg in msgs)
            {
                if (msg.Response != null && msg.Response.Result != null && msg.Response.Result.Output != null)
                {
                    if (msg.Response.Result.Output.Intents != null)
                    {
                        foreach (var entity in msg.Response.Result.Output.Intents)
                        {
                            if (entitiesNames.Contains(entity.Intent))
                                yield return msg.Clone();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get list of lists of all entities across all the messages
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public IEnumerable<List<RuntimeEntity>> GetMessagesEntities(string sessionId)
        {
            return MessageRecords.Where(mr => mr.Key.Contains(sessionId))
                                 .Select(mr => mr.Value)
                                 .OrderByDescending(mr => mr.Timestamp)
                                 .Where(mr => mr.Response != null)
                                 .Select(mr => mr.Response)
                                 .Where(r => r.Result.Output != null)
                                 .Select(r => r.Result.Output)
                                 .Where(r => r.Entities != null)
                                 .Select(r => r.Entities)
                                 .AsEnumerable();
        }
    }
}
