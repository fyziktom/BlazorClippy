using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Watson.LanguageTranslator.v3;
using IBM.Watson.TextToSpeech.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.AI
{
    public class WatsonTranslator
    {
        public WatsonTranslator(string apikey, string serviceurl)
        {
            ApiKey = apikey;
            ServiceUrl = serviceurl;
        }
        /// <summary>
        /// API Key to Watson Translator cloud service
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;
        /// <summary>
        /// Service URL to Watson Translator cloud service
        /// </summary>
        public string ServiceUrl { get; set; } = string.Empty;
        /// <summary>
        /// Translate text with use of Watson cloud service
        /// </summary>
        /// <param name="message"></param>
        /// <param name="translateModel"></param>
        /// <returns></returns>
        public async Task<(bool, string)> Translate(string message, string translateModel)
        {
            if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ServiceUrl) || string.IsNullOrEmpty(translateModel))
            {
                return (false, null);
            }
            IamAuthenticator authenticator = new IamAuthenticator(
                apikey: ApiKey);

            LanguageTranslatorService service = new LanguageTranslatorService("2018-05-01", authenticator);
            service.SetServiceUrl(ServiceUrl);

            var result = service.Translate(
                text: new List<string>() { message },
                modelId: translateModel
                );

            return (true, result.Result.Translations.Where(t => !string.IsNullOrEmpty(t._Translation)).Select(t => t._Translation).FirstOrDefault());
        }

    }
}
