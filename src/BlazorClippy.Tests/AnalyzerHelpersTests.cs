using BlazorClippyWatson.AI;
using BlazorClippyWatson.Analzyer;
using BlazorClippyWatson.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippy.Tests
{
    public class AnalyzerHelpersTests
    {
        [Fact]
        public void TestGetMermaidFromAnalyzedDataItem()
        {
            var item = new AnalyzedObjectDataItem();

            var mermaid = AnalyzerHelpers.GetMermaidFromDataItemLines(item).ToList();

            Assert.NotNull(mermaid);
            Assert.Equal(2, mermaid.Count);
            Assert.Contains("\tclass {\r\n", mermaid);

            item.Name = "name";
            mermaid = AnalyzerHelpers.GetMermaidFromDataItemLines(item).ToList();

            Assert.NotNull(mermaid);
            Assert.Equal(2, mermaid.Count);
            Assert.Contains("\tclass name{\r\n", mermaid);


            item.Intents.Add(new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "test"
            });

            mermaid = AnalyzerHelpers.GetMermaidFromDataItemLines(item).ToList();

            Assert.Equal(3, mermaid.Count);
            Assert.Contains("\t\t+Intent test\r\n", mermaid);

            item.Intents.Add(new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "test1"
            });

            mermaid = AnalyzerHelpers.GetMermaidFromDataItemLines(item).ToList();

            Assert.Equal(4, mermaid.Count);
            Assert.Contains("\t\t+Intent test\r\n", mermaid);
            Assert.Contains("\t\t+Intent test1\r\n", mermaid);

            item.Entities.Add(new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                 Entity = "ent",
                 Value = "val"
            });

            mermaid = AnalyzerHelpers.GetMermaidFromDataItemLines(item).ToList();

            Assert.Equal(5, mermaid.Count);
            Assert.Contains("\t\t+Entity ent:val\r\n", mermaid);

            item.Entities.Add(new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "ent",
                Value = string.Empty
            });

            mermaid = AnalyzerHelpers.GetMermaidFromDataItemLines(item).ToList();

            Assert.Equal(6, mermaid.Count);
            Assert.Contains("\t\t+Entity ent\r\n", mermaid);

            Assert.Equal("\t}\r\n", mermaid[mermaid.Count - 1]);

        }

        [Fact]
        public void GetMermaidFromDialogueTest()
        {
            var dialogue = new Dialogue();
            var messageHandler = new WatsonMessageRecordsHandler();

            var message = messageHandler.GetEmptyMessageDto("test",
                                    "test message",
                                    new List<string> { "intent1", "intent2" },
                                    new List<KeyValuePair<string, string>>
                                    {
                                        new KeyValuePair<string,string>("entity1", "evalue1"),
                                        new KeyValuePair<string,string>("entity2", "evalue2"),
                                        new KeyValuePair<string,string>("entity3", "evalue3")
                                    });
            var message1 = messageHandler.GetEmptyMessageDto("test1",
                                    "test message1",
                                    new List<string> { "intent3", "intent4" },
                                    new List<KeyValuePair<string, string>>
                                    {
                                        new KeyValuePair<string,string>("entity4", "evalue4"),
                                        new KeyValuePair<string,string>("entity1", "evalue1"),
                                        new KeyValuePair<string,string>("entity5", "evalue5")
                                    });
            dialogue.Messages.Add(message);
            dialogue.Messages.Add(message1);

            var mermaid = AnalyzerHelpers.GetMermaidFromDialogue(dialogue);

            var expected_result = "sequenceDiagram\r\n" +
                "\tClient->>Assistant: i.intent1, i.intent2, e.entity1:evalue1, e.entity2:evalue2, e.entity3:evalue3, \r\n" +
                "\tClient->>Assistant: i.intent3, i.intent4, e.entity4:evalue4, e.entity1:evalue1, e.entity5:evalue5, \r\n";

            Assert.Equal(expected_result, mermaid);
        }


        [Fact]
        public void GetDialogueFromMermaidTest()
        {
            var mermaid = "sequenceDiagram\r\n" +
                "\tClient->>Assistant: i.intent1, i.intent2, e.entity1:evalue1, e.entity2:evalue2, e.entity3:evalue3, \r\n" +
                "\tClient->>Assistant: i.intent3, i.intent4, e.entity4:evalue4, e.entity1:evalue1, e.entity5:evalue5, \r\n";
            var dialogue = AnalyzerHelpers.GetDialogueFromMermaid(mermaid, "test");

            Assert.NotNull(dialogue);
            Assert.Equal(2, dialogue.Participatns.Count);
            Assert.Contains("Client", dialogue.Participatns);
            Assert.Contains("Assistant", dialogue.Participatns);
            
            Assert.Equal("intent1", dialogue.Messages[0].Response.Result.Output.Intents[0].Intent);
            Assert.Equal("intent2", dialogue.Messages[0].Response.Result.Output.Intents[1].Intent);
            Assert.Equal("entity1", dialogue.Messages[0].Response.Result.Output.Entities[0].Entity);
            Assert.Equal("evalue1", dialogue.Messages[0].Response.Result.Output.Entities[0].Value);
            Assert.Equal("entity2", dialogue.Messages[0].Response.Result.Output.Entities[1].Entity);
            Assert.Equal("evalue2", dialogue.Messages[0].Response.Result.Output.Entities[1].Value);
            Assert.Equal("entity3", dialogue.Messages[0].Response.Result.Output.Entities[2].Entity);
            Assert.Equal("evalue3", dialogue.Messages[0].Response.Result.Output.Entities[2].Value);
                   
            Assert.Equal("intent3", dialogue.Messages[1].Response.Result.Output.Intents[0].Intent);
            Assert.Equal("intent4", dialogue.Messages[1].Response.Result.Output.Intents[1].Intent);
            Assert.Equal("entity4", dialogue.Messages[1].Response.Result.Output.Entities[0].Entity);
            Assert.Equal("evalue4", dialogue.Messages[1].Response.Result.Output.Entities[0].Value);
            Assert.Equal("entity1", dialogue.Messages[1].Response.Result.Output.Entities[1].Entity);
            Assert.Equal("evalue1", dialogue.Messages[1].Response.Result.Output.Entities[1].Value);
            Assert.Equal("entity5", dialogue.Messages[1].Response.Result.Output.Entities[2].Entity);
            Assert.Equal("evalue5", dialogue.Messages[1].Response.Result.Output.Entities[2].Value);
        }

        [Fact]
        public void RemoveLastMarksFromMarkerTest()
        {
            // this function will keep just new items in the marker

            var firstStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model;";
            var secondStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt;";
            var thirdStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt; marker_velikost_velka&&&&@velký_produkt:velký;";

            var result = AnalyzerHelpers.RemoveLastMarksFromMarker(firstStepMarker, secondStepMarker);
            Assert.Equal("&Markers: marker_materialproduktu&&&&@materiál:;@produkt:produkt;", result);
            var result1 = AnalyzerHelpers.RemoveLastMarksFromMarker(secondStepMarker, thirdStepMarker);
            Assert.Equal("&Markers: marker_velikost_velka&&&&@velký_produkt:velký;", result1);
            var result2 = AnalyzerHelpers.RemoveLastMarksFromMarker(firstStepMarker, thirdStepMarker);
            Assert.Equal("&Markers: marker_materialproduktu&&&&@materiál:;@produkt:produkt; marker_velikost_velka&&&&@velký_produkt:velký;", result2);

        }

        [Fact]
        public void GetMessageFromMarkerTest()
        {
            var firstStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model;";

            var message = AnalyzerHelpers.GetMessageFromMarker(firstStepMarker, "test");

            Assert.NotNull(message);
            Assert.Equal("my_máme", message.Response.Result.Output.Intents[0].Intent);
            Assert.Equal("podklady", message.Response.Result.Output.Entities[0].Entity);
            Assert.Equal("3d model", message.Response.Result.Output.Entities[0].Value);

            var secondStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt;";
            
            var message1 = AnalyzerHelpers.GetMessageFromMarker(secondStepMarker, "test");

            Assert.NotNull(message1);
            Assert.Equal("my_máme", message1.Response.Result.Output.Intents[0].Intent);
            Assert.Equal("materiál", message1.Response.Result.Output.Entities[0].Entity);
            Assert.Equal("", message1.Response.Result.Output.Entities[0].Value);
            Assert.Equal("podklady", message1.Response.Result.Output.Entities[1].Entity);
            Assert.Equal("3d model", message1.Response.Result.Output.Entities[1].Value);
            Assert.Equal("produkt", message1.Response.Result.Output.Entities[2].Entity);
            Assert.Equal("produkt", message1.Response.Result.Output.Entities[2].Value);
        }

        [Fact]
        public void GetMessageStepsFromMarker()
        {
            var marker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt; marker_velikost_velka&&&&@velký_produkt:velký;";

            var result = AnalyzerHelpers.GetMessagesStepsFromMarker(marker, "test").ToList();

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.NotNull(result[0]);
            Assert.Equal("my_máme", result[0].Response.Result.Output.Intents[0].Intent);
            Assert.Equal("podklady", result[0].Response.Result.Output.Entities[0].Entity);
            Assert.Equal("3d model", result[0].Response.Result.Output.Entities[0].Value);
            Assert.NotNull(result[1]);
            Assert.Equal("materiál", result[1].Response.Result.Output.Entities[0].Entity);
            Assert.Equal("", result[1].Response.Result.Output.Entities[0].Value);
            Assert.Equal("produkt", result[1].Response.Result.Output.Entities[1].Entity);
            Assert.Equal("produkt", result[1].Response.Result.Output.Entities[1].Value);
            Assert.NotNull(result[2]);
            Assert.Equal("velký_produkt", result[2].Response.Result.Output.Entities[0].Entity);
            Assert.Equal("velký", result[2].Response.Result.Output.Entities[0].Value);
        }

        [Fact]
        public void GetEntityValuePairTest()
        {
            var input = "podklady:3d model";
            var input1 = "produkt:test";
            var input2 = "velký:";
            var input3 = "materiál";

            var result = AnalyzerHelpers.GetEntityPair(input);
            Assert.Equal("podklady", result.Key);
            Assert.Equal("3d model", result.Value);
            var result1 = AnalyzerHelpers.GetEntityPair(input1);
            Assert.Equal("produkt", result1.Key);
            Assert.Equal("test", result1.Value);
            var result2 = AnalyzerHelpers.GetEntityPair(input2);
            Assert.Equal("velký", result2.Key);
            Assert.Equal("", result2.Value);
            var result3 = AnalyzerHelpers.GetEntityPair(input3);
            Assert.Equal("materiál", result3.Key);
            Assert.Equal("", result3.Value);
        }

        [Fact]
        public void GetDataItemFromMermaidTest()
        {
            var dataitemsMermaid = "classDiagram\r\n" +
                       "class dokumentace3dmodel{\r\n" +
                       "      +Intent my_máme\r\n" +
                       "      +Entity podklady:3d model\r\n" +
                       "      +IsWhenOnly true\r\n" +
                       "    }\r\n    " +
                       "class dokumentacevykres{\r\n" +
                       "      +Intent my_máme\r\n" +
                       "      +Entity podklady:3d výkres\r\n" +
                       "      +IsWhenOnly true\r\n" +
                       "    }";

            var dataitems = AnalyzerHelpers.GetAnalyzedDataItemFromMermaid(dataitemsMermaid).ToList();

            Assert.NotNull(dataitems);
            Assert.Equal(2, dataitems.Count);
            Assert.NotNull(dataitems[0]);
            Assert.Equal("dokumentace3dmodel", dataitems[0].Name);
            Assert.Single(dataitems[0].Intents);
            Assert.Single(dataitems[0].Entities);
            Assert.Equal("my_máme", dataitems[0].Intents[0].Intent);
            Assert.Equal("podklady", dataitems[0].Entities[0].Entity);
            Assert.Equal("3d model", dataitems[0].Entities[0].Value);

            Assert.NotNull(dataitems[1]);
            Assert.Equal("dokumentacevykres", dataitems[1].Name);
            Assert.Single(dataitems[1].Intents);
            Assert.Single(dataitems[1].Entities);
            Assert.Equal("my_máme", dataitems[1].Intents[0].Intent);
            Assert.Equal("podklady", dataitems[1].Entities[0].Entity);
            Assert.Equal("3d výkres", dataitems[1].Entities[0].Value);
        }

        [Fact]
        public void GetCombosWithCountOfMarkersTest()
        {
            var firstStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model;";
            var secondStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt;";
            var thirdStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt; marker_velikost_velka&&&&@velký_produkt:velký;";

            var combos = new Dictionary<string,string>();
            combos.Add("1234", firstStepMarker);
            combos.Add("12345", secondStepMarker);
            combos.Add("123456", thirdStepMarker);
            var result = AnalyzerHelpers.GetCombosWithCountOfMarkers(combos);

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Single(result.Where(v => v.Value == 1));
            Assert.Equal("1234", result.Where(v => v.Value == 1).Select(v => v.Key.Key).FirstOrDefault());
            Assert.Equal(firstStepMarker, result.Where(v => v.Value == 1).Select(v => v.Key.Value).FirstOrDefault());
            Assert.Single(result.Where(v => v.Value == 2));
            Assert.Equal("12345", result.Where(v => v.Value == 2).Select(v => v.Key.Key).FirstOrDefault());
            Assert.Equal(secondStepMarker, result.Where(v => v.Value == 2).Select(v => v.Key.Value).FirstOrDefault());
            Assert.Single(result.Where(v => v.Value == 3));
            Assert.Equal("123456", result.Where(v => v.Value == 3).Select(v => v.Key.Key).FirstOrDefault());
            Assert.Equal(thirdStepMarker, result.Where(v => v.Value == 3).Select(v => v.Key.Value).FirstOrDefault());

        }

        private Dictionary<KeyValuePair<string,string>, int> getComboDictWithCounts()
        {
            var firstStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model;";
            var secondStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt;";
            var thirdStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt; marker_velikost_velka&&&&@velký_produkt:velký;";

            /*
            var ch = new CryptographyHelpers();
            var firstHash = ch.GetHash(firstStepMarker, true); // CC2A87C146126A6B97455721F628B4F2
            var secondHash = ch.GetHash(secondStepMarker, true); // DD15947E69F75CB0C70F025FF06A5056
            var thirdHash = ch.GetHash(thirdStepMarker, true); // 682C957F7F0E5C41F53B752035D4901F
            */

            var combos = new Dictionary<string, string>();
            combos.Add("CC2A87C146126A6B97455721F628B4F2", firstStepMarker);
            combos.Add("DD15947E69F75CB0C70F025FF06A5056", secondStepMarker);
            combos.Add("682C957F7F0E5C41F53B752035D4901F", thirdStepMarker);
            var result = AnalyzerHelpers.GetCombosWithCountOfMarkers(combos);
            return result;
        }

        [Fact]
        public void GetAllBaseItemsFromCombosTest()
        {
            var combos = getComboDictWithCounts();
            var allBaseItems = AnalyzerHelpers.GetAllBaseItemsForCombos(combos);

            Assert.NotNull(allBaseItems);
            Assert.Equal(3, allBaseItems.Length);
            Assert.Equal("marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model;", allBaseItems[0]);
            Assert.Equal("marker_materialproduktu&&&&@materiál:;@produkt:produkt;", allBaseItems[1]);
            Assert.Equal("marker_velikost_velka&&&&@velký_produkt:velký;", allBaseItems[2]);

        }

        [Fact]
        public void GetAllCombosItemsListsBasedLevelTest()
        {
            var firstStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model;";
            var secondStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt;";
            var thirdStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt; marker_velikost_velka&&&&@velký_produkt:velký;";

            var combos = getComboDictWithCounts();
            var result = AnalyzerHelpers.GetAllCombosItemsListsBasedLevel(combos, combos.Count);

            Assert.NotNull(result);
            Assert.Equal(3, result.Count);

            Assert.Equal("CC2A87C146126A6B97455721F628B4F2", result[0][0].Key.Key);
            Assert.Equal(firstStepMarker, result[0][0].Key.Value);

            Assert.Equal("DD15947E69F75CB0C70F025FF06A5056", result[1][0].Key.Key);
            Assert.Equal(secondStepMarker, result[1][0].Key.Value);

            Assert.Equal("682C957F7F0E5C41F53B752035D4901F", result[2][0].Key.Key);
            Assert.Equal(thirdStepMarker, result[2][0].Key.Value);
        }

        [Fact]
        public void GetDialogueStepPossibleNextsTest()
        {
            var firstStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model;";
            var combos = getComboDictWithCounts();
            var allBaseItems = AnalyzerHelpers.GetAllBaseItemsForCombos(combos);

            var result = AnalyzerHelpers.GetDialogueStepPossibleNexts(firstStepMarker, allBaseItems);
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("DD15947E69F75CB0C70F025FF06A5056", result[0]);
            Assert.Equal("0ACBFA7CEB246A0CC4489896CA2418AB", result[1]);
        }

        [Fact]
        public void GetDialogueStepPossiblePreviousTest()
        {
            var thirdStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialproduktu&&&&@materiál:;@produkt:produkt; marker_velikost_velka&&&&@velký_produkt:velký;";

            var secondStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_velikost_velka&&&&@velký_produkt:velký;";
            var secondStepMarker1 = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_materialkomponenty&&&&@komponenta:komponenta;@materiál:;";
            var secondStepMarker2 = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model; marker_dokumentaceVykres&&#my_máme;&&@podklady:výkres;";

            var ch = new CryptographyHelpers();
            var firstHash = ch.GetHash(secondStepMarker, true); // 0ACBFA7CEB246A0CC4489896CA2418AB
            var secondHash = ch.GetHash(secondStepMarker1, true); // A5EC396CCB656D59FDF051D84B2EC68F
            var thirdHash = ch.GetHash(secondStepMarker2, true); // 469ECFBF2146ACBBD580057315169E9D

            var combos = getComboDictWithCounts();
            combos.Add(new KeyValuePair<string, string>("0ACBFA7CEB246A0CC4489896CA2418AB", secondStepMarker), 2);
            combos.Add(new KeyValuePair<string, string>("A5EC396CCB656D59FDF051D84B2EC68F", secondStepMarker1), 2);
            combos.Add(new KeyValuePair<string, string>("469ECFBF2146ACBBD580057315169E9D", secondStepMarker2), 2);
            var allBaseItems = AnalyzerHelpers.GetAllBaseItemsForCombos(combos);
            var combosAsListsByLevel = AnalyzerHelpers.GetAllCombosItemsListsBasedLevel(combos, combos.Count);

            var previous = AnalyzerHelpers.GetDialogueStepPossiblePrevious(allBaseItems, thirdStepMarker, combosAsListsByLevel[1]);
            Assert.NotNull(previous);
            Assert.Equal(3, previous.Count);
            Assert.Equal("DD15947E69F75CB0C70F025FF06A5056", previous[0]);
            Assert.Equal("A5EC396CCB656D59FDF051D84B2EC68F", previous[1]);
            Assert.Equal("469ECFBF2146ACBBD580057315169E9D", previous[2]);
        }

        [Fact]
        public void GetDialogueStepFromComboTest()
        {
            var firstStepMarker = "&Markers: marker_dokumentace3dmodel&&#my_máme;&&@podklady:3d model;";
            var combos = getComboDictWithCounts();
            var allBaseItems = AnalyzerHelpers.GetAllBaseItemsForCombos(combos);
            
            var item = combos.First();
            Assert.NotNull(item);
            var result = AnalyzerHelpers.GetDialogueStepFromCombo(item, combos, allBaseItems, "test");

            Assert.NotNull(result);
            Assert.Equal(1, result.Level);
            Assert.Equal(2, result.PossibleNextSteps.Count);
            Assert.Equal("DD15947E69F75CB0C70F025FF06A5056", result.PossibleNextSteps[0]);
            Assert.Equal("0ACBFA7CEB246A0CC4489896CA2418AB", result.PossibleNextSteps[1]);
            Assert.Equal(firstStepMarker, result.Marker);
            Assert.Single(result.Intents);
            Assert.Equal("my_máme", result.Intents[0]);
            Assert.Equal("podklady", result.Entities[0].Key);
            Assert.Equal("3d model", result.Entities[0].Value);
        }
    }
}
