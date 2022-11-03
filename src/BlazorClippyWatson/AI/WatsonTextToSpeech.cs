using IBM.Cloud.SDK.Core.Authentication.Iam;
using IBM.Watson.TextToSpeech.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorClippyWatson.AI
{
    public class WatsonTextToSpeech
    {
        public WatsonTextToSpeech(string apikey, string serviceurl, string voice)
        {
            ApiKey = apikey;
            ServiceUrl = serviceurl;
            Voice = voice;
        }
        public string ApiKey { get; set; } = string.Empty;
        public string ServiceUrl { get; set; } = string.Empty;
        public string Voice { get; set; } = string.Empty;

        private byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
        public async Task<(bool, byte[]?)> Synthesize(string text, string voice = "", string savetofile = "")
        {
            if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ServiceUrl) || (string.IsNullOrEmpty(Voice) && string.IsNullOrEmpty(voice)))
                return (false, null);

            IamAuthenticator authenticator = new IamAuthenticator(
                apikey: ApiKey);

            TextToSpeechService service = new TextToSpeechService(authenticator);
            service.SetServiceUrl(ServiceUrl);

            var vc = Voice;
            if (!string.IsNullOrEmpty(voice))
                vc = voice;

            var result = service.Synthesize(
                text: text,
                accept: "audio/wav",
                voice: vc
                );

            if (!string.IsNullOrEmpty(savetofile))
            {
                using (FileStream fs = File.Create(savetofile))
                {
                    result.Result.WriteTo(fs);
                    fs.Close();
                }
            }

            var bytes = ReadFully(result.Result);
            return (true, bytes);
        }

        public async Task<(bool, List<string>)> GetVoices()
        { 
            if (string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ServiceUrl) || string.IsNullOrEmpty(Voice))
                return (false, null);

            IamAuthenticator authenticator = new IamAuthenticator(
                apikey: ApiKey);

            TextToSpeechService service = new TextToSpeechService(authenticator);
            service.SetServiceUrl(ServiceUrl);

            var result = service.ListVoices();

            return (true, result.Result._Voices.Where(v => !string.IsNullOrEmpty(v.Name)).Select(v => v.Name).ToList());
        }

    }
}
