using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Namiono
{
	public class ClientEntry<T> : Member<T>
	{

		HTTPClient httpClient;
		public ClientEntry()
		{
			Url = "http://localhost:7999/api";
			Console.WriteLine("Client URL: {0}", Url);
			httpClient = new HTTPClient(Url, "POST");
			httpClient.HttpClientCompleted += (sender, e) => {
					OutPut = Encoding.UTF8.GetString(e.Response);
					Console.WriteLine(OutPut);
			};

			httpClient.Credentials.Add(new Uri(Url), "Basic",
				new System.Net.NetworkCredential("admin", "goaway"));
		}

		/*
		public virtual void Download()
		{
			if (string.IsNullOrEmpty(Url))
				return;

			using (var wc = new WebClient())
			{
				wc.DownloadStringCompleted += (wc_sender, wc_e) =>
				{
					if (wc_e.Cancelled)
						return;

					OutPut = wc_e.Result;
				};

				wc.Headers.Add("Accept", "text/xml");
				wc.Headers.Add("User-Agent", string.Format("Namiono - DNAS Client"));

				wc.Encoding = Encoding.UTF8;
				wc.DownloadStringAsync(new Uri(Url));
			}
		}
		*/

		public void Heartbeat()
		{
			httpClient.MakeRequest(Encoding.UTF8.GetBytes("op=test&seq=128"));
		}
	}
}
