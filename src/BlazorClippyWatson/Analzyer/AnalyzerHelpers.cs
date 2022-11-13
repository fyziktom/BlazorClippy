using BlazorClippyWatson.AI;
using BlazorClippyWatson.Common;
using IBM.Watson.Assistant.v2.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VEDriversLite.NeblioAPI;

namespace BlazorClippyWatson.Analzyer
{
    public static class AnalyzerHelpers
    {
        /// <summary>
        /// type of mermaid diagram. it uses sequenceDiagram for now
        /// </summary>
        public static readonly string mermaidDialogueDiagramType = "sequenceDiagram";
        /// <summary>
        /// type of mermaid diagram for creating DataItems
        /// </summary>
        public static readonly string mermaidDataItemDiagramType = "classDiagram";
        /// <summary>
        /// Default name of Client
        /// </summary>
        public static readonly string defaultClientName = "Client";
        /// <summary>
        /// Default name of Assistant
        /// </summary>
        public static readonly string defaultAssistantName = "Assistant";
        /// <summary>
        /// Line end
        /// </summary>
        public static readonly string LineEnd = "\r\n";
        /// <summary>
        /// type of mark between participants in communication in diagram
        /// </summary>
        public static readonly string MermaidParticipantsRelationMark = "->>";
        /// <summary>
        /// Identifier of Intent in Mermaid
        /// </summary>
        public static readonly string MermaidIntentMark = "i.";
        /// <summary>
        /// Identifier of Intent in Marker
        /// </summary>
        public static readonly string MarkerIntentMark = "#";
        /// <summary>
        /// Identifier of Entity in Mermaid
        /// </summary>
        public static readonly string MermaidEntityMark = "e.";
        /// <summary>
        /// Identifier of Entity in Marker
        /// </summary>
        public static readonly string MarkerEntityMark = "@";
        /// <summary>
        /// Parameters must be spit with this char/string in Mermaid
        /// </summary>
        public static readonly string MermaidParamsSplitter = ",";
        /// <summary>
        /// Parameters must be spit with this char/string in Marker
        /// </summary>
        public static readonly string MarkerParamsSplitter = ";";
        /// <summary>
        /// Basic start of extensions marks. This string is followed by markers build by DataItems
        /// </summary>
        public static readonly string MarkerExtensionStartDefault = "&Markers: ";
        /// <summary>
        /// Watson assisent to create empty messages
        /// </summary>
        private static WatsonAssistant assistant = new WatsonAssistant();
        public static Dialogue GetDialogueFromMermaid(string mermaid, string sessionId)
        {
            var parcitipants = new List<string>();
            var dialogue = new Dialogue() { SessionId = sessionId };

            try
            {
                using (var reader = new StringReader(mermaid))
                {
                    for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                    {
                        if (!string.IsNullOrEmpty(line) && !line.Contains(mermaidDialogueDiagramType) && line.Contains(MermaidParticipantsRelationMark))
                        {
                            var split = line.Split(":");
                            if (split != null && split.Length > 1)
                            {
                                var participants = split[0].Split(MermaidParticipantsRelationMark);
                                if (parcitipants != null && participants.Length > 0)
                                {
                                    dialogue.Participatns = parcitipants.ToList();
                                }
                                var parameters = string.Empty;
                                for (var i = 1; i < split.Length; i++)
                                    parameters += split[i] + (i == split.Length - 1 ? "" : ":");
                                var paramsSplit = parameters.Split(MermaidParamsSplitter);
                                if (paramsSplit != null && paramsSplit.Length > 0)
                                {
                                    var step = new DialogueStep();
                                    foreach (var p in paramsSplit)
                                    {
                                        if (p.Contains(MermaidIntentMark))
                                            step.Intents.Add(p.Replace(MermaidIntentMark, string.Empty).Trim(' '));
                                        if (p.Contains(MermaidEntityMark))
                                        {
                                            var ps = p.Split(":");
                                            if (ps != null && ps.Length == 1)
                                                step.Entities.Add(new KeyValuePair<string, string>(ps[0].Replace(MermaidEntityMark, string.Empty).Trim(' '), string.Empty));
                                            else if (ps != null && ps.Length == 2)
                                                step.Entities.Add(new KeyValuePair<string, string>(ps[0].Replace(MermaidEntityMark, string.Empty).Trim(' '), ps[1].Trim(' ')));
                                        }
                                    }

                                    dialogue.Messages.Add(assistant.MessageRecordHandler.GetEmptyMessageDto
                                        (
                                            dialogue.SessionId,
                                            "",
                                            step.Intents,
                                            step.Entities
                                        ));
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Cannot parse the mermaid: " + ex.Message);
            }

            return dialogue;
        }

        /// <summary>
        /// Get mermaid based on dialogue
        /// </summary>
        /// <param name="dialogue"></param>
        /// <returns></returns>
        public static string GetMermaidFromDialogue(Dialogue dialogue)
        {
            var result = mermaidDialogueDiagramType + LineEnd;
            var client = defaultClientName;
            var assistant = defaultAssistantName;

            if (dialogue.Participatns != null && dialogue.Participatns.Count == 2)
            {
                client = dialogue.Participatns[0];
                assistant = dialogue.Participatns[1];
            }
            else if (dialogue.Participatns != null && dialogue.Participatns.Count == 1)
            {
                client = dialogue.Participatns[0];
            }

            foreach (var message in dialogue.Messages)
            {
                var stepline = $"\t{client}{MermaidParticipantsRelationMark}{assistant}: ";
                foreach(var intent in message.Response.Result.Output.Intents)
                    stepline += $"{MermaidIntentMark}{intent.Intent}, ";
                foreach (var entity in message.Response.Result.Output.Entities)
                {
                    if (string.IsNullOrEmpty(entity.Value))
                        stepline += $"{MermaidEntityMark}{entity.Entity}, ";
                    else
                        stepline += $"{MermaidEntityMark}{entity.Entity}:{entity.Value}, ";
                }
                stepline += LineEnd;
                result += stepline;
            }

            return result;
        }

        /// <summary>
        /// Function will take actual Marker and remove all marks which were in last Marker from previous step of dialogue
        /// </summary>
        /// <param name="lastMarker"></param>
        /// <param name="actualMarker"></param>
        /// <returns></returns>
        public static string RemoveLastMarksFromMarker(string lastMarker, string actualMarker)
        {
            if (string.IsNullOrEmpty(actualMarker))
                return string.Empty;

            if (string.IsNullOrEmpty(lastMarker))
                return actualMarker;

            var split = lastMarker.Split(new[] { ": ", "; " }, StringSplitOptions.RemoveEmptyEntries);
            var mk = actualMarker;

            if (split != null && split.Length > 1)
                for (var i = 1; i < split.Length; i++)
                    if (!string.IsNullOrEmpty(split[i]) && !string.IsNullOrWhiteSpace(split[i]))
                        mk = mk.Replace(split[i].Trim(';') + "; ", string.Empty);

            return mk;
        }

        /// <summary>
        /// Parse Detailed Marker and convert it to one message which includes all intents and entities
        /// </summary>
        /// <param name="marker"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public static WatsonMessageRequestRecord? GetMessageFromMarker(string marker, string sessionId)
        {
            if (!string.IsNullOrEmpty(marker))
            {
                if (marker.Contains(AnalyzerHelpers.MarkerExtensionStartDefault))
                {
                    var intents = new List<string>();
                    var entities = new List<KeyValuePair<string, string>>();

                    var m = marker.Replace(AnalyzerHelpers.MarkerExtensionStartDefault, string.Empty);
                    var split = m.Split("; ");

                    for (var i = 0; i < split.Length; i++)
                    {
                        var displit = split[i].Split("&&");
                        if (displit != null && displit.Length > 0)
                        {
                            var name = displit[0];
                            var di = new AnalyzedObjectDataItem() { Name = name };

                            foreach (var item in displit.Where(d => !string.IsNullOrEmpty(d)))
                            {
                                if (item.Contains(AnalyzerHelpers.MarkerParamsSplitter))
                                {
                                    var itemsplit = item.Split(AnalyzerHelpers.MarkerParamsSplitter);
                                    if (itemsplit != null && itemsplit.Length > 0)
                                    {
                                        foreach (var its in itemsplit)
                                        {
                                            if (its.Contains(AnalyzerHelpers.MarkerIntentMark))
                                            {
                                                var tmp = its.Replace(AnalyzerHelpers.MarkerIntentMark, string.Empty).Trim(' ').Trim(';').Trim(',');
                                                if (!intents.Contains(tmp))
                                                    intents.Add(tmp);
                                            }
                                            else if (its.Contains(AnalyzerHelpers.MarkerEntityMark))
                                            {
                                                var e = GetEntityPair(its);
                                                if (!entities.Contains(e))
                                                    entities.Add(e);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (item.Contains(AnalyzerHelpers.MarkerIntentMark))
                                    {
                                        var tmp = item.Replace(AnalyzerHelpers.MarkerIntentMark, string.Empty).Trim(' ').Trim(';').Trim(',');
                                        if (!intents.Contains(tmp))
                                            intents.Add(tmp);
                                    }
                                    else if (item.Contains(AnalyzerHelpers.MarkerEntityMark))
                                    {
                                        var e = GetEntityPair(item);
                                        if (!entities.Contains(e))
                                            entities.Add(e);
                                    }
                                }
                            }
                        }
                    }

                    var msg = assistant.MessageRecordHandler.GetEmptyMessageDto(sessionId,
                                                                                "",
                                                                                intents.OrderBy(i => i).ToList(),
                                                                                entities.OrderBy(e => e.Key + e.Value).ToList());
                    return msg;
                }
            }
            return null;
        }

        /// <summary>
        /// Parse Detailed Marker and create enumerable of messages where each message include one of the intent or entity or its combination
        /// </summary>
        /// <param name="marker"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public static IEnumerable<WatsonMessageRequestRecord?> GetMessagesStepsFromMarker(string marker, string sessionId)
        {
            if (!string.IsNullOrEmpty(marker))
            {
                if (marker.Contains(AnalyzerHelpers.MarkerExtensionStartDefault))
                {
                    var intents = new List<string>();
                    var entities = new List<KeyValuePair<string,string>>();

                    var m = marker.Replace(AnalyzerHelpers.MarkerExtensionStartDefault, string.Empty);
                    var split = m.Split("; ");

                    for (var i = 0; i < split.Length; i++)
                    {
                        intents.Clear();
                        entities.Clear();

                        var displit = split[i].Split("&&");
                        if (displit != null && displit.Length > 0)
                        {
                            var name = displit[0];
                            var di = new AnalyzedObjectDataItem() { Name = name };

                            foreach (var item in displit.Where(d => !string.IsNullOrEmpty(d)))
                            {
                                if (item.Contains(AnalyzerHelpers.MarkerParamsSplitter))
                                {
                                    var itemsplit = item.Split(AnalyzerHelpers.MarkerParamsSplitter);
                                    if (itemsplit != null && itemsplit.Length > 0)
                                    {
                                        foreach (var its in itemsplit)
                                        {
                                            if (its.Contains(AnalyzerHelpers.MarkerIntentMark))
                                            {
                                                var tmp = its.Replace(AnalyzerHelpers.MarkerIntentMark, string.Empty).Trim(' ').Trim(';').Trim(',');
                                                if (!intents.Contains(tmp))
                                                    intents.Add(tmp);
                                            }
                                            else if (its.Contains(AnalyzerHelpers.MarkerEntityMark))
                                            {
                                                var e = GetEntityPair(its);
                                                if (!entities.Contains(e))
                                                    entities.Add(e);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (item.Contains(AnalyzerHelpers.MarkerIntentMark))
                                    {
                                        var tmp = item.Replace(AnalyzerHelpers.MarkerIntentMark, string.Empty).Trim(' ').Trim(';').Trim(',');
                                        if (!intents.Contains(tmp))
                                            intents.Add(tmp);
                                    }
                                    else if (item.Contains(AnalyzerHelpers.MarkerEntityMark))
                                    {
                                        var e = GetEntityPair(item);
                                        if (!entities.Contains(e))
                                            entities.Add(e);
                                    }
                                }
                            }
                        }

                        var msg = assistant.MessageRecordHandler.GetEmptyMessageDto(sessionId,
                                                                                    "",
                                                                                    intents.OrderBy(i => i).ToList(),
                                                                                    entities.OrderBy(e => e.Key + e.Value).ToList());
                        yield return msg;
                    }
                }
            }
        }

        
        private static KeyValuePair<string, string> GetEntityPair(string item)
        {
            var ps = item.Split(":");
            if (ps != null && ps.Length > 0)
            {
                var tmp = ps[0].Replace(AnalyzerHelpers.MarkerEntityMark, string.Empty).Trim(' ').Trim(';').Trim(',');
                var tmp1 = ps[1].Trim(' ').Trim(';').Trim(',');

                if (ps != null && ps.Length == 1)
                    return new KeyValuePair<string, string>(tmp, string.Empty);
                else if (ps != null && ps.Length == 2)
                    return new KeyValuePair<string, string>(tmp, tmp1);
            }
            return new KeyValuePair<string, string>(string.Empty, string.Empty);
        }

        public static IEnumerable<AnalyzedObjectDataItem> GetAnalyzedDataItemFromMermaid(string mermaid)
        {
            var result = new AnalyzedObjectDataItem();

            using (var reader = new StringReader(mermaid))
            {
                for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    line = line.Replace("\t", string.Empty).Trim();
                    if (!string.IsNullOrEmpty(line) && !line.Contains(mermaidDataItemDiagramType))
                    {
                        if (line.Contains("}"))
                        {
                            yield return result;
                        }
                        else
                        {
                            if (line.Contains("class"))
                            {
                                result = new AnalyzedObjectDataItem();

                                var split = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                                if (split != null && split.Length > 1)
                                {
                                    result.Name = split[1].Replace(" {", string.Empty).Replace("{", string.Empty);
                                }
                            }
                            else if (line.Contains("+"))
                            {
                                var split = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                                if (split != null && split.Length > 1)
                                {
                                    if (split[0].ToLower().Contains("intent"))
                                    {
                                        if (split[1] != null)
                                        {
                                            result.Intents.Add(new RuntimeIntent()
                                            {
                                                Intent = split[1],
                                            });
                                        }
                                    }
                                    else if (split[0].ToLower().Contains("entity"))
                                    {
                                        var spp = string.Empty;
                                        for (var i = 1; i < split.Length; i++)
                                            spp += split[i] + " ";
                                        spp = spp.Remove(spp.Length - 1, 1);

                                        var sp = spp.Split(":", StringSplitOptions.RemoveEmptyEntries);
                                        if (sp != null && sp.Length > 0)
                                        {
                                            if (sp.Length == 1)
                                                result.Entities.Add(new RuntimeEntity()
                                                {
                                                    Entity = sp[0],
                                                    Value = string.Empty
                                                });
                                            else
                                                result.Entities.Add(new RuntimeEntity()
                                                {
                                                    Entity = sp[0],
                                                    Value = sp[1]
                                                });
                                        }
                                    }
                                    else if (split[0].ToLower().Contains("iswhenonly"))
                                    {
                                        if (split[1].ToLower().Contains("true"))
                                            result.IsWhenAllOnly = true;
                                        else
                                            result.IsWhenAllOnly = false;
                                    }

                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get combos with information about level of dialogue. It means how many markers it has
        /// </summary>
        /// <param name="combinations"></param>
        /// <returns></returns>
        public static Dictionary<KeyValuePair<string, string>, int> GetCombosWithCountOfMarkers(Dictionary<string, string>? combinations)
        {
            var combosWithCountOfMarkers = new Dictionary<KeyValuePair<string, string>, int>();
            foreach (var combo in combinations)
            {
                var a = combo.Value.Split("marker_");
                combosWithCountOfMarkers.Add(combo, a.Length - 1);
            }
            return combosWithCountOfMarkers;
        }

        /// <summary>
        /// Get dialogue step for specific combos set
        /// </summary>
        /// <param name="item"></param>
        /// <param name="combos"></param>
        /// <param name="allBaseItems"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public static DialogueStep? GetDialogueStepFromCombo(KeyValuePair<KeyValuePair<string, string>, int> item, Dictionary<KeyValuePair<string, string>, int> combos, string[] allBaseItems, string sessionId)
        {
            var msg = AnalyzerHelpers.GetMessageFromMarker(item.Key.Value, sessionId);

            var intents = new List<string>();
            var entities = new List<KeyValuePair<string, string>>();
            if (msg != null)
            {
                if (msg?.Response?.Result?.Output?.Intents != null && msg.Response.Result.Output.Intents.Count > 0)
                {
                    foreach (var intent in msg.Response.Result.Output.Intents)
                        intents.Add(intent.Intent);
                }
                if (msg?.Response?.Result?.Output?.Entities != null && msg.Response.Result.Output.Entities.Count > 0)
                {
                    foreach (var entity in msg.Response.Result.Output.Entities)
                        entities.Add(new KeyValuePair<string, string>(entity.Entity, entity.Value));
                }
            }

            var it = new DialogueStep()
            {
                Marker = item.Key.Value,
                MarkerHash = item.Key.Key,
                Level = item.Value,
                Intents = intents,
                Entities = entities,
                MessageRecord = msg
            };
            
            var nexts = GetDialogueStepPossibleNexts(it.Marker, allBaseItems);
            it.PossibleNextSteps = new List<string>(nexts);

            return it;
        }

        /// <summary>
        /// Get possible next steps for the marker
        /// </summary>
        /// <param name="marker"></param>
        /// <param name="allBaseItems"></param>
        /// <returns></returns>
        public static List<string> GetDialogueStepPossibleNexts(string marker, string[] allBaseItems)
        {
            var result = new List<string>();
            var actualSplit = marker.Split(new[] { ": ", "; " }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < actualSplit.Length; i++)
                actualSplit[i] = actualSplit[i].Trim().Trim(';') + ";";

            var possibleNextItems = new string[allBaseItems.Length];
            for (var i = 0; i < allBaseItems.Length; i++)
            {
                if (!actualSplit.Contains(allBaseItems[i].Trim().Trim(';') + ";"))
                    possibleNextItems[i] = (allBaseItems[i].Trim().Trim(';') + ";");
            }

            for (var i = 0; i < possibleNextItems.Length; i++)
            {
                if (possibleNextItems[i] != null)
                {
                    var markerFull = marker.Trim() + " " + possibleNextItems[i].Trim().Trim(';') + ";";
                    var ch = new CryptographyHelpers();
                    var hash = ch.GetHash(markerFull.Trim());

                    result.Add(hash);
                }
            }
            return result;
        }

        /// <summary>
        /// Get possible previous steps for this marker
        /// </summary>
        /// <param name="allBaseItems"></param>
        /// <param name="marker"></param>
        /// <param name="its"></param>
        /// <returns></returns>
        public static List<string> GetDialogueStepPossiblePrevious(string[] allBaseItems, string marker, KeyValuePair<KeyValuePair<string,string>,int>[] its)
        {
            var prevsteps = new List<string>();
            var actualSplit = marker.Split(new[] { ": ", "; " }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < actualSplit.Length; i++)
                actualSplit[i] = actualSplit[i].Trim().Trim(';') + ";";

            for (var i = 0; i < its.Length; i++)
            {
                if (!its[i].Key.Value.Contains(actualSplit[actualSplit.Length - 1]))
                    prevsteps.Add(its[i].Key.Key);
            }
            return prevsteps;
        }

        /// <summary>
        /// Find longest combo in all combos with count of markers, take it and split to array of separated markers.
        /// It means it returns array with all possible markers in these combos
        /// </summary>
        /// <param name="combosWithCountOfMarkers"></param>
        /// <returns></returns>
        public static string[] GetAllBaseItemsForCombos(Dictionary<KeyValuePair<string, string>, int> combosWithCountOfMarkers)
        {
            var fullItemN = combosWithCountOfMarkers.Where(c => c.Value > 0).Select(c => c.Value).Max();
            var fullItem = combosWithCountOfMarkers.FirstOrDefault(c => c.Value == fullItemN);
            var allBaseItems = new string[fullItemN];

            var fullItemSplit = fullItem.Key.Value.Split("marker_", StringSplitOptions.RemoveEmptyEntries).ToArray();
            if (fullItemSplit.Length > 1)
            {
                for (var i = 1; i < fullItemSplit.Length; i++)
                    allBaseItems[i - 1] = "marker_" + fullItemSplit[i].Trim();
            }
            return allBaseItems;
        }

        /// <summary>
        /// Get list of combos for each level. 
        /// Each item in list represents one level of dialogue. 
        /// Each item contains list of combos related to this level
        /// It is important for input of function GetDialogueStepPossiblePrevious
        /// </summary>
        /// <param name="combosWithCountOfMarkers"></param>
        /// <param name="limitOfLevel"></param>
        /// <returns></returns>
        public static List<KeyValuePair<KeyValuePair<string, string>, int>[]> GetAllCombosItemsListsBasedLevel(Dictionary<KeyValuePair<string, string>, int> combosWithCountOfMarkers, int limitOfLevel)
        {
            var combosByLevel = new List<KeyValuePair<KeyValuePair<string, string>, int>[]>();

            for (var i = 1; i <= limitOfLevel; i++)
            {
                var its = combosWithCountOfMarkers.Where(c => c.Value == i).Select(c => c).ToArray();
                if (its != null)
                    combosByLevel.Add(its);
            }
            return combosByLevel;
        }

        /// <summary>
        /// Get Dialogue with tree details of all possible ways through dialogue based on input combinations
        /// Function allows to provide precalculated lists to speed up calculation if you allready have them for different reasosn like calc steps separatelly
        /// </summary>
        /// <param name="combinations"></param>
        /// <param name="sessionId"></param>
        /// <param name="combosWithCountOfMarkers"></param>
        /// <param name="combosByLevel"></param>
        /// <param name="allBaseItems"></param>
        /// <returns></returns>
        public static Dialogue? GetDialogueFromCombos(Dictionary<string, string>? combinations, 
                                                      string sessionId = "default", 
                                                      int limitDepthOfPrevStepsSearch = 4,
                                                      Dictionary<KeyValuePair<string, string>, int>? combosWithCountOfMarkers = null,
                                                      List<KeyValuePair<KeyValuePair<string, string>, int>[]>? combosByLevel = null,
                                                      string[]? allBaseItems = null)
        {
            if (combinations == null)
                return null;

            var steps = new ConcurrentDictionary<string, DialogueStep>();

            if (combosWithCountOfMarkers == null)
                combosWithCountOfMarkers = GetCombosWithCountOfMarkers(combinations);
            if (allBaseItems == null)
                allBaseItems = GetAllBaseItemsForCombos(combosWithCountOfMarkers);
            var fullItemN = allBaseItems.Length;

            var items = combosWithCountOfMarkers.Where(c => c.Value > 0).Select(c => c).ToArray();
            Parallel.ForEach(items, (item) =>
            {
                var step = GetDialogueStepFromCombo(item, combosWithCountOfMarkers, allBaseItems, sessionId);
                steps.TryAdd(step.MarkerHash, step);
            });

            if (limitDepthOfPrevStepsSearch == -1)
                limitDepthOfPrevStepsSearch = fullItemN;

            var sts = steps.Values.Where(s => s.Level > 1 && s.Level <= limitDepthOfPrevStepsSearch).Select(s => s).ToList();
            if (combosByLevel == null)
                combosByLevel = GetAllCombosItemsListsBasedLevel(combosWithCountOfMarkers, limitDepthOfPrevStepsSearch);
            
            Parallel.ForEach(sts, (item) =>
            {
                var prevsteps = GetDialogueStepPossiblePrevious(allBaseItems, item.Marker, combosByLevel[item.Level - 2]);
                if (steps.TryGetValue(item.MarkerHash, out var step))
                    step.PossiblePreviousSteps = new List<string>(prevsteps);
                prevsteps = null;
            });

            if (steps != null)
            {
                var stepsValues = steps.Values.ToList();
                if (stepsValues == null)
                    stepsValues = new List<DialogueStep>();

                var result = new Dialogue()
                {
                    Steps = stepsValues,
                    SessionId = sessionId,
                    Participatns = new List<string>() { "Client", "Assistant" },
                };

                if (stepsValues.Count > 1)
                    foreach (var val in stepsValues)
                        if (val != null && val.MessageRecord != null)
                            result.Messages.Add(val.MessageRecord);

                return result;
            }

            return new Dialogue();
        }

        /// <summary>
        /// Convert Data Item to Mermaid class diagram
        /// </summary>
        /// <param name="dataitem"></param>
        /// <returns></returns>
        public static string GetMermaidFromDataItem(AnalyzedObjectDataItem dataitem)
        {
            var result = string.Empty;
            foreach (var line in GetMermaidFromDataItemLines(dataitem))
                result += line;
            return result;
        }
        /// <summary>
        /// Get separated lines of mermaid diagram from DataItem.
        /// Lines are combined together in function: GetMermaidFromDataItem
        /// </summary>
        /// <param name="dataitem"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetMermaidFromDataItemLines(AnalyzedObjectDataItem dataitem)
        {
            yield return $"\tclass {dataitem.Name + "{"}\r\n";

            foreach (var intent in dataitem.Intents)
                yield return $"\t\t+Intent {intent.Intent}\r\n";

            foreach (var entity in dataitem.Entities)
            {
                if (string.IsNullOrEmpty(entity.Value))
                    yield return $"\t\t+Entity {entity.Entity}\r\n";
                else
                    yield return $"\t\t+Entity {entity.Entity}:{entity.Value}\r\n";
            }
            yield return "\t}\r\n";
        }


        public static List<string> DefaultTestIntents = new List<string>()
        {
            "Co_je_to",
            "detektor_formát_dat",
            "detektor_připojení",
            "detektor_rozlišení",
            "detektor_scintilátor",
            "detektor_typ",
            "kontrola_pájení",
            "kontrola_svárů",
            "kontrola_umístění_komponenty",
            "manipulace_pozicování",
            "manipulace_vyložení",
            "manipulace_založení",
            "materiál_produktu",
            "my_máme",
            "onlinecall",
            "parametry",
            "potřeba_nečeho",
            "rentgenka_typ",
            "rentgenka_údržba",
            "rentgen_stínění",
            "ukládání_dat",
            "úvod",
            "velikost_detailu",
            "velikost_produktu",
            "využiji_rentgen"
        };
        public static List<string> DefaultTestEntities = new List<string>()
        {
            "defekt:",
            "defekt:prasklina",
            "defekt:void",
            "defekt:obecný",
            "defekt:studený spoj",
            "detektor:",
            "detektor:scintilátor",
            "detektor:detektor",
            "detektor:detekční element",
            "elektronika:",
            "elektronika:transformátor",
            "elektronika:plošný spoj",
            "elektronika:dioda",
            "elektronika:tranzistor",
            "elektronika:kondenzátor",
            "elektronika:rezistor",
            "elektronika:mikroprocesor",
            "elektronika:piny",
            "elektronika:cívka",
            "elektronika:pájení",
            "elektronika:klasické nožičky",
            "elektronika:smd",
            "itinfrastruktura:",
            "itinfrastruktura:firewall",
            "itinfrastruktura:kabel",
            "itinfrastruktura:cloud",
            "itinfrastruktura:tagy",
            "itinfrastruktura:RPI",
            "itinfrastruktura:monitor",
            "itinfrastruktura:server",
            "itinfrastruktura:notebook",
            "itinfrastruktura:mobil",
            "itinfrastruktura:myš",
            "itinfrastruktura:router",
            "itinfrastruktura:",
            "itinfrastruktura:počítač",
            "ittsoftware:",
            "ittsoftware:hosting",
            "ittsoftware:zdrojový kód",
            "ittsoftware:browser",
            "ittsoftware:backup",
            "ittsoftware:ransomware",
            "ittsoftware:local",
            "ittsoftware:databáze",
            "ittsoftware:IoT",
            "ittsoftware:NFT",
            "ittsoftware:aplikace",
            "ittsoftware:FTP",
            "ittsoftware:IPFS",
            "ittsoftware:UI",
            "ittsoftware:blockchain",
            "ittsoftware:node",
            "ittsoftware:UX",
            "ittsoftware:deployment",
            "ittsoftware:OpenSource",
            "ittsoftware:licence",
            "ittsoftware:orchestrační platforma",
            "ittsoftware:transakce",
            "Jára_Cimrman",
            "Jára_Cimrman:jára",
            "komponenta:",
            "komponenta:komponenta",
            "malý_produkt:",
            "malý_produkt:malý",
            "manipulace:",
            "manipulace:manipulace",
            "materiál:",
            "materiál:težký kov",
            "materiál:plast",
            "materiál:lehký kov",
            "parametry_RTG:",
            "parametry_RTG:rtg parametry",
            "podklady:",
            "podklady:3d model",
            "podklady:rozměry",
            "podklady:výkres",
            "podklady:vzorové snímky",
            "podklady:schéma",
            "produkt:",
            "produkt:produkt",
            "rentgen:",
            "rentgen:filament",
            "rentgen:zákony",
            "rentgen:kolimátor",
            "rentgen:poziční systém",
            "rentgen:stínění",
            "rentgen:rentgen",
            "rentgenka:",
            "rentgenka:rentgenka",
            "rentgenka:anodové napětí",
            "rentgenka:ohnisko",
            "rentgenka:anodový proud",
            "snímek:",
            "snímek:geometrická neostrost",
            "snímek:bitová hloubka",
            "snímek:rozlišení",
            "snímek:snímek",
            "snímek:digitální filtry",
            "snímek:zvětšení",
            "svár:",
            "svár:svár",
            "velký_produkt:",
            "velký_produkt:velký"
        };
    }
}
