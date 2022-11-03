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
            get => _textResponse; 
            set => _textResponse = value;
        }
    }
}
