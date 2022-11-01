using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazorClippy.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

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

        public string WatsonApiUrl { get; set; } = "https://blazorclippydemoserver20221101171158.azurewebsites.net/api";
        public string SessionId { get; set; } = string.Empty;

        public ClippyService(IJSRuntime js, IServiceProvider serviceProvider)
        {
            this.js = js;
            this.httpClient = serviceProvider.GetRequiredService<HttpClient>();
        }

        public async Task<(bool, string)> StartWatsonSession(string apiurlbase)
        {
            watsonService = new WatsonService(httpClient);
            WatsonApiUrl = apiurlbase;

            if (watsonService == null)
                return (false, "Load Watson first please.");

            var res = await watsonService.StartWatsonSession(WatsonApiUrl);
            if (res.Item1)
                if (Guid.TryParse(res.Item2, out var si))
                    SessionId = si.ToString();

            return res;
        }

        public async Task<(bool,string)> AskWatson(string question, string sessionId = "", bool speak = false, string apiurlbase = "")
        {

            if (string.IsNullOrEmpty(WatsonApiUrl) && string.IsNullOrEmpty(apiurlbase))
                return (false, "Fill the api url base please.");
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
                var res = await watsonService.AskWatson(question, SessionId);
                if (res.Item1)
                {
                    if (speak)
                        await Speak(res.Item2);

                    return res;
                }
            }
            return (false, string.Empty);
        }

        public async Task Load()
        {
            if (!loaded)
                await js.InvokeVoidAsync("blazorClippy.load");

            loaded = true;
        }
        public async Task AnimateRandom()
        {
            await js.InvokeVoidAsync("blazorClippy.animateRandom");
        }
        public async Task PlayAnimation(ClippyAnimations animation)
        {
            await js.InvokeVoidAsync("blazorClippy.play", Enum.GetName(typeof(ClippyAnimations), animation));
        }
        public async Task Speak(string text)
        {
            await js.InvokeVoidAsync("blazorClippy.speak", text);
        }
        public async Task GestureAt(int x, int y )
        {
            await js.InvokeVoidAsync("blazorClippy.gestureAt", new object[] { x, y });
        }
        public async Task StopCurrent()
        {
            await js.InvokeVoidAsync("blazorClippy.stopCurrent");
        }
        public async Task Stop()
        {
            await js.InvokeVoidAsync("blazorClippy.stop");
        }
        public async Task<List<string>> GetAnimationsList()
        {
            var list = await js.InvokeAsync<List<string>>("blazorClippy.getAnimationsList");
            if (list != null)
                list.ForEach(i => Console.WriteLine(i));
            return list ?? new List<string>();
        }
    }
}
