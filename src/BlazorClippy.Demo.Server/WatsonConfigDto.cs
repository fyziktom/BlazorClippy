namespace BlazorClippy.Demo.Server
{
    public class WatsonConfigDto
    {
        public string ApiKey { get; set; } = "";
        public string InstanceId { get; set; } = "";
        public string ApiUrlBase { get; set; } = "";
        public string AssistantId { get; set; } = "";
        public int MaxOnHoldSessionTimeInMinutes { get; set; } = 60;
        public string SpeechToTextApiKey { get; set; } = "";
        public string SpeechToTextUrl { get; set; } = "";
        public string SpeechToTextVoice { get; set; } = "";
        public string TranslatorApiKey { get; set; } = "";
        public string TranslatorUrl { get; set; } = "";
    }
}
