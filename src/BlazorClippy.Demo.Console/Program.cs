using BlazorClippyWatson.AI;
using BlazorClippyWatson.Analzyer;
using BlazorClippyWatson.Common;
using IBM.Watson.Assistant.v2.Model;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Reflection;

Console.WriteLine("Hello, World! I am Watson Replay Console demo");

var sessionId = "cf628279-5bb3-4716-8fe2-71bc5c3fe3f1";
var filename = "messageExport.json";

var dataitemsFileName = "dataitems.mmd";
var loadDataItemsFromFile = true;
var addDataItemsManually = !loadDataItemsFromFile;

var calcCombos = false;
var loadCombosFromFile = !calcCombos;
var combosFileName = "combos.txt";
var saveCombos = calcCombos;
var saveHistory = false;
var printCombos = false;
var playDialogueFromFile = false;
var printDialogue = false;

// create assistant
var assistant = new WatsonAssistant();
var analyzer = new WatsonAssistantAnalyzer();
var hashHistory = new Dictionary<string, string>();
var combinations = new Dictionary<string, string>();

assistant.SessionId = sessionId;




// input answer from watson
var answer = "Klíčové nyní bude definování ještě několika informací o vašem produktu. "+ 
    "<< ? =@velikost:mala; \"Protože se jedná oprodukt malý, tak se dají celkem běžně sehnat rentgeny na takovou kontrolu i za dobré peníze.\"" + 
    " ? =@velikost:velka&?@podklady:3d model; \"Tady bude potřeba udělat hlubší analýzu ideálně včetně 3D modelu. Máte k dispozizici 3D model?\"" + 
    " ? =@velikost:velka&?@podklady:3d model; \"Musíme se určitě podívat blížeji na 3D model a díky simulaci pak můžeme říci jak velký rentgen bude potřeba. Hodně totiž zálezí na pozici místa, které je potřaba zaměřit v detailu a jak v tu chvíli bude muset být natočený předmět.\"" + 
    " ? =@velikost:velka&!@podklady:3d model; \"Pokud nemáte 3d model, tak bude potřeba alespoň nafotit předmět s měřítkem nebo ideálně poslat vzorky k testům.\">>"+
    "<< N ?@svár; \"28778eb3b393497e58fab1389e59811a390d10abe61b86ce82f7ddde3f06a844:0\">>";

Console.WriteLine($"Example Answer from Watson with conditions:");
Console.WriteLine($"{answer}");

Console.WriteLine("Parsing answer...");
var rules = AnswerRulesHelpers.ParseRules(answer);
Console.WriteLine("Ruels parsed:");
foreach (var rule in rules)
{
    Console.WriteLine("-------Rule-------");
    Console.WriteLine($" Rule type: {rule.Value.Type}");
    Console.WriteLine($" Rule: {rule.Value.ParsedRuleFromAnswer}");   

    if (rule.Value.Type == AnswerRuleType.Condition)
    {
        foreach(var r in rule.Value.Rules)
        {
            Console.WriteLine($"\tCondition SubRule Object Name: {r.Object.Name}");
            Console.WriteLine($"\tCondition SubRule Object Command: {r.Object.ObjectCommand}");
            Console.WriteLine($"\tCondition SubRule String: {r.RuleString}");
            if (r.Object.ChildObject != null)
            {
                Console.WriteLine($"\t\tCondition SubRule Object Child Object Name: {r.Object.ChildObject.Name}");
                Console.WriteLine($"\t\tCondition SubRule Object Child Object Command: {r.Object.ChildObject.ObjectCommand}");
                Console.WriteLine($"\t\tCondition SubRule Child Object Connector: {r.Object.ChildObject.Connector}");
            }
        }
    }
    if (rule.Value.Type == AnswerRuleType.NFT)
    {
        var r = rule.Value.Rules.FirstOrDefault();
        if (r != null)
        {
            var split = r.RuleString.Split(':');
            if (split != null && split.Length > 0)
            {
                if (Int32.TryParse(split[1], out var index))
                {
                    var nft = await VEDriversLite.NFT.NFTFactory.GetNFT("", split[0], index, 0, true);
                    Console.WriteLine($"\tNFT Name: {nft.Name}");
                    Console.WriteLine($"\tNFT Type: {nft.TypeText}");
                    Console.WriteLine($"\tNFT Tags: {nft.Tags}");
                    Console.WriteLine($"\tNFT Description: {nft.Description}");
                    Console.WriteLine($"\tNFT Copy share link: https://test.basedataplace.com/gallery?utxo={nft.Utxo}:{nft.UtxoIndex}");
                }
            }
        }
    }
}


///////////////////////////////////
    /// import data from file
    ///////////////////////////////////
if (playDialogueFromFile)
{
    // load the file with message history export
    var file = File.ReadAllText(filename);
    // import history and set session Id
    assistant.ImportMessageHistory(file);

    // check the intents and entities of messages
    var intents = assistant.GetMessagesIntents().ToList();
    var entities = assistant.GetMessagesEntities().ToList();
}
////////////////////////////////////

/////////////////////////////////
// define data items which should be explored
/////////////////////////////////
#region dataitemsdefinition

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

if (loadDataItemsFromFile)
{
    if (File.Exists(dataitemsFileName))
    {
        var filecontent = File.ReadAllText(dataitemsFileName);
        if (!string.IsNullOrEmpty(filecontent))
            dataitemsMermaid = filecontent;
    }
}
var dataitems = new List<AnalyzedObjectDataItem>();

var printDataItems = true;
if (printDataItems)
{
    foreach (var dataitem in AnalyzerHelpers.GetAnalyzedDataItemFromMermaid(dataitemsMermaid))
    {
        dataitems.Add(dataitem);

        Console.WriteLine("DataItem: " + dataitem.NameWithoutUnsuportedChars);
        foreach (var intent in dataitem.Intents)
            Console.WriteLine($"DataItem intent: #{intent.Intent}");
        foreach (var entity in dataitem.Entities)
            Console.WriteLine($"DataItem entity: @{entity.Entity}{entity.Value}");

        var dimermaid = AnalyzerHelpers.GetMermaidFromDataItem(dataitem);
        Console.WriteLine("DataItem reconstructed mermaid: ");
        Console.WriteLine(dimermaid);
    }
}

#region manualaddofDataItems
// create data items which should be captured
if (addDataItemsManually)
{ 
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
}
# endregion

// add dataitems to analyzer
foreach (var di in dataitems)
    analyzer.AddDataItem(di);

#endregion
////////////////////////////


//////////////////////////////
// load markers combinations from file, faster than calc them all the time
/////////////////////////////
if (loadCombosFromFile)
{
    if (!analyzer.ImportDataItemCombinations("", combosFileName))
    {
        calcCombos = true;
        saveCombos = true;
    }
}
/////////////////////////////

////////////////////////
// calculation of combos
////////////////////////
if (calcCombos)
{
    Console.WriteLine("Calculating all combinations of parameters. This can take a while...");
    var spw = new Stopwatch();
    spw.Start();
    combinations = analyzer.GetHashesOfAllCombinations();
    spw.Stop();
    Console.WriteLine($"Elapsed: {spw.ElapsedMilliseconds / 1000} seconds to get all combos.");
    Console.WriteLine($"Total found: {analyzer.DataItemsCombinations.Count} combos for {analyzer.DataItems.Count} DataItems");
    if (printCombos)
    {
        foreach (var combo in combinations)
        {
            Console.WriteLine($"Hash: {combo.Key}, Combo: {combo.Value}");
        }
    }
    if (saveCombos)
    {
        var dilines = new List<string>();
        dilines.Add($"Hash\tCombo");
        foreach (var hashh in analyzer.DataItemsCombinations)
            dilines.Add($"{hashh.Key}\t{hashh.Value}");
        var difilename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), combosFileName);
        if (File.Exists(difilename))
        {
            File.Delete(difilename);
        }
        File.AppendAllLines(difilename, dilines);
    }
}
///////////////////////

////////////////////////////////////
/// simulation of messages
////////////////////////////////////
#region simulationOfMessages

Console.WriteLine("-------------------------------------------------------");
Console.WriteLine("Simulation of message to get hash of stage of dialogue:");
Console.WriteLine("-------------------------------------------------------");

// load message from Marker
var marker = "&Markers: marker_dokumentace3dModel&&#my_máme;#kontrola_pájení;&&@podklady:3d model; marker_kategorieelektroniky&&&&@elektronika:;@podklady:3d model; marker_pajeni&&#kontrola_pájení;&& ";
var msgFromMarker = AnalyzerHelpers.GetMessageFromMarker(marker, assistant.SessionId);
var printMsgsFromMarker = false;

if (printMsgsFromMarker)
{ 
    foreach (var message in AnalyzerHelpers.GetMessagesStepsFromMarker(marker, assistant.SessionId))
    {
        Console.WriteLine("Message: ");
        foreach (var intent in message.Response.Result.Output.Intents)
            Console.WriteLine($"\tMessage intent: #{intent.Intent}");
        foreach (var entity in message.Response.Result.Output.Entities)
            Console.WriteLine($"\tMessage entity: @{entity.Entity}{entity.Value}");
    }
}

// load data from mermaid
var inputDialogueMermaid = "sequenceDiagram\r\n" +
                           "Client->>Analyzer: i.my_máme, e.podklady:3d model,\r\n" +
                           "Client->>Analyzer: i.my_máme, e.podklady:výkres,";
var inputDialogueMermaid1 = "sequenceDiagram\r\n" + "" +
                            "\tClient->>Assistant: i.my_máme, e.podklady:3d model, \r\n" +
                            "\tClient->>Assistant: e.materiál:, e.produkt:produkt,\r\n" +
                            "\tClient->>Assistant: e.materiál:, e.komponenta:komponenta,  \r\n" +
                            "\tClient->>Assistant: i.my_máme, e.podklady:výkres, ";

var dialogue = AnalyzerHelpers.GetDialogueFromMermaid(inputDialogueMermaid, assistant.SessionId); 
var dialogue1 = AnalyzerHelpers.GetDialogueFromMermaid(inputDialogueMermaid1, assistant.SessionId);

// example export of Mermaid from dialogue
var mermaidOutput = AnalyzerHelpers.GetMermaidFromDialogue(dialogue1);
Console.WriteLine("Dialogue from mermaid loaded:");
Console.WriteLine("");
Console.WriteLine(inputDialogueMermaid1);
Console.WriteLine("");

// play dialogue
foreach (var step in analyzer.PlayDialogue(dialogue1, false))
    Console.WriteLine($"Actual hash: {step.Key}.");

//analyzer.ClearAllFoundInAllDataItems();
Console.WriteLine("----------------------------------------------------------");
Console.WriteLine("----------------------------------------------------------");
Console.WriteLine("----------------------------------------------------------");
#endregion
////////////////////////////////////////////////////


///////////////////////////////////////////////////
/// play dialogue from file
///////////////////////////////////////////////////
if (playDialogueFromFile)
{
    // simulating the dialogue
    foreach (var message in assistant.MessageRecordHandler.GetMessageHistory(assistant.SessionId))
    {
        if (printDialogue)
        {
            Console.WriteLine("-------------Message------------------");
            Console.WriteLine($"--------------{message.Timestamp.ToString("MM.dd.yyyy hh:mm:ss")}----------------");
        }
        // add markers to question before "send" to Watson to do not repeat answer for already mentioned/captured dataitem
        var questionextension = analyzer.MarkerExtension;
        // request for MarkExtensionHash will refresh value and save to history.
        var actualMarkerExtensionHash = analyzer.MarkerExtensionHash;

        hashHistory.TryAdd(actualMarkerExtensionHash, questionextension);
        if (printDialogue)
        {
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
        }

        // try to match the results of the items in the message. we should do that after message go through watson.
        // result of match will be considered in next question. Before next send to Watson it will add the markers
        var matchresult = analyzer.MatchDataItems(message);
        // get new list of identified items
        var dis = analyzer.GetIdentifiedDataItemsDetailedMarkers();

        if (printDialogue)
        {
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
    }
}
/////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////
/// save history to file
/////////////////////////////////////////////////////////
if (saveHistory)
{
    var lines = new List<string>();

    lines.Add($"Hash\tCombo");
    foreach (var hashh in hashHistory)
        lines.Add($"{hashh.Key}\t{hashh.Value}");
    filename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "history.txt");
    if (File.Exists(filename))
    {
        File.Delete(filename);
    }
    File.AppendAllLines(filename, lines);
}
/////////////////////////////////////////////////////////


Console.WriteLine("-----------------------------------");
Console.WriteLine("History of dialogue:");
Console.WriteLine("-----------------------------------");
var lastMarkerDetailed = AnalyzerHelpers.MarkerExtensionStartDefault;
foreach(var history in analyzer.GetHistoryOfDialogue())
{
    Console.WriteLine("....................");
    Console.WriteLine($"DateTime: {history.Key.ToString("MM.dd.yyyy hh:mm:ss")}");
    Console.WriteLine($"Hash: {history.Value.Item1}");
    Console.WriteLine($"Marker: {history.Value.Item2}");
    var mk = AnalyzerHelpers.RemoveLastMarksFromMarker(lastMarkerDetailed, history.Value.Item2);

    lastMarkerDetailed = history.Value.Item2;

    var msgReconstructedFromHistoryMarker = AnalyzerHelpers.GetMessageFromMarker(mk, assistant.SessionId);
    Console.WriteLine("\tMessage Details: ");
    foreach (var intent in msgReconstructedFromHistoryMarker.Response.Result.Output.Intents)
        Console.WriteLine($"\t\tMessage intent: #{intent.Intent}");
    foreach (var entity in msgReconstructedFromHistoryMarker.Response.Result.Output.Entities)
        Console.WriteLine($"\t\tMessage entity: @{entity.Entity}:{entity.Value}");
}

Console.WriteLine("-----------------------------------");
Console.WriteLine("----------------END----------------");
Console.WriteLine("-----------------------------------");
Console.WriteLine("Press enter to exit...");
Console.ReadLine();

