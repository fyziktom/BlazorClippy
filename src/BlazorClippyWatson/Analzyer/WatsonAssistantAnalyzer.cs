using BlazorClippyWatson.AI;
using BlazorClippyWatson.Common;
using IBM.Watson.Assistant.v2.Model;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.Analzyer
{
    public class WatsonAssistantAnalyzer
    {
        private CryptographyHelpers cryptHelper = new CryptographyHelpers();
        
        private readonly object _lock = new object();

        private List<string> LastMatchedDataItemsState = new List<string>();
        [JsonIgnore]
        public bool IsSomeMatch { get => LastMatchedDataItemsState.Count > 0; }
        public static string MarkerExtensionStartDefault { get => "&Markers: ";}
        
        public ConcurrentDictionary<string, AnalyzedObjectDataItem> DataItems { get; set; } = new ConcurrentDictionary<string, AnalyzedObjectDataItem>();

        public IOrderedEnumerable<KeyValuePair<string, AnalyzedObjectDataItem>> DataItemsOrderedByName { get => DataItems.OrderBy(e => e.Value.Name); }

        public string MarkerExtension 
        { 
            get
            {
                var questionextension = MarkerExtensionStartDefault;
                if (LastMatchedDataItemsState.Count > 0)
                {
                    foreach (var d in LastMatchedDataItemsState)
                        questionextension += $"{d} ";
                }
                return questionextension ;
            } 
        }

        public Dictionary<string,DateTime> MarkerExtensionHashHistory { get; set; } = new Dictionary<string, DateTime>();
        private string _lastMarkerExtensionHash = string.Empty;
        public string MarkerExtensionHash
        {
            get
            {
                lock (_lock)
                {
                    var hash = cryptHelper.GetHash(MarkerExtension);
                    if (hash != _lastMarkerExtensionHash)
                    {
                        _lastMarkerExtensionHash = hash;
                        if (!MarkerExtensionHashHistory.ContainsKey(hash))
                            MarkerExtensionHashHistory.TryAdd(hash, DateTime.UtcNow);
                    }
                    return hash;
                }
            }
        }
        public ConcurrentDictionary<string, string> DataItemsCombinations { get; set; } = new ConcurrentDictionary<string, string>();
        public void AddDataItem(AnalyzedObjectDataItem dataItem)
        {
            if (!DataItems.ContainsKey(dataItem.CapturedMarker))
            {
                DataItems.TryAdd(dataItem.CapturedMarker, dataItem);
            }            
        }
        public void RemoveDataItem(string dataItemMarker)
        {
            if (DataItems.TryRemove(dataItemMarker, out var di))
            {
                return;
            }
        }
        public AnalyzedObjectDataItem GetDataItem(string dataItemMarker)
        {
            if (DataItems.TryGetValue(dataItemMarker, out var di))
                return di;
            else
                return null;
        }
        public AnalyzedObjectDataItem GetDataItemByDetailedMarker(string dataItemMarkerDetailed)
        {
            return DataItems.Values.FirstOrDefault(d => d.CapturedMarkerDetailed == dataItemMarkerDetailed);
        }

        public void AddDataItemIntent(string dataItemMarker, RuntimeIntent intent)
        {
            if (DataItems.TryGetValue(dataItemMarker, out var di))
            {
                di.Intents.Add(intent);
            }
        }
        public void RemoveDataItemIntent(string dataItemMarker, string intent)
        {
            if (DataItems.TryGetValue(dataItemMarker, out var di))
            {
                var dintent = di.Intents.FirstOrDefault(i => i.Intent == intent);
                if (dintent != null)
                {
                    var index = di.Intents.IndexOf(dintent);
                    di.Intents.RemoveAt(index);
                }
            }
        }
        public void AddDataItemEntity(string dataItemMarker, RuntimeEntity entity)
        {
            if (DataItems.TryGetValue(dataItemMarker, out var di))
            {
                di.Entities.Add(entity);
            }
        }

        public void RemoveDataItemEntity(string dataItemMarker, string entity, string value)
        {
            if (DataItems.TryGetValue(dataItemMarker, out var di))
            {
                var dentity = di.Entities.FirstOrDefault(i => i.Entity == entity && i.Value == value);
                if (dentity != null)
                {
                    var index = di.Entities.IndexOf(dentity);
                    di.Entities.RemoveAt(index);
                }
            }
        }

        public List<string> MatchDataItems(WatsonMessageRequestRecord message)
        {
            var result = new List<string>();

            foreach (var dataitem in DataItemsOrderedByName)
            {
                var res = dataitem.Value.IsMessageInterestMatch(message);
                if (res.Item1.Count > 0 || res.Item2.Count > 0)
                    result.Add(dataitem.Key);
            }
            return result;
        }

        public List<string> GetIdentifiedDataItemsKeys()
        {
            var result = new List<string>();

            foreach (var dataitem in DataItemsOrderedByName)
            {
                if(dataitem.Value.IsIdentified && !result.Contains(dataitem.Key))
                    result.Add(dataitem.Key);
            }
            return result;
        }

        public List<string> GetIdentifiedDataItemsMarkers()
        {
            var result = new List<string>();

            foreach (var dataitem in DataItemsOrderedByName)
            {
                if (dataitem.Value.IsIdentified && !result.Contains(dataitem.Key))
                    result.Add(dataitem.Value.CapturedMarker);
            }
            return result;
        }

        public IEnumerable<AnalyzedObjectDataItem> GetIdentifiedDataItems()
        {
            var result = new List<string>();
            foreach (var dataitem in DataItemsOrderedByName)
            {
                if (dataitem.Value.IsIdentified)
                {
                    result.Add(dataitem.Value.CapturedMarkerDetailed);
                    yield return dataitem.Value;
                }
            }
            LastMatchedDataItemsState = result;
        }

        public List<string> GetIdentifiedDataItemsDetailedMarkers()
        {
            var result = new List<string>();

            foreach (var dataitem in DataItemsOrderedByName)
            {
                if (dataitem.Value.IsIdentified && !result.Contains(dataitem.Key))
                    result.Add(dataitem.Value.CapturedMarkerDetailed);
            }
            LastMatchedDataItemsState = result;
            return result;
        }

        public string ExportDataItems()
        {
            return JsonConvert.SerializeObject(DataItems, Formatting.Indented);
        }

        public void ImportDataItems(string importData)
        {
            var import = JsonConvert.DeserializeObject<Dictionary<string, AnalyzedObjectDataItem>>(importData);
            if (import != null)
            {
                DataItems.Clear();
                foreach (var item in import)
                    DataItems.TryAdd(item.Key, item.Value);
            }
        }

        public IEnumerable<KeyValuePair<DateTime, (string, string)>> GetHistoryOfDialogue()
        {
            var result = new Dictionary<DateTime, (string, string)>();
            foreach (var history in MarkerExtensionHashHistory.OrderBy(h => h.Value))
            {
                if (DataItemsCombinations.TryGetValue(history.Key, out var combo))
                {
                    yield return new KeyValuePair<DateTime, (string, string)>(history.Value, (history.Key, combo));
                }
            }
        }


        public Dictionary<string, string> GetHashesOfAllCombinations()
        {
            var result = new Dictionary<string, string>();
            var combos = new List<List<string>>();

            Console.WriteLine("\tCreating individual combinations...");
            foreach(var dataitem in DataItemsOrderedByName)
            {
                var res = dataitem.Value.GetAllDetailedMarksCombination();

                foreach (var r in res)
                {
                    if (r.Contains(dataitem.Value.NameWithoutUnsuportedChars))
                        combos.Add(new List<string>() { r, null });
                }
            }

            Console.WriteLine("\tCalculation all possible combos...");
            var output = ComboHelpers.GetAllPossibleCombosOptimizedNotGeneric(combos);
            //var output = ComboHelpers.GetAllPossibleCombosOptimized<string>(combos);

            Console.WriteLine("\tCreating MarkersExtensions...");
            var cmbs = new ConcurrentBag<string>();
            Parallel.ForEach(output, item =>
            {
                var sb = new StringBuilder(MarkerExtensionStartDefault);
                
                var counter = 0;
                foreach (var i in item)
                {
                    sb.Append($"{i}");
                    
                    if (counter >= 0 && counter < item.Count - 1)
                        sb.Append($" ");

                    cmbs.Add(sb.ToString());
                    counter++;
                }
            });

            Console.WriteLine("\tCalculating hashes...");
            var fbag = new ConcurrentQueue<KeyValuePair<string, string>>();
            Parallel.ForEach(cmbs, item =>
            {
                var ch = new CryptographyHelpers();
                var hash = ch.GetHash(item);

                fbag.Enqueue(new KeyValuePair<string, string>(hash, item));

            });

            cmbs.Clear();
            cmbs = null;

            Console.WriteLine("\tCopying final items to result dictionary...");

            while (fbag.TryDequeue(out var item))
            {
                result.TryAdd(item.Key, item.Value);
            }

            DataItemsCombinations = new ConcurrentDictionary<string, string>(result);

            return result;
        }
    }
}
