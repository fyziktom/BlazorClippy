using BlazorClippyWatson.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.Analzyer
{
    public class Dialogue
    {
        /// <summary>
        /// Session Id of dialogue
        /// </summary>
        public string SessionId { get; set; } = string.Empty;
        /// <summary>
        /// Participants in dialogue
        /// </summary>
        public List<string> Participatns { get; set; } = new List<string>();
        /// <summary>
        /// Steps of the dialogue
        /// </summary>
        public List<DialogueStep> Steps { get; set; } = new List<DialogueStep>();
        /// <summary>
        /// Messages of the dialogue
        /// </summary>
        public List<WatsonMessageRequestRecord> Messages { get; set; } = new List<WatsonMessageRequestRecord>();
    }
}
