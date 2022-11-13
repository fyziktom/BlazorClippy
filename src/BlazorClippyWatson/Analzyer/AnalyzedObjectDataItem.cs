using BlazorClippyWatson.AI;
using BlazorClippyWatson.Common;
using IBM.Watson.Assistant.v2.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.Scripting.OutputDescriptor;

namespace BlazorClippyWatson.Analzyer
{
    public class AnalyzedObjectDataItem
    {
        private CryptographyHelpers cryptHelper = new CryptographyHelpers();

        private readonly object _lock = new object();
        /// <summary>
        /// Id of analyzed data item
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Name of analyzed data item
        /// The name is used to create marker start
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Name without special characters
        /// </summary>
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
        /// <summary>
        /// Start of name of the marker
        /// </summary>
        [JsonIgnore]
        public string CapturedMarker { get => $"marker_{NameWithoutUnsuportedChars}"; }
        /// <summary>
        /// Hash of start of the marker
        /// </summary>
        [JsonIgnore]
        public string CapturedMarkerHash
        {
            get
            {
                lock (_lock)
                {
                    var hash = cryptHelper.GetHash(CapturedMarker);
                    return hash;
                }
            }
        }
        /// <summary>
        /// String with marks of found intents
        /// </summary>
        [JsonIgnore]
        public string FoundIntentsTotal 
        { 
            get
            {
                var res = "";
                foreach(var i in FoundIntents.OrderBy(e => e.Value.Intent))
                {
                    res += $"#{i.Value.Intent};";
                }
                return res;
            }
        }
        /// <summary>
        /// String with marks of found entities
        /// </summary>
        [JsonIgnore]
        public string FoundEntitiesTotal
        {
            get
            {
                var res = "";
                foreach (var e in FoundEntities.OrderBy(e => e.Value.Entity + e.Value.Value))
                {
                    res += $"@{e.Key};";
                }
                return res;
            }
        }
        /// <summary>
        /// Detailed marker which contains all actual found intents and entities marks
        /// </summary>
        [JsonIgnore]
        public string CapturedMarkerDetailed { get => $"marker_{NameWithoutUnsuportedChars}&&{FoundIntentsTotal}&&{FoundEntitiesTotal}"; }
        /// <summary>
        /// Hash of detailed marker
        /// </summary>
        [JsonIgnore]
        public string CapturedMarkerDetailedHash
        {
            get
            {
                lock (_lock)
                {
                    var hash = cryptHelper.GetHash(CapturedMarkerDetailed);
                    return hash;
                }
            }
        }
        /// <summary>
        /// List of all entities which should be found/collected
        /// </summary>
        public List<RuntimeEntity> Entities { get; set; } = new List<RuntimeEntity>();
        /// <summary>
        /// Dictionary of already found entities
        /// </summary>
        public Dictionary<string, RuntimeEntity> FoundEntities { get; set; } = new Dictionary<string, RuntimeEntity>();
        /// <summary>
        /// List of all intents which should be found/collected
        /// </summary>
        public List<RuntimeIntent> Intents { get; set; } = new List<RuntimeIntent>();
        /// <summary>
        /// Dictionary of already found intents
        /// </summary>
        public Dictionary<string, RuntimeIntent> FoundIntents { get; set; } = new Dictionary<string,RuntimeIntent>();
        /// <summary>
        /// If the all entities and intents was found this will be true.
        /// It is just simple detection by comparing count of lists and dictioanries.
        /// </summary>
        [JsonIgnore]
        public bool IsIdentified { get => Entities.Count == FoundEntities.Count && Intents.Count == FoundIntents.Count; }
        /// <summary>
        /// If this is set it will add found items to found dicts just when all intents and entities are together in one message. Otherwise it will be ignored.
        /// Example when to use it is if you have shared intent between different categories of intents.
        /// You can imagine it as "AND" operator between all intents and entities in list.
        /// </summary>
        public bool IsWhenAllOnly { get; set; } = false;
        /// <summary>
        /// Dictionary of all "DataItems" related to this analyzed objects. 
        /// It can be another analyzed item or "DataItem" as it is in VEFramework (some kind of attachement like JSON, txt, image, etc.).
        /// </summary>
        public Dictionary<string, object> DataItems { get; set; } = new Dictionary<string, object>();
        /// <summary>
        /// Clear list of all found intents and entities
        /// </summary>
        public void ClearAllFound()
        {
            FoundIntents.Clear();
            FoundEntities.Clear();
        }
        /// <summary>
        /// Function will check if the message contains some intents and entities which should be captured. If yes, it will save them.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Function will create all possible combinations of intents and entities
        /// </summary>
        /// <returns></returns>
        public List<string> GetAllDetailedMarksCombination()
        {
            var intentsCombinations = new List<string>();
            var entitiesCombinations = new List<string>();

            var lastCombo = string.Empty;
            foreach (var intent in Intents.OrderBy(i => i.Intent))
            {
                var tmp = lastCombo + $"#{intent.Intent};";
                var combo = $"{tmp}";
                lastCombo = combo;
                intentsCombinations.Add(lastCombo);
                if (!tmp.Contains($"marker_{NameWithoutUnsuportedChars}&&)"))
                {
                    var comboextra = $"marker_{NameWithoutUnsuportedChars}&&{tmp}&&;";
                    intentsCombinations.Add(comboextra);
                }
            }

            lastCombo = string.Empty;
            foreach (var entity in Entities.OrderBy(e => e.Entity + e.Value))
            {
                var tmp = lastCombo + $"@{entity.Entity}:{entity.Value};";
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
            if (!IsWhenAllOnly && (Intents.Count + Entities.Count) > 1)
            {
                foreach (var icombo in intentsCombinations.OrderBy(i => i))
                {
                    if (!final.Contains(icombo))
                        final.Add(icombo);

                    var combo = icombo;
                    foreach (var ecombo in entitiesCombinations.OrderBy(e => e))
                    {
                        if (icombo != ecombo)
                        {
                            if (!final.Contains(ecombo))
                                final.Add(ecombo);

                            if (!icombo.Contains($"marker_{NameWithoutUnsuportedChars}&&") && !ecombo.Contains($"marker_{NameWithoutUnsuportedChars}&&"))
                            {
                                combo = $"marker_{NameWithoutUnsuportedChars}&&{icombo.Trim()}&&{ecombo.Trim()}";
                                if (!final.Contains(combo))
                                    final.Add(combo);
                            }
                        }
                    }
                }

                foreach (var ecombo in entitiesCombinations.OrderBy(e => e))
                {
                    if (!final.Contains(ecombo))
                        final.Add(ecombo);

                    var combo = ecombo;
                    foreach (var icombo in intentsCombinations.OrderBy(i => i))
                    {
                        if (icombo != ecombo)
                        {
                            if (!final.Contains(icombo))
                                final.Add(icombo);

                            if (!icombo.Contains($"marker_{NameWithoutUnsuportedChars}&&") && !ecombo.Contains($"marker_{NameWithoutUnsuportedChars}&&"))
                            {
                                //combo = ecombo + " " + icombo;
                                combo = $"marker_{NameWithoutUnsuportedChars}&&{icombo.Trim()}&&{ecombo.Trim()}";
                                if (!final.Contains(combo))
                                    final.Add(combo);
                            }
                        }
                    }
                }
            }
            else if (IsWhenAllOnly || (Intents.Count + Entities.Count) == 1)
            {
                if (intentsCombinations.Count > 0 && entitiesCombinations.Count > 0)
                {
                    foreach (var icombo in intentsCombinations.OrderBy(i => i))
                    {
                        var combo = icombo;
                        foreach (var ecombo in entitiesCombinations.OrderBy(e => e))
                        {
                            if (icombo != ecombo)
                            {
                                if (!icombo.Contains($"marker_{NameWithoutUnsuportedChars}&&") && !ecombo.Contains($"marker_{NameWithoutUnsuportedChars}&&"))
                                {
                                    if (IsWhenAllOnly)
                                    {
                                        var alli = true;
                                        var alle = true;
                                        foreach (var e in Entities)
                                            if (!ecombo.Contains($"{e.Entity}:{e.Value}"))
                                                alle = false;
                                        
                                        foreach (var i in Intents)
                                            if (!icombo.Contains($"{i.Intent}"))
                                                alli = false;
                                        
                                        if (alli && alle)
                                        {
                                            combo = $"marker_{NameWithoutUnsuportedChars}&&{icombo.Trim()}&&{ecombo.Trim()}";
                                            if (!final.Contains(combo))
                                                final.Add(combo);
                                        }
                                    }
                                    else
                                    {
                                        combo = $"marker_{NameWithoutUnsuportedChars}&&&&{icombo.Trim()}";
                                        if (!final.Contains(combo))
                                            final.Add(combo);
                                    }
                                    combo = $"marker_{NameWithoutUnsuportedChars}&&{icombo.Trim()}&&{ecombo.Trim()}";
                                    if (!final.Contains(combo))
                                        final.Add(combo);
                                }
                            }
                        }
                    }
                }
                else if (intentsCombinations.Count > 0 && entitiesCombinations.Count == 0)
                {
                    foreach (var icombo in intentsCombinations.OrderBy(e => e))
                    {
                        var combo = string.Empty;
                        if (!icombo.Contains($"marker_{NameWithoutUnsuportedChars}&&"))
                        {
                            if (IsWhenAllOnly)
                            {
                                var all = true;
                                foreach (var i in Intents)
                                {
                                    if (!icombo.Contains($"{i.Intent}"))
                                        all = false;
                                }
                                if (all)
                                {
                                    combo = $"marker_{NameWithoutUnsuportedChars}&&{icombo.Trim()}&&;";
                                    if (!final.Contains(combo))
                                        final.Add(combo);

                                }
                            }
                            else
                            {
                                combo = $"marker_{NameWithoutUnsuportedChars}&&{icombo.Trim()}&&;";
                                if (!final.Contains(combo))
                                    final.Add(combo);
                            }
                        }
                    }
                }
                else if (intentsCombinations.Count == 0 && entitiesCombinations.Count > 0)
                {
                    foreach (var ecombo in entitiesCombinations.OrderBy(e => e))
                    {
                        var combo = string.Empty;
                        if (!ecombo.Contains($"marker_{NameWithoutUnsuportedChars}&&"))
                        {
                            if (IsWhenAllOnly)
                            {
                                var all = true;
                                foreach(var e in Entities)
                                {
                                    if (!ecombo.Contains($"{e.Entity}:{e.Value}"))
                                        all = false;
                                }
                                if (all)
                                {
                                    combo = $"marker_{NameWithoutUnsuportedChars}&&&&{ecombo.Trim()}";
                                    if (!final.Contains(combo))
                                        final.Add(combo);
                                }
                            }
                            else
                            {
                                combo = $"marker_{NameWithoutUnsuportedChars}&&&&{ecombo.Trim()}";
                                if (!final.Contains(combo))
                                    final.Add(combo);
                            }
                        }
                    }
                }
                
            }

            return final;
        }
    }
}
