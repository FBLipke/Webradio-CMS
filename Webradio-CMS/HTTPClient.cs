using System;
using System.Net;

namespace Namiono
{
	public class HTTPClient : IDisposable
	{
		Uri url;

		public delegate void HTTPClientEventHandler(object sender, HTTPClientEventArgs e);
		public event HTTPClientEventHandler HttpClientCompleted;

		WebRequest webRequest;
		
		public HTTPClient(string url, string method)
		{
			this.url = new Uri(url);
			webRequest = WebRequest.CreateHttp(this.url.OriginalString);
			webRequest.Method = method;

			Credentials = new CredentialCache();
		}

		public CredentialCache Credentials { get; set; }

		public void Dispose()
		{
			Credentials.Remove(url, "Basic");
		}

		public void MakeRequest(byte[] dataToSend, string contentType = "application/x-www-form-urlencoded; charset=UTF-8")
		{
			webRequest.ContentType = contentType;
			webRequest.Headers.Add("User-Agent", "Mozilla 5.0");
			webRequest.ContentLength = dataToSend.Length;

			using (var reqStream = webRequest.GetRequestStream())
			{
				reqStream.Write(dataToSend, 0, dataToSend.Length);
				reqStream.Close();
			}

			using (var response = (HttpWebResponse)webRequest.GetResponse())
			{
				var data = new byte[response.ContentLength];
				using (var strm = response.GetResponseStream())
				{
					strm.Read(data, 0, data.Length);
					strm.Close();
				}

				using (var evArgs = new HTTPClientEventArgs(response.StatusCode,
					response.StatusDescription, data, response.ContentType, response.ContentLength))
						HttpClientCompleted?.Invoke(this, evArgs);

				response.Close();
			}
		}
	}
}
