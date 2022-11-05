using BlazorClippyWatson.AI;
using BlazorClippyWatson.Common;
using IBM.Watson.Assistant.v2.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        /// <summary>
        /// List of all last matched DataItems
        /// </summary>

        private List<string> LastMatchedDataItemsState = new List<string>();
        /// <summary>
        /// If the list of matched items contais some items this will be set.
        /// </summary>
        [JsonIgnore]
        public bool IsSomeMatch { get => LastMatchedDataItemsState.Count > 0; }
        /// <summary>
        /// Dictionary of all DataItems in this analyzer. These are the objects which sould be captured during dialogue
        /// </summary>
        public ConcurrentDictionary<string, AnalyzedObjectDataItem> DataItems { get; set; } = new ConcurrentDictionary<string, AnalyzedObjectDataItem>();
        /// <summary>
        /// Data Items list ordered by Name
        /// </summary>
        public IOrderedEnumerable<KeyValuePair<string, AnalyzedObjectDataItem>> DataItemsOrderedByName { get => DataItems.OrderBy(e => e.Value.Name); }
        /// <summary>
        /// Full marker extension of actual conversation
        /// </summary>
        public string MarkerExtension 
        { 
            get
            {
                var questionextension = AnalyzerHelpers.MarkerExtensionStartDefault;
                if (LastMatchedDataItemsState.Count > 0)
                {
                    foreach (var d in LastMatchedDataItemsState)
                        questionextension += $"{d} ";
                }
                return questionextension ;
            } 
        }

        /// <summary>
        /// Marker extension history. This can show the flow of the dialogue
        /// </summary>
        public Dictionary<string,DateTime> MarkerExtensionHashHistory { get; set; } = new Dictionary<string, DateTime>();
        private string _lastMarkerExtensionHash = string.Empty;
        /// <summary>
        /// Actual marker extension hash
        /// </summary>
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
        /// <summary>
        /// All data items combinations. This can show all possible ways of dialogue and possible stages of collecting of DataItems
        /// </summary>
        public ConcurrentDictionary<string, string> DataItemsCombinations { get; set; } = new ConcurrentDictionary<string, string>();
        /// <summary>
        /// Add DataItem which should be captured during dialogue
        /// </summary>
        /// <param name="dataItem"></param>
        public void AddDataItem(AnalyzedObjectDataItem dataItem)
        {
            if (!DataItems.ContainsKey(dataItem.CapturedMarker))
            {
                DataItems.TryAdd(dataItem.CapturedMarker, dataItem);
            }            
        }
        /// <summary>
        /// Remove data item
        /// </summary>
        /// <param name="dataItemMarker"></param>
        public void RemoveDataItem(string dataItemMarker)
        {
            if (DataItems.TryRemove(dataItemMarker, out var di))
            {
                return;
            }
        }
        /// <summary>
        /// Get exact data item based on its Id
        /// </summary>
        /// <param name="dataItemMarker"></param>
        /// <returns></returns>
        public AnalyzedObjectDataItem GetDataItem(string dataItemMarker)
        {
            if (DataItems.TryGetValue(dataItemMarker, out var di))
                return di;
            else
                return null;
        }
        /// <summary>
        /// Get data item based on detailed marker of the dataitem
        /// </summary>
        /// <param name="dataItemMarkerDetailed"></param>
        /// <returns></returns>
        public AnalyzedObjectDataItem GetDataItemByDetailedMarker(string dataItemMarkerDetailed)
        {
            return DataItems.Values.FirstOrDefault(d => d.CapturedMarkerDetailed == dataItemMarkerDetailed);
        }
        /// <summary>
        /// Add Intent to the DataItem
        /// </summary>
        /// <param name="dataItemMarker"></param>
        /// <param name="intent"></param>
        public void AddDataItemIntent(string dataItemMarker, RuntimeIntent intent)
        {
            if (DataItems.TryGetValue(dataItemMarker, out var di))
            {
                di.Intents.Add(intent);
            }
        }
        /// <summary>
        /// Remove Intent from the DataItem
        /// </summary>
        /// <param name="dataItemMarker"></param>
        /// <param name="intent"></param>
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
        /// <summary>
        /// Add Entity to the DataItem
        /// </summary>
        /// <param name="dataItemMarker"></param>
        /// <param name="entity"></param>
        public void AddDataItemEntity(string dataItemMarker, RuntimeEntity entity)
        {
            if (DataItems.TryGetValue(dataItemMarker, out var di))
            {
                di.Entities.Add(entity);
            }
        }
        /// <summary>
        /// Remove Entity from the DataItem
        /// </summary>
        /// <param name="dataItemMarker"></param>
        /// <param name="entity"></param>
        /// <param name="value"></param>
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
        /// <summary>
        /// Clear all found intents and entities in all DataItems
        /// </summary>
        public void ClearAllFoundInAllDataItems()
        {
            foreach (var dataitem in DataItemsOrderedByName)
                ClearAllFound(dataitem.Key);
        }
        /// <summary>
        /// Clear all found intents and entities in specific DataItem
        /// </summary>
        /// <param name="dataItemMarker"></param>
        public void ClearAllFound(string dataItemMarker)
        {
            if (DataItems.TryGetValue(dataItemMarker, out var di))
            {
                di.ClearAllFound();
            }
        }

        /// <summary>
        /// Try to match message in all DataItems
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get already identified DataItems Ids list
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Get already identified DataItems  markers
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Get list of all already identified DataItems
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Get already identified DataItems detailed markers
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Export all DataItems serialized to JSON
        /// </summary>
        /// <returns></returns>
        public string ExportDataItems()
        {
            return JsonConvert.SerializeObject(DataItems, Formatting.Indented);
        }

        /// <summary>
        /// Import DataItems from serialized JSON
        /// </summary>
        /// <param name="importData"></param>
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
        /// <summary>
        /// Get history of whole dialogue
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Get hashes of all combinations of all intents and entities across all DataItems. 
        /// This will refresh list of all possible combinations of acquisition of parameters during the dialogue.
        /// </summary>
        /// <returns></returns>
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
                var sb = new StringBuilder(AnalyzerHelpers.MarkerExtensionStartDefault);
                
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

            output.Clear();
            output = null;

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

        /// <summary>
        /// Play dialogue
        /// </summary>
        /// <param name="dialogue"></param>
        /// <param name="clearBeforePlay">clear all history and found items in analyzer before play</param>
        /// <returns></returns>
        public IEnumerable<KeyValuePair<string, List<string>>> PlayDialogue(Dialogue dialogue, bool clearBeforePlay)
        {
            if (dialogue != null)
            {
                if (clearBeforePlay)
                {
                    ClearAllFoundInAllDataItems();
                    LastMatchedDataItemsState = new List<string>();
                    MarkerExtensionHashHistory = new Dictionary<string, DateTime>();
                }

                foreach (var msg in dialogue.Messages)
                {
                    var res = MatchDataItems(msg);
                    var sdis = GetIdentifiedDataItemsDetailedMarkers();
                    var actualHash = MarkerExtensionHash;
                    var marker = MarkerExtension;
                    MarkerExtensionHashHistory.TryAdd(actualHash, DateTime.UtcNow);
                    yield return new KeyValuePair<string, List<string>>(actualHash, sdis);
                }
            }
        }
        
        /// <summary>
        /// Import DataItems combinations Detailed Markers and hashes
        /// Structure of data must be: hash\tmarker
        /// </summary>
        /// <param name="fileContent">you can fill content readed from file. each line is one item</param>
        /// <param name="fileName">fill filename if it should be loaded from file</param>
        public bool ImportDataItemCombinations(string fileContent = "", string fileName = "")
        {
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                fileContent = File.ReadAllText(fileName);

            if (!string.IsNullOrEmpty(fileContent))
            {
                using (var reader = new StringReader(fileContent))
                {
                    for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                    {
                        var split = line.Split("\t");
                        if (split != null && split.Length == 2)
                            DataItemsCombinations.TryAdd(split[0], split[1]);
                    }
                }
                return true;
            }
            return false;
        }
    }
}
