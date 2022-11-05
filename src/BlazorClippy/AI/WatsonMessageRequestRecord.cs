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
        /// <summary>
        /// Id of message record
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Timestamp of message record (not message as itself)
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// If some processing is running (for example waiting for response)
        /// </summary>
        public bool Processing { get; set; } = false;
        /// <summary>
        /// Record is finished (response received, etc.)
        /// </summary>
        public bool Finished { get; set; } = false;
        /// <summary>
        /// Messsage was captured successfully
        /// </summary>
        public bool Success { get; set; } = false;
        /// <summary>
        /// Session Id of the dialogue where message belongs
        /// </summary>
        public string SessionId { get; set; } = string.Empty;
        /// <summary>
        /// Question asked in this message
        /// </summary>
        public string Question { get; set; } = string.Empty;

        private string _textResponse = string.Empty;
        /// <summary>
        /// Text response for the asked question.
        /// If the object from Watson is provided it will take it from there. 
        /// If not it will provide private _textResposnse content.
        /// </summary>
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

        /// <summary>
        /// Fill object
        /// </summary>
        /// <param name="record"></param>
        public void Fill(WatsonMessageRequestRecord record)
        {
            foreach (var param in typeof(WatsonMessageRequestRecord).GetProperties())
            {
                try
                {
                    if (param.CanWrite)
                    {
                        var value = param.GetValue(record);
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
