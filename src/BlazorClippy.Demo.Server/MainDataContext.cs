
using System.Collections.Concurrent;

namespace BlazorClippy.Demo.Server
{
    public static class MainDataContext
    {
        public static WatsonConfigDto WatsonConfig { get; set; } = new WatsonConfigDto();
        public static ConcurrentDictionary<string, WatsonAssistant> Assistants { get; set; } = new ConcurrentDictionary<string, WatsonAssistant>();
    }
}
