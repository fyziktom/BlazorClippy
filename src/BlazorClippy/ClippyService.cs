using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorClippy.AI;
using BlazorClippyWatson.AI;
using BlazorClippyWatson.Analzyer;
using IBM.Watson.Assistant.v2.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace BlazorClippy
{
    public enum ClippyAnimations
    {
        Congratulate,
        LookRight,
        SendMail,
        Thinking,
        Explain,
        IdleRopePile,
        IdleAtom,
        Print,
        Hide,
        GetAttention,
        Save,
        GetTechy,
        GestureUp,
        Idle1_1,
        Processing,
        Alert,
        LookUpRight,
        IdleSideToSide,
        GoodBye,
        LookLeft,
        IdleHeadScratch,
        LookUpLeft,
        CheckingSomething,
        Hearing_1,
        GetWizardy,
        IdleFingerTap,
        GestureLeft,
        Wave,
        GestureRight,
        Writing,
        IdleSnooze,
        LookDownRight,
        GetArtsy,
        Show,
        LookDown,
        Searching,
        EmptyTrash,
        Greeting,
        LookUp,
        GestureDown,
        RestPose,
        IdleEyeBrowRaise,
        LookDownLeft,
    }
    public class ClippyService
    {
        private readonly IJSRuntime js;
        private readonly HttpClient httpClient;
         
        bool loaded = false;
        private WatsonService watsonService;
        /// <summary>
        /// Url for Watson service. It is addres of API of BlazorClippy.Demo.Server app
        /// </summary>
        public string WatsonApiUrl { get; set; } = "https://localhost:7008/api";
        /// <summary>
        /// Watson analyzer
        /// </summary>
        public WatsonAssistantAnalyzer Analyzer { get; set; } = new WatsonAssistantAnalyzer();
        /// <summary>
        /// Last captured DataItems Ids
        /// </summary>
        public List<string> LastCapturedDataItems = new List<string>();
        /// <summary>
        /// Id of session of actual dialogue with Watson
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        public ClippyService(IJSRuntime js, IServiceProvider serviceProvider)
        {
            this.js = js;
            this.httpClient = serviceProvider.GetRequiredService<HttpClient>();
        }

        /// <summary>
        /// Start watson session. It will initiate WatsonService instance and provide ApiUrl for BlazorClippy.Demo.Server app
        /// </summary>
        /// <param name="apiurlbase"></param>
        /// <returns></returns>
        public async Task<(bool, string)> StartWatsonSession(string apiurlbase)
        {
            watsonService = new WatsonService(httpClient, apiurlbase);
            WatsonApiUrl = apiurlbase;

            if (watsonService == null)
                return (false, "Load Watson first please.");

            var res = await watsonService.StartWatsonSession(WatsonApiUrl);
            if (res.Item1)
                if (Guid.TryParse(res.Item2, out var si))
                    SessionId = si.ToString();

            return res;
        }
        /// <summary>
        /// Send question to Watson and wait for the response
        /// </summary>
        /// <param name="question"></param>
        /// <param name="sessionId"></param>
        /// <param name="speak"></param>
        /// <param name="apiurlbase"></param>
        /// <returns></returns>
        public async Task<(bool,QuestionResponseDto?)> AskWatson(string question, string sessionId = "", bool speak = false, string apiurlbase = "", WatsonAssistantAnalyzer? analyzer = null)
        {

            if (string.IsNullOrEmpty(WatsonApiUrl) && string.IsNullOrEmpty(apiurlbase))
                return (false, null);
            else if (string.IsNullOrEmpty(WatsonApiUrl) && !string.IsNullOrEmpty(apiurlbase))
                WatsonApiUrl = apiurlbase;

            if (!string.IsNullOrEmpty(sessionId))
            {
                if (Guid.TryParse(sessionId, out var si))
                    SessionId = si.ToString();
            }
            else if (string.IsNullOrEmpty(SessionId) && string.IsNullOrEmpty(sessionId))
                await StartWatsonSession(WatsonApiUrl);

            if (watsonService != null && !string.IsNullOrEmpty(watsonService.WatsonApiUrl))
            {
                var res = await watsonService.AskWatson(question, SessionId, analyzer:analyzer);
                if (res.Item1)
                {
                    if (speak)
                        await Speak(res.Item2.answer);

                    return res;
                }
            }
            return (false, null);
        }

        /// <summary>
        /// Get history of messages
        /// </summary>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public IEnumerable<WatsonMessageRequestRecord> GetMessageHistory(string sessionId, bool descending = false)
        {
            return watsonService.GetMessageHistory(sessionId, descending);
        }
        /// <summary>
        /// Find message by Id
        /// </summary>
        /// <param name="recordId"></param>
        /// <returns></returns>
        public WatsonMessageRequestRecord GetMessageById(string recordId)
        {
            return watsonService.GetMessageById(recordId);
        }
        /// <summary>
        /// Get all messages intents
        /// </summary>
        /// <returns></returns>
        public IEnumerable<List<RuntimeIntent>> GetMessagesIntents()
        {
            return watsonService.GetMessagesIntents(SessionId);
        }
        /// <summary>
        /// Get all messages entities:values
        /// </summary>
        /// <returns></returns>
        public IEnumerable<List<RuntimeEntity>> GetMessagesEntities()
        {
            return watsonService.GetMessagesEntities(SessionId);
        }
        /// <summary>
        /// Export message history
        /// </summary>
        /// <returns></returns>
        public string ExportMessageHistory()
        {
            return watsonService.ExportMessageHistory();
        }
        /// <summary>
        /// Import message history
        /// </summary>
        /// <param name="importData"></param>
        public void ImportMessageHistory(string importData)
        {
            watsonService.ImportMessageHistory(importData);
        }
        /// <summary>
        /// Send text to Watson translator
        /// </summary>
        /// <param name="text"></param>
        /// <param name="translateModel"></param>
        /// <returns></returns>
        public async Task<(bool, string)> Translate(string text, string translateModel = "cs-en")
        {
            return await watsonService.Translate(text, translateModel);
        }
        /// <summary>
        /// Send text to Watson text to speech synthetiser
        /// </summary>
        /// <param name="text"></param>
        /// <param name="voice"></param>
        /// <returns></returns>
        public async Task<(bool, string)> Synthetise(string text, string voice = "en-US_LisaV3Voice")
        {
            return await watsonService.Synthetise(text, voice);
        }
        /// <summary>
        /// Download file with conversation history
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="aslist"></param>
        /// <returns></returns>
        public async Task BackupConversation(string sessionId, bool aslist = false)
        {
            var filename = $"Backup-SessionId-{sessionId}_Time_" + DateTime.UtcNow.ToString("dd-MM-yyyyThh_mm_ss") + ".txt";
            var backupData = GetMessageHistory(sessionId).ToList();
            backupData.Reverse();
            if (backupData != null)
            {
                var result = string.Empty;
                if (!aslist)
                {
                    result += $"WebUrl: {WatsonApiUrl}\n";
                    result += $"SessionId: {sessionId}\n";
                    result += "--------------------------------------------------------\n";
                    result += "--------------------------------------------------------\n\n";
                    foreach (var message in backupData)
                    {
                        result += $"DateTime: {message.Timestamp.ToString("MM.dd.yyyy hh:mm:ss")}\n";
                        result += $"Question:\n\t{message.Question}\nAnswer:\n\t{message.TextResponse}\n";
                        result += "--------------------------------------------------------\n";
                    }
                }
                else
                    result = JsonConvert.SerializeObject(filename, Formatting.Indented);

                await js.InvokeVoidAsync("blazorClippy.downloadText", result, filename);
            }
        }

        /// <summary>
        /// Save actual Analyzer DataItems as mermaid
        /// </summary>
        /// <returns></returns>
        public async Task SaveDataItemsAsMermaid()
        {
            var filename = $"Backup-SessionId-{SessionId}-DataItems_Time_" + DateTime.UtcNow.ToString("dd-MM-yyyyThh_mm_ss") + ".txt";

            var result = string.Empty;
            result += "classDiagram\r\n";
            foreach (var item in Analyzer.DataItems)
            {
                var res = AnalyzerHelpers.GetMermaidFromDataItem(item.Value);
                if (!string.IsNullOrEmpty(res))
                    result += res;
            }

            await js.InvokeVoidAsync("blazorClippy.downloadText", result, filename);
        }

        /// <summary>
        /// Load DataItems from Mermaid script to Analyzer 
        /// </summary>
        /// <param name="mermaid">Input mermaid class diagram</param>
        /// <returns></returns>
        public async Task LoadDataItemsFromMermaid(string mermaid)
        {
            Analyzer.DataItems.Clear();

            foreach(var item in AnalyzerHelpers.GetAnalyzedDataItemFromMermaid(mermaid))
                Analyzer.AddDataItem(item);
        }

        /// <summary>
        /// Load Clippy
        /// </summary>
        /// <returns></returns>
        public async Task Load()
        {
            if (!loaded)
                await js.InvokeVoidAsync("blazorClippy.load");

            loaded = true;
        }
        /// <summary>
        /// Play random Clippy animation
        /// </summary>
        /// <returns></returns>
        public async Task AnimateRandom()
        {
            await js.InvokeVoidAsync("blazorClippy.animateRandom");
        }
        /// <summary>
        /// Play specific Clippy animation
        /// </summary>
        /// <param name="animation"></param>
        /// <returns></returns>
        public async Task PlayAnimation(ClippyAnimations animation)
        {
            await js.InvokeVoidAsync("blazorClippy.play", Enum.GetName(typeof(ClippyAnimations), animation));
        }
        /// <summary>
        /// Display some text by Clippy
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task Speak(string text)
        {
            await js.InvokeVoidAsync("blazorClippy.speak", text);
        }
        /// <summary>
        /// Clippy will show to some place
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public async Task GestureAt(int x, int y )
        {
            await js.InvokeVoidAsync("blazorClippy.gestureAt", new object[] { x, y });
        }
        /// <summary>
        /// Stop current animation
        /// </summary>
        /// <returns></returns>
        public async Task StopCurrent()
        {
            await js.InvokeVoidAsync("blazorClippy.stopCurrent");
        }
        /// <summary>
        /// Stop doing anything
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            await js.InvokeVoidAsync("blazorClippy.stop");
        }
        public async Task CopyToClipboard(string text)
        {
            await js.InvokeVoidAsync("blazorClippy.copyToClipboard", text);
        }
        /// <summary>
        /// Get list of animations
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetAnimationsList()
        {
            var list = await js.InvokeAsync<List<string>>("blazorClippy.getAnimationsList");
            if (list != null)
                list.ForEach(i => Console.WriteLine(i));
            return list ?? new List<string>();
        }
    }
}
