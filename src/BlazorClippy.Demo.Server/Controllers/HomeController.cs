using BlazorClippy.AI;
using IBM.Watson.TextToSpeech.v1.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using static System.Net.Mime.MediaTypeNames;

namespace BlazorClippy.Demo.Server.Controlers
{   /*
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
    */
    /*
    public class AllowCrossSiteAudioAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var ctx = filterContext.HttpContext;
            ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            ctx.Response.Headers.Add("Access-Control-Allow-Headers", "*");
            if (ctx.Response.Headers.ContainsKey("content-type"))
                ctx.Response.Headers["content-type"] = "audio/wav";
            else
                ctx.Response.Headers.Add("content-type", "audio/wav");

            var hds = ctx.Response.Headers.ToList();
            ctx.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
            base.OnActionExecuting(filterContext);
        }
    
    }
    */

    [Route("api")]
    [ApiController]
    public class HomeController : Controller
    {
        
        [HttpGet("StartWatsonSession")]
        //[AllowCrossSiteJsonAttribute]
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
        //[AllowCrossSiteJsonAttribute]
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

        public class TranslateRequest
        {
            public string text { get; set; } = string.Empty;
            public string translateModel { get; set; } = "cs-en";
        }

        [HttpPost("Translate")]
        //[AllowCrossSiteJsonAttribute]
        public async Task<string> Translate([FromBody] TranslateRequest data)
        {
            if (string.IsNullOrEmpty(data.text))
                return "Please fill some text in property text.";

                var res = await MainDataContext.Translator.Translate(data.text, data.translateModel);
                if (res.Item1)
                    return res.Item2;
            
            return string.Empty;
        }

        [HttpPost("GetVoices")]
        //[AllowCrossSiteJsonAttribute]
        public async Task<List<string>> GetVoices()
        {
            var res = await MainDataContext.TextToSpeech.GetVoices();
            if (res.Item1)
            {
                return res.Item2;
            }
            return new List<string>();
        }

        public class SyntethizeRequest
        {
            public string text { get; set; } = string.Empty;
            public string voice { get; set; } = string.Empty;
        }

        [HttpPost("Syntethise")]
        [Produces("audio/wav")]
        public async Task<FileStreamResult> Syntethise([FromBody] SyntethizeRequest data)
        {
            if (string.IsNullOrEmpty(data.text))
                data.text = "Musíš vyplnit nějaký text.";

            if (data.voice == "string")
                data.voice = string.Empty;
            if (data.text == "string")
                data.text = string.Empty;

            /*
            var folder = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "tmprecords");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            var filename = Path.Join(folder, $"record-{DateTime.UtcNow.ToString("yyyy-MM-ddThh_mm_ss_ff")}.wav");
            */
            try
            {
                var res = await MainDataContext.TextToSpeech.Synthesize(data.text, data.voice);

                if (res.Item1)
                {
                    if (res.Item2 != null)
                    {
                        try
                        {
                            var memory = new MemoryStream(res.Item2);
                            memory.Position = 0;
                            return File(memory, "audio/wav", $"message.wav", true);
                        }
                        catch (Exception ex)
                        {
                            return null;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot get text.", ex.Message);
                return null;
            }
            return null;
        }
    }
}
