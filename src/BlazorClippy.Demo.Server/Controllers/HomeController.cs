using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BlazorClippy.Demo.Server.Controlers
{
    public class AllowCrossSiteJsonAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // We'd normally just use "*" for the allow-origin header, 
            // but Chrome (and perhaps others) won't allow you to use authentication if
            // the header is set to "*".
            // TODO: Check elsewhere to see if the origin is actually on the list of trusted domains.
            var ctx = filterContext.HttpContext;
            //var origin = ctx.Request.Headers["Origin"];
            //var allowOrigin = !string.IsNullOrWhiteSpace(origin) ? origin : "*";
            ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            ctx.Response.Headers.Add("Access-Control-Allow-Headers", "*");
            ctx.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            base.OnActionExecuting(filterContext);
        }
    }

    [Route("api")]
    [ApiController]
    public class HomeController : Controller
    {
        
        [HttpGet("StartWatsonSession")]
        [AllowCrossSiteJsonAttribute]
        public async Task<string> StartWatsonSession()
        {
            var wa = new WatsonAssistant();
            var resinit = await wa.InitAssistantService(MainDataContext.WatsonConfig.ApiKey, 
                                                        MainDataContext.WatsonConfig.ApiUrlBase, 
                                                        MainDataContext.WatsonConfig.InstanceId);
            if (resinit.Item1)
            {
                var res = await wa.CreateSession(MainDataContext.WatsonConfig.AssistantId);
                if (res.Item1)
                {
                    MainDataContext.Assistants.TryAdd(res.Item2, wa);
                    if (res.Item1)
                        return res.Item2;
                }
            }

            cleanOldSessions();

            return string.Empty;
        }

        private void cleanOldSessions()
        {
            var list = new List<string>();

            foreach (var assistent in MainDataContext.Assistants)
                if ((DateTime.UtcNow - assistent.Value.LastQuestionAsked).TotalMinutes > MainDataContext.WatsonConfig.MaxOnHoldSessionTimeInMinutes)
                    list.Add(assistent.Key);

            foreach (var id in list)
                MainDataContext.Assistants.TryRemove(id, out var rwa);
        }

        public class Question
        {
            public string sessionId { get; set; } = string.Empty;
            public string text { get; set; } = string.Empty;
        }

        [HttpPost("AskWatson")]
        [AllowCrossSiteJsonAttribute]
        public async Task<string> AskQuestion([FromBody] Question data)
        {
            if (string.IsNullOrEmpty(data.text))
                return "Please fill some question in property text.";

            if (!MainDataContext.Assistants.ContainsKey(data.sessionId))
                data.sessionId = await StartWatsonSession();

            if (MainDataContext.Assistants.TryGetValue(data.sessionId, out var wa))
            {
                var res = await wa.SendMessage(data.text, MainDataContext.WatsonConfig.AssistantId, data.sessionId);
                if (res.Item1)
                    return res.Item2;                    
            }
            return string.Empty;
        }
    }
}
