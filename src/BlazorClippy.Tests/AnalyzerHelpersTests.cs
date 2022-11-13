using BlazorClippyWatson.Analzyer;
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
    }
}
