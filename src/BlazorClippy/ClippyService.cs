using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public ClippyService(IJSRuntime js)
        {
            this.js = js;
        }
        
        public async Task Load()
        {
            await js.InvokeVoidAsync("blazorClippy.load");
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
