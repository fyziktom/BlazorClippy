namespace BlazorClippy.Demo.Server
{
    public class WatsonConfigDto
    {
        public string ApiKey { get; set; } = "";
        public string InstanceId { get; set; } = "";
        public string ApiUrlBase { get; set; } = "";
        public string AssistantId { get; set; } = "";
        public int MaxOnHoldSessionTimeInMinutes { get; set; } = 60;

    }
}
