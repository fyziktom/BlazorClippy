using BlazorClippyWatson.AI;
using BlazorClippyWatson.Common;
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
        public AnalyzedObjectDataItem() 
        {
            cryptHandler = new MD5();
        }
        private MD5? cryptHandler;
        private readonly object _lock = new object();

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
        public string CapturedMarkerHash
        {
            get
            {
                lock (_lock)
                {
                    cryptHandler.Value = CapturedMarker;
                    var hash = cryptHandler.FingerPrint;
                    return hash;
                }
            }
        }
        [JsonIgnore]
        public string FoundIntentsTotal 
        { 
            get
            {
                var res = "";
                foreach(var i in FoundIntents.OrderBy(e => e.Value.Intent))
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
                foreach (var e in FoundEntities.OrderBy(e => e.Value.Entity + e.Value.Value))
                {
                    res += $"@{e.Key};" + "+";
                }
                if (!string.IsNullOrEmpty(res))
                    res = res.Remove(res.Length - 1, 1);
                return res;
            }
        }
        [JsonIgnore]
        public string CapturedMarkerDetailed { get => $"marker_{NameWithoutUnsuportedChars}&&{FoundIntentsTotal}&&{FoundEntitiesTotal}"; }
        [JsonIgnore]
        public string CapturedMarkerDetailedHash
        {
            get
            {
                lock (_lock)
                {
                    cryptHandler.Value = CapturedMarkerDetailed;
                    var hash = cryptHandler.FingerPrint;
                    return hash;
                }
            }
        }
        
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

        public List<string> GetAllDetailedMarksCombination()
        {
            var intentsCombinations = new List<string>();
            var entitiesCombinations = new List<string>();

            var lastCombo = string.Empty;
            foreach (var intent in Intents)
            {
                var tmp = lastCombo + $"#{intent.Intent};" + "+";
                if (!string.IsNullOrEmpty(tmp))
                    tmp = tmp.Remove(tmp.Length - 1, 1);

                var combo = $"{tmp}";
                lastCombo = combo;
                intentsCombinations.Add(lastCombo);
                if (!tmp.Contains($"marker_{NameWithoutUnsuportedChars}&&)"))
                {
                    var comboextra = $"marker_{NameWithoutUnsuportedChars}&&{tmp}&&";
                    intentsCombinations.Add(comboextra);
                }
            }

            lastCombo = string.Empty;
            foreach (var entity in Entities)
            {
                var tmp = lastCombo + $"@{entity.Entity}:{entity.Value};" + "+";
                if (!string.IsNullOrEmpty(tmp))
                    tmp = tmp.Remove(tmp.Length - 1, 1);
                var combo = $"{tmp}";
                lastCombo = combo;
                entitiesCombinations.Add(lastCombo);
                if (!tmp.Contains($"marker_{NameWithoutUnsuportedChars}&&)"))
                { 
                    var comboextra = $"marker_{NameWithoutUnsuportedChars}&&&&{tmp}";
                    entitiesCombinations.Add(comboextra);
                }
            }

            var final = new List<string>();
            foreach(var icombo in intentsCombinations)
            {
                if (!final.Contains(icombo))
                    final.Add(icombo);

                var combo = icombo;
                foreach (var ecombo in entitiesCombinations)
                {
                    if (icombo != ecombo)
                    {
                        if (!final.Contains(ecombo))
                            final.Add(ecombo);

                        if (!icombo.Contains($"marker_{NameWithoutUnsuportedChars}&&") && !ecombo.Contains($"marker_{NameWithoutUnsuportedChars}&&"))
                        {
                            combo = $"marker_{NameWithoutUnsuportedChars}&&{icombo}&&{ecombo}";
                            if (!final.Contains(combo))
                                final.Add(combo);
                        }
                    }
                }
            }
            
            foreach (var ecombo in entitiesCombinations)
            {
                if (!final.Contains(ecombo))
                    final.Add(ecombo);

                var combo = ecombo;
                foreach (var icombo in intentsCombinations)
                {
                    if (icombo != ecombo)
                    {
                        if (!final.Contains(icombo))
                            final.Add(icombo);

                        if (!icombo.Contains($"marker_{NameWithoutUnsuportedChars}&&") && !ecombo.Contains($"marker_{NameWithoutUnsuportedChars}&&"))
                        {
                            //combo = ecombo + " " + icombo;
                            combo = $"marker_{NameWithoutUnsuportedChars}&&{icombo}&&{ecombo}";
                            if (!final.Contains(combo))
                                final.Add(combo);
                        }
                    }
                }
            }
            
            return final;
        }
    }
}
