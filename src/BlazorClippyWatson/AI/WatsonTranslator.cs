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
        public string ApiKey { get; set; } = string.Empty;
        public string ServiceUrl { get; set; } = string.Empty;

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
