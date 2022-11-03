
using BlazorClippyWatson.AI;
using System.Collections.Concurrent;

namespace BlazorClippy.Demo.Server
{
    public static class MainDataContext
    {
        public static WatsonConfigDto WatsonConfig { get; set; } = new WatsonConfigDto();
        public static ConcurrentDictionary<string, WatsonAssistant> Assistants { get; set; } = new ConcurrentDictionary<string, WatsonAssistant>();
        public static WatsonTextToSpeech? TextToSpeech { get; set; }
        public static WatsonTranslator? Translator { get; set; }
    }
}
