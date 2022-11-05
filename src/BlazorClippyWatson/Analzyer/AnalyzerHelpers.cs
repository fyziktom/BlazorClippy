using BlazorClippyWatson.AI;
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
        private static readonly string mermaidDiagramType = "sequenceDiagram";
        /// <summary>
        /// Default name of Client
        /// </summary>
        private static readonly string defaultClientName = "Client";
        /// <summary>
        /// Default name of Assistant
        /// </summary>
        private static readonly string defaultAssistantName = "Assistant";
        /// <summary>
        /// Line end
        /// </summary>
        private static readonly string lineEnd = "\r\n";
        /// <summary>
        /// type of mark between participants in communication in diagram
        /// </summary>
        private static readonly string mermaidParticipantsRelationMark = "->>";
        /// <summary>
        /// Identifier of Intent
        /// </summary>
        private static readonly string intentMark = "i.";
        /// <summary>
        /// Identifier of Entity
        /// </summary>
        private static readonly string entityMark = "e.";
        /// <summary>
        /// Parameters must be spit with this char/string
        /// </summary>
        private static readonly string paramsSplitter = ",";
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
                        if (!string.IsNullOrEmpty(line) && !line.Contains(mermaidDiagramType) && line.Contains(mermaidParticipantsRelationMark))
                        {
                            var split = line.Split(":");
                            if (split != null && split.Length > 1)
                            {
                                var participants = split[0].Split(mermaidParticipantsRelationMark);
                                if (parcitipants != null && participants.Length > 0)
                                {
                                    dialogue.Participatns = parcitipants.ToList();
                                }
                                var parameters = string.Empty;
                                for (var i = 1; i < split.Length; i++)
                                    parameters += split[i] + (i == split.Length - 1 ? "" : ":");
                                var paramsSplit = parameters.Split(paramsSplitter);
                                if (paramsSplit != null && paramsSplit.Length > 0)
                                {
                                    var step = new DialogueStep();
                                    foreach (var p in paramsSplit)
                                    {
                                        if (p.Contains(intentMark))
                                            step.Intents.Add(p.Replace(intentMark, string.Empty).Trim(' '));
                                        if (p.Contains(entityMark))
                                        {
                                            var ps = p.Split(":");
                                            if (ps != null && ps.Length == 1)
                                                step.Entities.Add(ps[0].Replace(entityMark, string.Empty).Trim(' '), string.Empty);
                                            else if (ps != null && ps.Length == 2)
                                                step.Entities.Add(ps[0].Replace(entityMark, string.Empty).Trim(' '), ps[1].Trim(' '));
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
            var result = mermaidDiagramType + lineEnd;
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
                var stepline = $"\t{client}{mermaidParticipantsRelationMark}{assistant}: ";
                foreach(var intent in message.Response.Result.Output.Intents)
                    stepline += $"{intentMark}{intent.Intent}, ";
                foreach (var entity in message.Response.Result.Output.Entities)
                {
                    if (string.IsNullOrEmpty(entity.Value))
                        stepline += $"{entityMark}{entity.Entity}, ";
                    else
                        stepline += $"{entityMark}{entity.Entity}:{entity.Value}, ";
                }
                stepline += lineEnd;
                result += stepline;
            }

            return result;
        }
    }
}
