using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Engine
{
    public static class HttpUtils
    {
        public static int DefaultConnectionLimit
        {
            get { return System.Net.ServicePointManager.DefaultConnectionLimit; }
            set { System.Net.ServicePointManager.DefaultConnectionLimit = value; }
        }

        public static async Task<TResult> MakeHttpCallAsync<TResult>(
            string requestUriString, 
            Func<TextReader, TResult> consumeTextReader,
            bool pretendBrowser = false)
        {
            HttpWebRequest request = WebRequest.CreateHttp(requestUriString);
            if (pretendBrowser)
            {
                request.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1) ; .NET CLR 2.0.50727; .NET CLR 3.0.04506.30; .NET CLR 1.1.4322; .NET CLR 3.5.20404)");
            }

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            {
                return consumeTextReader(streamReader);
            }
        }

        public static async Task<TResult> MakeHttpCallAsync<TResult>(
            string host,
            string method,
            string path,
            string accessToken,
            string clientApplicationName,
            Func<TextReader, TResult> consumeTextReader,
            string[] queryArgs = null)
        {
            HttpWebRequest request = CreateHttpsWebRequest(host, method, path, accessToken, clientApplicationName, queryArgs);

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            {
                return consumeTextReader(streamReader);
            }
        }

        private static HttpWebRequest CreateHttpsWebRequest(
            string host,
            string method,
            string path,
            string accessToken,
            string clientApplicationName,
            string[] queryArgs = null)
        {
            Uri uri = new UriBuilder("https", host)
                {
                    Path = path,
                    Query = queryArgs != null ? String.Join("&", queryArgs) : null
                }.Uri;

            HttpWebRequest request = WebRequest.CreateHttp(uri);
            request.Method = method;
            request.Headers.Add("x-ms-client-application-name", clientApplicationName);
            request.Headers.Add("Authorization", "Bearer " + accessToken);
            return request;
        }        
    }
}