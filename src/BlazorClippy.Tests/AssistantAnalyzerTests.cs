using BlazorClippyWatson.Analzyer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippy.Tests
{
    public class AssistantAnalyzerTests
    {
        private AnalyzedObjectDataItem getDataItemIntentsAndEntities()
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

            return item;
        }

        [Fact]
        public void AddDataItemToAssistantTest()
        {
            var assistant = new WatsonAssistantAnalyzer();
            var item = getDataItemIntentsAndEntities();
            assistant.AddDataItem(item);

            Assert.Single(assistant.DataItems);

            var itemBack = assistant.GetDataItem(item.CapturedMarker);
            Assert.NotNull(itemBack);
        }

        [Fact]
        public void RemoveDataItemToAssistantTest()
        {
            var assistant = new WatsonAssistantAnalyzer();
            var item = getDataItemIntentsAndEntities();
            assistant.AddDataItem(item);

            var item1 = getDataItemIntentsAndEntities();
            item1.Name = "test1";
            assistant.AddDataItem(item1);

            Assert.Equal(2, assistant.DataItems.Count);
            
            assistant.RemoveDataItem(item.CapturedMarker);
            Assert.Single(assistant.DataItems);

            var itemBack = assistant.GetDataItem(item1.CapturedMarker);
            Assert.NotNull(itemBack);

            assistant.RemoveDataItem(item1.CapturedMarker);
            Assert.Empty(assistant.DataItems);
        }

        [Fact]
        public void AddDataItemIntentTest()
        {
            var assistant = new WatsonAssistantAnalyzer();
            var item = getDataItemIntentsAndEntities();
            assistant.AddDataItem(item);

            assistant.AddDataItemIntent(item.CapturedMarker, new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "testintent"
            });

            var itemBack = assistant.GetDataItem(item.CapturedMarker);
            Assert.NotNull(itemBack);

            Assert.Equal(2, itemBack.Intents.Count);
            Assert.Equal("testintent", itemBack.Intents[1].Intent);
        }

        [Fact]
        public void RemoveDataItemIntentTest()
        {
            var assistant = new WatsonAssistantAnalyzer();
            var item = getDataItemIntentsAndEntities();
            assistant.AddDataItem(item);

            assistant.AddDataItemIntent(item.CapturedMarker, new IBM.Watson.Assistant.v2.Model.RuntimeIntent()
            {
                Intent = "testintent"
            });

            var itemBack = assistant.GetDataItem(item.CapturedMarker);
            Assert.NotNull(itemBack);

            Assert.Equal(2, itemBack.Intents.Count);
            Assert.Equal("testintent", itemBack.Intents[1].Intent);

            assistant.RemoveDataItemIntent(item.CapturedMarker, "testintent");
            Assert.Single(itemBack.Intents);
            Assert.Equal("intent1", itemBack.Intents[0].Intent);

        }

        [Fact]
        public void AddDataItemEntityTest()
        {
            var assistant = new WatsonAssistantAnalyzer();
            var item = getDataItemIntentsAndEntities();
            assistant.AddDataItem(item);

            assistant.AddDataItemEntity(item.CapturedMarker, new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "testentity",
                Value = "testvalue"
            });

            var itemBack = assistant.GetDataItem(item.CapturedMarker);
            Assert.NotNull(itemBack);

            Assert.Equal(4, itemBack.Entities.Count);
            Assert.Equal("testentity", itemBack.Entities[3].Entity);
            Assert.Equal("testvalue", itemBack.Entities[3].Value);
        }

        [Fact]
        public void RemoveDataItemEntityTest()
        {
            var assistant = new WatsonAssistantAnalyzer();
            var item = getDataItemIntentsAndEntities();
            assistant.AddDataItem(item);

            assistant.AddDataItemEntity(item.CapturedMarker, new IBM.Watson.Assistant.v2.Model.RuntimeEntity()
            {
                Entity = "testentity",
                Value = "testvalue"
            });

            var itemBack = assistant.GetDataItem(item.CapturedMarker);
            Assert.NotNull(itemBack);

            Assert.Equal(4, itemBack.Entities.Count);
            Assert.Equal("testentity", itemBack.Entities[3].Entity);
            Assert.Equal("testvalue", itemBack.Entities[3].Value);

            assistant.RemoveDataItemEntity(item.CapturedMarker, "testentity", "testvalue");
            Assert.Equal(3, itemBack.Entities.Count);
            Assert.Equal("entity3", itemBack.Entities[2].Entity);
            Assert.Equal("evalue3", itemBack.Entities[2].Value);

        }

    }

}
