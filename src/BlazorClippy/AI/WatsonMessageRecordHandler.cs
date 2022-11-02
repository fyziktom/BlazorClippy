using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippy.AI
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

        public IEnumerable<WatsonMessageRequestRecord> GetMessageHistory(string sessionId)
        {
            return MessageRecords.Where(mr => mr.Key.Contains(sessionId))
                                 .Select(mr => mr.Value)
                                 .OrderByDescending(mr => mr.Timestamp);
        }

        public WatsonMessageRequestRecord GetMessageRecordById(string recordId)
        {
            if (MessageRecords.TryGetValue(recordId, out var wrr))
                return wrr;
            else
                return null;
        }
    }
}
