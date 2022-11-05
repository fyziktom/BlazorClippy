namespace BlazorClippyWatson.AI
{
    public class WatsonConfigDto
    {
        /// <summary>
        /// Api Key for Watson Assistant cloud service
        /// </summary>
        public string ApiKey { get; set; } = "";
        /// <summary>
        /// Instance Id for Watson Assistant cloud service
        /// </summary>
        public string InstanceId { get; set; } = "";
        /// <summary>
        /// Api Url for Watson Assistant cloud service
        /// </summary>
        public string ApiUrlBase { get; set; } = "";
        /// <summary>
        /// Assistant Id for Watson Assistant cloud service
        /// </summary>
        public string AssistantId { get; set; } = "";
        /// <summary>
        /// Keep session in cache for max number of minutes. Each question will refresh the counter
        /// </summary>
        public int MaxOnHoldSessionTimeInMinutes { get; set; } = 60;
        /// <summary>
        /// Api Key for Watson Text to Speech cloud service
        /// </summary>
        public string TextToSpeechApiKey { get; set; } = "";
        /// <summary>
        /// Api Url for Watson Text to Speech cloud service
        /// </summary>
        public string TextToSpeechUrl { get; set; } = "";
        /// <summary>
        /// Watson Text to Speech cloud service selected voice
        /// </summary>
        public string TextToSpeechVoice { get; set; } = "";
        /// <summary>
        /// Api Key for Watson Translator cloud service
        /// </summary>
        public string TranslatorApiKey { get; set; } = "";
        /// <summary>
        /// Api Url for Watson Translator cloud service
        /// </summary>
        public string TranslatorUrl { get; set; } = "";
    }
}
