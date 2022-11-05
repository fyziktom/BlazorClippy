
using BlazorClippyWatson.AI;
using System.Collections.Concurrent;

namespace BlazorClippy.Demo.Server
{
    public static class MainDataContext
    {
        /// <summary>
        /// Loaded config for Watson services (config is in appsettings.json
        /// </summary>
        public static WatsonConfigDto WatsonConfig { get; set; } = new WatsonConfigDto();
        /// <summary>
        /// Dictionary of actual instances of assistants
        /// </summary>
        public static ConcurrentDictionary<string, WatsonAssistant> Assistants { get; set; } = new ConcurrentDictionary<string, WatsonAssistant>();
        /// <summary>
        /// Service for text to speech
        /// </summary>
        public static WatsonTextToSpeech? TextToSpeech { get; set; }
        /// <summary>
        /// Service for translations
        /// </summary>
        public static WatsonTranslator? Translator { get; set; }
    }
}
