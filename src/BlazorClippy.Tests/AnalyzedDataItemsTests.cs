using BlazorClippyWatson.AI;
using BlazorClippyWatson.Analzyer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippy.Tests
{
    public class AnalyzedDataItemsTests
    {
        [Fact]
        public void UnsuportedCharactersRemovalTest()
        {
            var item = new AnalyzedObjectDataItem();

            item.Name = "te;st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
            item.Name = "te st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
            item.Name = "te$st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
            item.Name = "te#st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
            item.Name = "te!st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
            item.Name = "te%st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
            item.Name = "te*st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
            item.Name = "te~st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
            item.Name = "te+st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
            item.Name = "te@st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
            item.Name = "te&st";
            Assert.Equal("test", item.NameWithoutUnsuportedChars);
        }

        [Fact]
        public void CapturedMarkerTest()
        {
            var item = new AnalyzedObjectDataItem();

            item.Name = "test";
            Assert.Equal("marker_test", item.CapturedMarker);
        }

        [Fact]
        public void CapturedMarkerDetailedTest()
        {
            var item = new AnalyzedObjectDataItem();

            item.Name = "test";
            Assert.Equal("marker_test&&&&", item.CapturedMarkerDetailed);
        }

        [Fact]
        public void FoundIntentsTest()
        {
            var item = new AnalyzedObjectDataItem();
            var intent1 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent1"
            };
            item.Intents.Add(intent1);
            var intent2 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent2"
            };
            item.Intents.Add(intent2);
            item.FoundIntents.Add("intent1", intent1);
            item.FoundIntents.Add("intent2", intent2);
            item.Name = "test";
            Assert.Equal("#intent1;#intent2;", item.FoundIntentsTotal);
            Assert.Equal("marker_test&&#intent1;#intent2;&&", item.CapturedMarkerDetailed);
        }

        [Fact]
        public void FoundEntitiesTest()
        {
            var item = new AnalyzedObjectDataItem();
            var entity1 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity1",
                Value = "evalue1"
            };
            item.Entities.Add(entity1);
            var entity2 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity2",
                Value = "evalue2"
            };
            item.Entities.Add(entity2);
            item.FoundEntities.Add("entity1:evalue1", entity1);
            item.FoundEntities.Add("entity2:evalue2", entity2);
            item.Name = "test";
            Assert.Equal("@entity1:evalue1;@entity2:evalue2;", item.FoundEntitiesTotal);
            Assert.Equal("marker_test&&&&@entity1:evalue1;@entity2:evalue2;", item.CapturedMarkerDetailed);
        }

        [Fact]
        public void FoundIntentsAndEntitiesTest()
        {
            var item = new AnalyzedObjectDataItem();
            var intent1 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent1"
            };
            item.Intents.Add(intent1);
            var intent2 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent2"
            };
            item.Intents.Add(intent2);
            item.FoundIntents.Add("intent1", intent1);
            item.FoundIntents.Add("intent2", intent2);
            var entity1 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity1",
                Value = "evalue1"
            };
            item.Entities.Add(entity1);
            var entity2 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity2",
                Value = "evalue2"
            };
            item.Entities.Add(entity2);
            item.FoundEntities.Add("entity1:evalue1", entity1);
            item.FoundEntities.Add("entity2:evalue2", entity2);

            item.Name = "test";
            Assert.Equal("#intent1;#intent2;", item.FoundIntentsTotal);
            Assert.Equal("@entity1:evalue1;@entity2:evalue2;", item.FoundEntitiesTotal);
            Assert.Equal("marker_test&&#intent1;#intent2;&&@entity1:evalue1;@entity2:evalue2;", item.CapturedMarkerDetailed);
        }

        [Fact]
        public void MatchMessageInAnalyzedDataItemJustIntentsTest()
        {
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

            var item = new AnalyzedObjectDataItem();
            var intent1 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent1"
            };
            item.Intents.Add(intent1);
            var intent2 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent2"
            };
            item.Intents.Add(intent2);

            var result = item.IsMessageInterestMatch(message);
            Assert.Equal(2, result.Item1.Count);
            Assert.Empty(result.Item2);
            Assert.Equal("intent1", result.Item1[0].Intent);
            Assert.Equal("intent2", result.Item1[1].Intent);
            Assert.True(item.IsIdentified);

        }

        [Fact]
        public void MatchMessageInAnalyzedDataItemJustEntitiesTest()
        {
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

            var item = new AnalyzedObjectDataItem();
            var entity1 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity1",
                Value = "evalue1"
            };
            item.Entities.Add(entity1);
            var entity2 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity2",
                Value = "evalue2"
            };
            item.Entities.Add(entity2);

            var result = item.IsMessageInterestMatch(message);
            Assert.Equal(2, result.Item2.Count);
            Assert.Empty(result.Item1);
            Assert.Equal("entity1", result.Item2[0].Entity);
            Assert.Equal("evalue1", result.Item2[0].Value);
            Assert.Equal("entity2", result.Item2[1].Entity);
            Assert.Equal("evalue2", result.Item2[1].Value);
            Assert.True(item.IsIdentified);

        }


        [Fact]
        public void MatchMessageInAnalyzedDataItemTest()
        {
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

            var item = new AnalyzedObjectDataItem();
            var intent1 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent1"
            };
            item.Intents.Add(intent1);
            var intent2 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent2"
            };
            item.Intents.Add(intent2);

            var entity1 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity1",
                Value = "evalue1"
            };
            item.Entities.Add(entity1);
            var entity2 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity2",
                Value = "evalue2"
            };
            item.Entities.Add(entity2);

            var result = item.IsMessageInterestMatch(message);
            Assert.Equal(2, result.Item1.Count);
            Assert.Equal(2, result.Item2.Count);
            Assert.Equal("intent1", result.Item1[0].Intent);
            Assert.Equal("intent2", result.Item1[1].Intent);
            Assert.Equal("entity1", result.Item2[0].Entity);
            Assert.Equal("evalue1", result.Item2[0].Value);
            Assert.Equal("entity2", result.Item2[1].Entity);
            Assert.Equal("evalue2", result.Item2[1].Value);
            Assert.True(item.IsIdentified);

        }


        [Fact]
        public void GetCombosOfAnalyzedDataItemJustIntentsTest()
        {
            var item = new AnalyzedObjectDataItem() { Name = "test" };
            var intent1 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent1"
            };
            item.Intents.Add(intent1);
            var intent2 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent2"
            };
            item.Intents.Add(intent2);
            var intent3 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent3"
            };
            item.Intents.Add(intent3);

            var combos = item.GetAllDetailedMarksCombination();

            Assert.Equal(6, combos.Count);
            Assert.Equal("#intent1;", combos[0]);
            Assert.Equal("#intent1;#intent2;", combos[1]);
            Assert.Equal("#intent1;#intent2;#intent3;", combos[2]);
            Assert.Equal("marker_test&&#intent1;&&;", combos[3]);
            Assert.Equal("marker_test&&#intent1;#intent2;&&;", combos[4]);
            Assert.Equal("marker_test&&#intent1;#intent2;#intent3;&&;", combos[5]);

        }

        [Fact]
        public void GetCombosOfAnalyzedDataItemJustEntitiesTest()
        {
            var item = new AnalyzedObjectDataItem() { Name = "test" };
            var entity1 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity1",
                Value = "evalue1"
            };
            item.Entities.Add(entity1);
            var entity2 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity2",
                Value = ""
            };
            item.Entities.Add(entity2);
            var entity3 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity3",
                Value = "evalue3"
            };
            item.Entities.Add(entity3);

            var combos = item.GetAllDetailedMarksCombination();

            Assert.Equal(6, combos.Count);
            Assert.Equal("@entity1:evalue1;", combos[0]);
            Assert.Equal("@entity1:evalue1;@entity2:;", combos[1]);
            Assert.Equal("@entity1:evalue1;@entity2:;@entity3:evalue3;", combos[2]);
            Assert.Equal("marker_test&&&&@entity1:evalue1;", combos[3]);
            Assert.Equal("marker_test&&&&@entity1:evalue1;@entity2:;", combos[4]);
            Assert.Equal("marker_test&&&&@entity1:evalue1;@entity2:;@entity3:evalue3;", combos[5]);

        }

        [Fact]
        public void GetCombosOfAnalyzedDataItemTest()
        {
            var item = new AnalyzedObjectDataItem() { Name = "test" };

            var intent1 = new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "intent1"
            };
            item.Intents.Add(intent1);

            var entity1 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity1",
                Value = "evalue1"
            };
            item.Entities.Add(entity1);
            var entity2 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity2",
                Value = ""
            };
            item.Entities.Add(entity2);
            var entity3 = new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "entity3",
                Value = "evalue3"
            };
            item.Entities.Add(entity3);

            var combos = item.GetAllDetailedMarksCombination();

            Assert.Equal(11, combos.Count);
            Assert.Equal("#intent1;", combos[0]);
            Assert.Equal("@entity1:evalue1;", combos[1]);
            Assert.Equal("marker_test&&#intent1;&&@entity1:evalue1;", combos[2]);
            Assert.Equal("@entity1:evalue1;@entity2:;", combos[3]);
            Assert.Equal("marker_test&&#intent1;&&@entity1:evalue1;@entity2:;", combos[4]);
            Assert.Equal("@entity1:evalue1;@entity2:;@entity3:evalue3;", combos[5]);
            Assert.Equal("marker_test&&#intent1;&&@entity1:evalue1;@entity2:;@entity3:evalue3;", combos[6]);
            Assert.Equal("marker_test&&&&@entity1:evalue1;", combos[7]);
            Assert.Equal("marker_test&&&&@entity1:evalue1;@entity2:;", combos[8]);
            Assert.Equal("marker_test&&&&@entity1:evalue1;@entity2:;@entity3:evalue3;", combos[9]);
            Assert.Equal("marker_test&&#intent1;&&;", combos[10]);

        }
    }
}
