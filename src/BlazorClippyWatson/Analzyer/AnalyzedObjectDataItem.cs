using BlazorClippyWatson.AI;
using IBM.Watson.Assistant.v2.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.Analzyer
{
    public class AnalyzedObjectDataItem
    {
        public string Name { get; set; } = string.Empty;

        [JsonIgnore]
        public string NameWithoutUnsuportedChars
        {
            get => Name.Replace(";", string.Empty)
                       .Replace(" ", string.Empty)
                       .Replace("$", string.Empty)
                       .Replace("#", string.Empty)
                       .Replace("!", string.Empty)
                       .Replace("%", string.Empty)
                       .Replace("*", string.Empty)
                       .Replace("~", string.Empty)
                       .Replace("+", string.Empty)
                       .Replace("@", string.Empty)
                       .Replace("&", string.Empty);
        }
        [JsonIgnore]
        public string CapturedMarker { get => $"marker_{NameWithoutUnsuportedChars}"; }
        [JsonIgnore]
        public string FoundIntentsTotal 
        { 
            get
            {
                var res = "";
                foreach(var i in FoundIntents)
                {
                    res += $"#{i.Value.Intent};" + "+";
                }
                if (!string.IsNullOrEmpty(res))
                    res = res.Remove(res.Length - 1, 1);
                return res;
            }
        }

        [JsonIgnore]
        public string FoundEntitiesTotal
        {
            get
            {
                var res = "";
                foreach (var e in FoundEntities)
                {
                    res += $"@{e.Key};" + "+";
                }
                if (!string.IsNullOrEmpty(res))
                    res = res.Remove(res.Length - 1, 1);
                return res;
            }
        }
        public string CapturedMarkerDetailed { get => $"marker_{NameWithoutUnsuportedChars}&&{FoundIntentsTotal}&&{FoundEntitiesTotal}"; }
        
        public List<RuntimeEntity> Entities { get; set; } = new List<RuntimeEntity>();
        public Dictionary<string, RuntimeEntity> FoundEntities { get; set; } = new Dictionary<string, RuntimeEntity>();
        public List<RuntimeIntent> Intents { get; set; } = new List<RuntimeIntent>();
        public Dictionary<string, RuntimeIntent> FoundIntents { get; set; } = new Dictionary<string,RuntimeIntent>();

        [JsonIgnore]
        public bool IsIdentified { get => Entities.Count == FoundEntities.Count && Intents.Count == FoundIntents.Count; }

        public bool IsWhenAllOnly { get; set; } = false;
        public Dictionary<string, object> DataItems { get; set; } = new Dictionary<string, object>();

        public (List<RuntimeIntent>, List<RuntimeEntity>) IsMessageInterestMatch(WatsonMessageRequestRecord message)
        {
            var ints = new List<RuntimeIntent>();
            var ents = new List<RuntimeEntity>();

            var isAllCounter = 0;

            foreach (var input in message.Response.Result.Output.Intents)
            {
                foreach (var item in Intents)
                {
                    if (item.Intent == input.Intent)
                    {
                        ints.Add(input);
                        if (!FoundIntents.ContainsKey(input.Intent))
                            FoundIntents.TryAdd(input.Intent, input);
                        isAllCounter++;
                    }
                }
            }

            foreach (var input in message.Response.Result.Output.Entities)
            {
                foreach (var item in Entities)
                {
                    if (item.Entity == input.Entity && (item.Value == input.Value || string.IsNullOrEmpty(item.Value)))
                    {
                        ents.Add(input);
                        if (!FoundEntities.ContainsKey($"{item.Entity}:{item.Value}"))
                            FoundEntities.TryAdd($"{item.Entity}:{item.Value}", input);
                        isAllCounter++;
                    }
                }
            }

            if (IsWhenAllOnly && !IsIdentified)
            {
                if (isAllCounter != Intents.Count + Entities.Count)
                {
                    ints.Clear();
                    ents.Clear();
                    FoundIntents.Clear();
                    FoundEntities.Clear();
                }
            }

            return (ints, ents);
        }
    }
}
