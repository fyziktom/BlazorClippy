using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.AI
{
    public class WatsonMessageRecordsHandler
    {
        public ConcurrentDictionary<string, WatsonMessageRequestRecord> MessageRecords { get; set; } = new ConcurrentDictionary<string, WatsonMessageRequestRecord>();

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
        public void MarkRecordAsProcessing(string id)
        {
            if (MessageRecords.TryGetValue(id, out var wrr))
                wrr.Processing = true;
        }
        public void MarkRecordAsFinished(string id, bool success)
        {
            if (MessageRecords.TryGetValue(id, out var wrr))
            {
                wrr.Processing = false;
                wrr.Finished = true;
                wrr.Success = success;
            }
        }
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

        public WatsonMessageRequestRecord GetMessageRecordById(string recordId)
        {
            if (MessageRecords.TryGetValue(recordId, out var wrr))
                return wrr;
            else
                return null;
        }

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
