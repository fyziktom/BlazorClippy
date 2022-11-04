using BlazorClippyWatson.AI;
using BlazorClippyWatson.Analzyer;
using BlazorClippyWatson.Common;
using IBM.Watson.Assistant.v2.Model;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;

Console.WriteLine("Hello, World! I am Watson Replay Console demo");

// load the file with message history export
var file = File.ReadAllText("messageExport.json");

// create assistant
var assistant = new WatsonAssistant();

// import history and set session Id
assistant.ImportMessageHistory(file);
assistant.SessionId = "cf628279-5bb3-4716-8fe2-71bc5c3fe3f1";

// check the intents and entities of messages
var intents = assistant.GetMessagesIntents().ToList();
var entities = assistant.GetMessagesEntities().ToList();

// create analyzer
var analyzer = new WatsonAssistantAnalyzer();

/////////////////////////////////
// define data items which should be explored
#region dataitemsdefinition
// create data items which should be captured
var dataitems = new List<AnalyzedObjectDataItem>();
dataitems.Add(new AnalyzedObjectDataItem()
{
    Name = "dokumentace3dModel",
    IsWhenAllOnly = true,
    Entities = new List<RuntimeEntity>()
    {
        new RuntimeEntity(){ Entity = "podklady", Value = "3d model" }
    },
    Intents = new List<RuntimeIntent>()
    {
        new RuntimeIntent(){ Intent = "my_máme" }
    }
});
dataitems.Add(new AnalyzedObjectDataItem()
{
    Name = "dokumentaceVykres",
    IsWhenAllOnly = true,
    Entities = new List<RuntimeEntity>()
    {
        new RuntimeEntity(){ Entity = "podklady", Value = "výkres" },
    },
    Intents = new List<RuntimeIntent>()
    {
        new RuntimeIntent(){ Intent = "my_máme" }
    }
});
dataitems.Add(new AnalyzedObjectDataItem()
{
    Name = "material produktu",
    IsWhenAllOnly = true,
    Entities = new List<RuntimeEntity>()
    {
        new RuntimeEntity(){ Entity = "materiál", Value = "" },
        new RuntimeEntity(){ Entity = "produkt", Value = "produkt" },
    }
});
dataitems.Add(new AnalyzedObjectDataItem()
{
    Name = "material komponenty",
    IsWhenAllOnly = true,
    Entities = new List<RuntimeEntity>()
    {
        new RuntimeEntity(){ Entity = "materiál", Value = "" },
        new RuntimeEntity(){ Entity = "komponenta", Value = "komponenta" },
    }
});
dataitems.Add(new AnalyzedObjectDataItem()
{
    Name = "maji server",
    IsWhenAllOnly = true,
    Entities = new List<RuntimeEntity>()
    {
        new RuntimeEntity(){ Entity = "itinfrastruktura", Value = "server" },
    },
    Intents = new List<RuntimeIntent>()
    {
        new RuntimeIntent(){ Intent = "my_máme" }
    }
});
dataitems.Add(new AnalyzedObjectDataItem()
{
    Name = "Velikost produktu",
    IsWhenAllOnly = true,
    Entities = new List<RuntimeEntity>()
    {
        new RuntimeEntity(){ Entity = "produkt", Value = "produkt" }
    },
    Intents = new List<RuntimeIntent>()
    {
        new RuntimeIntent(){ Intent = "velikost_produktu" }
    }
});
dataitems.Add(new AnalyzedObjectDataItem()
{
    Name = "kategorie elektroniky",
    Entities = new List<RuntimeEntity>()
    {
        new RuntimeEntity(){ Entity = "elektronika", Value = "" }
    }
});
dataitems.Add(new AnalyzedObjectDataItem()
{
    Name = "pajeni",
    Intents = new List<RuntimeIntent>()
    {
        new RuntimeIntent(){ Intent = "kontrola_pájení" }
    }
});



// add dataitems to analyzer
foreach (var di in dataitems)
    analyzer.AddDataItem(di);

#endregion
////////////////////////////

Console.WriteLine("Calculating all combinations of parameters. This can take a while...");
var spw = new Stopwatch();
spw.Start();
var combinations = analyzer.GetHashesOfAllCombinations();
spw.Stop();
Console.WriteLine($"Elapsed: {spw.ElapsedMilliseconds/1000} seconds to get all combos.");
Console.WriteLine($"Total found: {analyzer.DataItemsCombinations.Count} combos for {analyzer.DataItems.Count} DataItems");
/*
foreach(var combo in combinations)
{
    Console.WriteLine($"Hash: {combo.Key}, Combo: {combo.Value}");
}*/


/*
Console.WriteLine("Press enter to exit...");
Console.ReadLine();
return;
*/
var hashHistory = new Dictionary<string, string>();

foreach (var message in assistant.MessageRecordHandler.GetMessageHistory(assistant.SessionId))
{
    Console.WriteLine("-------------Message------------------");
    Console.WriteLine($"--------------{message.Timestamp.ToString("MM.dd.yyyy hh:mm:ss")}----------------");

    // add markers to question before "send" to Watson to do not repeat answer for already mentioned/captured dataitem
    var questionextension = analyzer.MarkerExtension;
    // request for MarkExtensionHash will refresh value and save to history.
    var actualMarkerExtensionHash = analyzer.MarkerExtensionHash;

    hashHistory.TryAdd(actualMarkerExtensionHash, questionextension);
    // when there is some dataitems already matched we should send info to Watson by sending changed question (with added markers).
    if (analyzer.IsSomeMatch)
    {
        Console.WriteLine($"Question Extension: {questionextension}");
        Console.WriteLine($"Question Extension Hash: {actualMarkerExtensionHash}");
        Console.WriteLine($"Question: {message.Question} {actualMarkerExtensionHash}");       
    }
    else
        Console.WriteLine($"Question: {message.Question}");

    Console.WriteLine("=====>");
    Console.WriteLine($"Answer: {message.TextResponse}");
    Console.WriteLine("");

    // try to match the results of the items in the message. we should do that after message go through watson.
    // result of match will be considered in next question. Before next send to Watson it will add the markers
    var matchresult = analyzer.MatchDataItems(message);
    // get new list of identified items
    var dis = analyzer.GetIdentifiedDataItemsDetailedMarkers();

    if (analyzer.DataItemsCombinations.ContainsKey(actualMarkerExtensionHash))
    {
        Console.WriteLine($"Identified Sequence of parameters: {actualMarkerExtensionHash}!\n");
    }

    // just output of what is already captured
    Console.WriteLine("Identified DataItems:\n");

    foreach (var d in dis)
    {
        var item = analyzer.GetDataItemByDetailedMarker(d);
        if (item != null)
        {
            Console.WriteLine($"Item: {item.Name} identified.");
            /*
            Console.WriteLine("Item Intents:");
            foreach (var intent in item.Intents)
                Console.Write($"#{intent.Intent} ");
            Console.WriteLine("\nItem Entities:");
            foreach (var entity in item.Entities)
                Console.Write($"@{entity.Entity}:{entity.Value} ");
            */
        }
    }
}

/*
var lines = new List<string>();

lines.Add($"Hash\tCombo");
foreach (var hashh in hashHistory)
    lines.Add($"{hashh.Key}\t{hashh.Value}");
var filename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "history.txt");
if (File.Exists(filename))
{
    File.Delete(filename);
}
File.AppendAllLines(filename, lines);

var dilines = new List<string>();
dilines.Add($"Hash\tCombo");
foreach (var hashh in analyzer.DataItemsCombinations)
    dilines.Add($"{hashh.Key}\t{hashh.Value}");
var difilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "combos.txt");
if (File.Exists(difilename))
{
    File.Delete(difilename);
}
File.AppendAllLines(difilename, dilines);
*/

Console.WriteLine("-----------------------------------");
Console.WriteLine("History of dialogue:");
Console.WriteLine("-----------------------------------");
foreach(var history in analyzer.GetHistoryOfDialogue())
{
    Console.WriteLine("....................");
    Console.WriteLine($"DateTime: {history.Key.ToString("MM.dd.yyyy hh:mm:ss")}, Hash: {history.Value.Item1}, Marker: {history.Value.Item2}");
    Console.WriteLine($"Marker: {history.Value.Item2}");
}


Console.WriteLine("-----------------------------------");
Console.WriteLine("----------------END----------------");
Console.WriteLine("-----------------------------------");
Console.WriteLine("Press enter to exit...");
Console.ReadLine();

