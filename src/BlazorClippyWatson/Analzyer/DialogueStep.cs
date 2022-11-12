using BlazorClippyWatson.AI;
using Org.BouncyCastle.Crypto.Tls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VEDriversLite.NeblioAPI;

namespace BlazorClippyWatson.Analzyer
{
    public class DialogueStep
    {
        /// <summary>
        /// List of intents for specific dialogue step
        /// </summary>
        public List<string> Intents { get; set; } = new List<string>();
        /// <summary>
        /// Dictionary of entities for specific dialogue step
        /// </summary>
        public List<KeyValuePair<string, string>> Entities { get; set; } = new List<KeyValuePair<string, string>>();
        /// <summary>
        /// Hashes of possible next steps in dialogue
        /// </summary>
        public List<string> PossibleNextSteps { get; set; } = new List<string>();
        /// <summary>
        /// Hashes of possible previous steps in dialogue
        /// </summary>
        public List<string> PossiblePreviousSteps { get; set; } = new List<string>();
        /// <summary>
        /// Actual Marker
        /// </summary>
        public string Marker { get; set; } = string.Empty;
        /// <summary>
        /// Marker Hash
        /// </summary>
        public string MarkerHash { get; set; } = string.Empty;
        /// <summary>
        /// Level of dialogue
        /// </summary>
        public int Level { get; set; } = 1;
        /// <summary>
        /// Message Record
        /// </summary>
        public WatsonMessageRequestRecord? MessageRecord { get; set; }

        /// <summary>
        /// Get Message Record object created from Marker
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public WatsonMessageRequestRecord? GetMessage(string sessionId)
        {
            if (!string.IsNullOrEmpty(Marker) && MessageRecord == null)
                MessageRecord = AnalyzerHelpers.GetMessageFromMarker(Marker, sessionId);
            
            return MessageRecord;
        }

    }
}
