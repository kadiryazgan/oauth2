using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Ky.OAuth2
{
    static public class Google
    {
        static string GoogleClientId = ConfigurationManager.AppSettings["GoogleClientId"];
        static string GoogleClientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];
        static string GoogleCallbackUrl = ConfigurationManager.AppSettings["GoogleCallbackUrl"] ?? "/oauth2callback";

        static string GetRedirectUri
        {
            get
            {
                if (GoogleCallbackUrl.StartsWith("http:") || GoogleCallbackUrl.StartsWith("https://"))
                {
                    return GoogleCallbackUrl;
                }
                var url = HttpContext.Current.Request.Url;
                return url.Scheme + "://" + url.Authority + GoogleCallbackUrl;
            }
        }

        static public string GetOAuthUrl(string state = null)
        {
            return $"https://accounts.google.com/o/oauth2/auth?response_type=code&client_id={GoogleClientId}&redirect_uri={GetRedirectUri}&scope=https://www.googleapis.com/auth/userinfo.profile%20https://www.googleapis.com/auth/userinfo.email" +
                (state != null ? "&state=" + HttpUtility.UrlEncode(state) : null);
        }

        static public ProfileInfo GetUserProfile(string authCode)
        {
            return _loadProfileInfo(_loadAccessToken(authCode));
        }

        static string _loadAccessToken(string authCode)
        {
            var request = (HttpWebRequest)WebRequest.Create("https://accounts.google.com/o/oauth2/token");

            string postData = string.Format("code={0}&client_id={1}&client_secret={2}&redirect_uri={3}&grant_type=authorization_code",
                authCode,
                GoogleClientId,
                GoogleClientSecret,
                GetRedirectUri);

            var data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();

            return JObject.Parse(responseString).SelectToken("access_token").ToString();
        }

        static ProfileInfo _loadProfileInfo(string accessToken)
        {
            var url = $"https://www.googleapis.com/oauth2/v1/userinfo?access_token={accessToken}";
            using (var wc = new WebClient())
            {
                var r = wc.DownloadString(url);
                return JsonConvert.DeserializeObject<ProfileInfo>(r);
            }
        }
    }


    public class ProfileInfo
    {
        public string id;
        public string email;
        public string name;
        public string given_name;
        public string family_name;
        public string link;
        public string picture;
        public string gender;
        public string locale;
    }

}