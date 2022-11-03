using BlazorClippyWatson.AI;
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

        public void AddDataItem(AnalyzedObjectDataItem dataItem)
        {
            if (!DataItems.ContainsKey(dataItem.CapturedMarker))
                DataItems.TryAdd(dataItem.CapturedMarker, dataItem);
        }
        public void RemoveDataItem(string dataItemMarker)
        {
            if (DataItems.TryRemove(dataItemMarker, out var di))
                return;
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
                di.Intents.Add(intent);
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
                di.Entities.Add(entity);
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

            foreach (var dataitem in DataItems)
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

            foreach (var dataitem in DataItems)
            {
                if(dataitem.Value.IsIdentified && !result.Contains(dataitem.Key))
                    result.Add(dataitem.Key);
            }
            return result;
        }

        public List<string> GetIdentifiedDataItemsMarkers()
        {
            var result = new List<string>();

            foreach (var dataitem in DataItems)
            {
                if (dataitem.Value.IsIdentified && !result.Contains(dataitem.Key))
                    result.Add(dataitem.Value.CapturedMarker);
            }
            return result;
        }

        public IEnumerable<AnalyzedObjectDataItem> GetIdentifiedDataItems()
        {
            foreach (var dataitem in DataItems)
            {
                if (dataitem.Value.IsIdentified)
                    yield return dataitem.Value;
            }
        }

        public List<string> GetIdentifiedDataItemsDetailedMarkers()
        {
            var result = new List<string>();

            foreach (var dataitem in DataItems)
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
    }
}
