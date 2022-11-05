using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
