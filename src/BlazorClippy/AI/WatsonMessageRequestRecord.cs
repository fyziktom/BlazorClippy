using IBM.Cloud.SDK.Core.Http;
using IBM.Watson.Assistant.v2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippy.AI
{
    public class WatsonMessageRequestRecord
    {
        public WatsonMessageRequestRecord(string sessionId, string question)
        {
            SessionId = sessionId;
            Question = question;
            Id = $"{sessionId}_{Guid.NewGuid()}";
        }
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool Processing { get; set; } = false;
        public bool Finished { get; set; } = false;
        public bool Success { get; set; } = false;
        public string SessionId { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        private string _textResponse = string.Empty;
        public string TextResponse
        {
            get 
            { 
                if (string.IsNullOrEmpty(_textResponse) && Response != null && Response.Result != null)
                {
                    try
                    {
                        if (Response.Result.Output.Generic != null)
                        {
                            var res = Response.Result.Output.Generic.FirstOrDefault();
                            if (res != null && res.ResponseType == "text")
                            {
                                _textResponse = res.Text;
                                return res.Text;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Cannot parse the response object. " + ex.Message);
                        return string.Empty;
                    }
                }
                return _textResponse; 
            }
            set => _textResponse = value;
        }
        public DetailedResponse<MessageResponse> Response { get; set; } = new DetailedResponse<MessageResponse>();
    }
}
