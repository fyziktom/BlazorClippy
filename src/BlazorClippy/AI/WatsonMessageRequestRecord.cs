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
                if (Response != null && Response.Result != null)
                {
                    try
                    {
                        if (Response.Result.Output.Generic != null)
                        {
                            var res = Response.Result.Output.Generic.FirstOrDefault();
                            if (res != null)// && res.ResponseType == "text")
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

        /// <summary>
        /// Fill object with source PVPanel
        /// </summary>
        /// <param name="panel"></param>
        public void Fill(WatsonMessageRequestRecord panel)
        {
            foreach (var param in typeof(WatsonMessageRequestRecord).GetProperties())
            {
                try
                {
                    if (param.CanWrite)
                    {
                        var value = param.GetValue(panel);
                        var paramname = param.Name;
                        var pr = typeof(WatsonMessageRequestRecord).GetProperties().FirstOrDefault(p => p.Name == paramname);
                        if (pr != null)
                            pr.SetValue(this, value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Cannot load parameter." + ex.Message);
                }
            }
        }

        /// <summary>
        /// Clone
        /// </summary>
        public WatsonMessageRequestRecord Clone()
        {
            var res = new WatsonMessageRequestRecord(SessionId, Question);
            foreach (var param in typeof(WatsonMessageRequestRecord).GetProperties())
            {
                try
                {
                    if (param.CanWrite)
                    {
                        var value = param.GetValue(this);
                        var paramname = param.Name;
                        var pr = typeof(WatsonMessageRequestRecord).GetProperties().FirstOrDefault(p => p.Name == paramname);
                        if (pr != null)
                            pr.SetValue(res, value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Cannot load parameter. " + ex.Message);
                }
            }
            return res;
        }
    }
}
