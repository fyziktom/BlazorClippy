using BlazorClippyWatson.AI;
using IBM.Watson.Assistant.v2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                        mk = mk.Replace(split[i] + "; ", string.Empty);

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


        public static string GetMermaidFromDataItem(AnalyzedObjectDataItem dataitem)
        {
            var result = string.Empty;
            foreach (var line in GetMermaidFromDataItemLines(dataitem))
                result += line;
            return result;
        }
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
            yield return "\t}";
        }
    }
}
