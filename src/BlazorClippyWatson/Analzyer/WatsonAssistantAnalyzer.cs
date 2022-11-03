using BlazorClippyWatson.AI;
using BlazorClippyWatson.Common;
using IBM.Watson.Assistant.v2.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.Analzyer
{
    public class WatsonAssistantAnalyzer
    {
        public ConcurrentDictionary<string, AnalyzedObjectDataItem> DataItems { get; set; } = new ConcurrentDictionary<string, AnalyzedObjectDataItem>();

        public IOrderedEnumerable<KeyValuePair<string, AnalyzedObjectDataItem>> DataItemsOrderedByName { get => DataItems.OrderBy(e => e.Value.Name); }

        public Dictionary<string, string> DataItemsCombinations { get; set; } = new Dictionary<string, string>();
        public void AddDataItem(AnalyzedObjectDataItem dataItem)
        {
            if (!DataItems.ContainsKey(dataItem.CapturedMarker))
            {
                DataItems.TryAdd(dataItem.CapturedMarker, dataItem);
                RefreshCombinations();
            }            
        }
        public void RemoveDataItem(string dataItemMarker)
        {
            if (DataItems.TryRemove(dataItemMarker, out var di))
            {
                RefreshCombinations();
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
                RefreshCombinations();
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
                    RefreshCombinations();
                }
            }
        }
        public void AddDataItemEntity(string dataItemMarker, RuntimeEntity entity)
        {
            if (DataItems.TryGetValue(dataItemMarker, out var di))
            {
                di.Entities.Add(entity);
                RefreshCombinations();
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
                    RefreshCombinations();
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
            foreach (var dataitem in DataItemsOrderedByName)
            {
                if (dataitem.Value.IsIdentified)
                    yield return dataitem.Value;
            }
        }

        public List<string> GetIdentifiedDataItemsDetailedMarkers()
        {
            var result = new List<string>();

            foreach (var dataitem in DataItemsOrderedByName)
            {
                if (dataitem.Value.IsIdentified && !result.Contains(dataitem.Key))
                    result.Add(dataitem.Value.CapturedMarkerDetailed);
            }
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

        public void RefreshCombinations()
        {
            var res = GetHashesOfAllCombinations();
            if (res != null)
                DataItemsCombinations = new Dictionary<string, string>(res);
        }
        public Dictionary<string, string> GetHashesOfAllCombinations()
        {
            var cryptHandler = new MD5();

            var result = new Dictionary<string, string>();
            var combos = new List<List<string>>();

            foreach (var dataitem in DataItemsOrderedByName)
            {
                var res = dataitem.Value.GetAllDetailedMarksCombination();
                combos.Add(res);
            }

            for (var i = 0; i < combos.Count; i++)
            {
                var icombo = combos[i];
                for (var k = 0; k < combos.Count; k++)
                {
                    if (i != k)
                    {
                        var kcombo = combos[k];

                        var ikcombo = new List<string>();
                        foreach (var icomb in icombo)
                        {
                            foreach (var kcomb in kcombo)
                            {
                                if (icomb != kcomb && !kcomb.Contains(icomb) && !icomb.Contains(kcomb))
                                {
                                    var ikc = icomb + " " + kcomb;
                                    if (!ikcombo.Contains(ikc))
                                        ikcombo.Add(ikc);
                                }
                                
                            }
                        }
                        foreach (var kcomb in kcombo)
                        {
                            foreach (var icomb in icombo)
                            {
                                if (kcomb != icomb && !kcomb.Contains(icomb) && !icomb.Contains(kcomb))
                                {
                                    var kic = kcomb + " " + icomb;
                                    if (!ikcombo.Contains(kic))
                                        ikcombo.Add(kic);
                                }
                            }
                        }

                        foreach(var ikc in ikcombo)
                        {
                            cryptHandler.Value = ikc;
                            var hash = cryptHandler.FingerPrint;
                            if (!result.ContainsKey(hash))
                                result.Add(hash, ikc);
                        }
                    }
                }
            }

            return result;
        }
    }
}
