using System;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Xml;
using MaxMind.GeoIP2;

namespace Namiono
{
	/*
    public sealed partial class ShoutcastServer<T> : Provider<T, ShoutcastStream<T>>
	{
		Uri scURL;
		ushort port;
		string password;
		string hostname;

		//Users<T> users;
		DatabaseReader geodb;
		SQLDatabase<T> db;
		Filesystem fs;
		TranscastClients<T> autodj;
		string uaid;

		public enum ServerType
		{
			Legacy,
			Shoutcast2,
			Transcast
		}

		public ShoutcastServer(ref SQLDatabase<T> db, ref Filesystem fs, ref Users<T> users, string serverid,
			string hostname, ushort port, string password, bool isStateServer, bool transcastNeeded)
		{
			this.db = db;
			this.uaid = Guid.NewGuid().ToString();

			if (transcastNeeded)
			{
				var tc = this.db.SQLQuery("SELECT relay_host, relay_port, relay_admin, relay_pass FROM shoutcast");
				if (tc.Count != 0)
				{
					for (var i = uint.MinValue; i < tc.Count; i++)
					{
                        var _i = (T)Convert.ChangeType(i, typeof(T));
                        autodj = new TranscastClients<T>(tc[_i]["relay_host"],
							ushort.Parse(tc[_i]["relay_port"]), tc[_i]["relay_pass"], tc[_i]["relay_admin"]);
						Download(ref autodj);
					}
				}
			}

			this.ServerID = serverid;
			this.IsStateServer = isStateServer;
			this.hostname = hostname;
			this.password = password;
			this.port = port;
			this.users = users;
			this.fs = fs;

			this.geodb = new DatabaseReader(fs.ResolvePath("uploads/GeoLite2-City.mmdb"));

			var scurl = string.Format("http://{0}:{1}/statistics", this.hostname, this.port);
			scURL = new Uri(scurl);
		}

		public string Webplayer(T id)
		{
			var player_output = "<audio controls=\"\" preload=\"none\" muted=\"yes\" class=\"player_control\">\n";
			var strPath = Members[id].StreamPath;
			var ctype = Members[id].ContentType;

			player_output += string.Format("<source src=\"{0}\" type=\"{1}\" />\n",
				strPath, ctype);

			player_output += "Not supported!</audio>\n";

			return player_output;
		}

		public bool IsStateServer { get; private set; }

		public string OutPut(T userid)
		{
			var playerOutput = "<span id=\"streaminfo\"><p class=\"exclaim\">...</p></span>\n";

			if (Members.Count != 0)
			{
				foreach (var member in Members.Values)
				{
					if (member == null)
						continue;

					if (!member.Active)
						continue;

					playerOutput = member.MetaData;
					playerOutput += "<hr />";

					var nav_links = db.SQLQuery("SELECT * FROM navigation WHERE target ='playlist'");
					playerOutput += "<ul>";

					if (nav_links.Count != 0)
					{
						for (var i = uint.MinValue; i < nav_links.Count; i++)
						{
                            var _i = (T)Convert.ChangeType(i, typeof(T));
                            var data = nav_links[_i]["data"]
								.Replace("[#STREAMID#]", string.Format("{0}", member.Id))
								.Replace("[#SERVERID#]", string.Format("{0}", ServerID));

							var url = string.Format("/providers/{0}/?{1}", nav_links[_i]["link"], data);
							playerOutput += string.Format("<li><a href=\"#\" onclick=\"Window('{0}','{1}')\">{1}</a></li>", url, nav_links[_i]["name"]);
						}
					}
					playerOutput += "</ul>";
				}
			}

			return playerOutput;
		}

		void Download(ref TranscastClients<T> source)
		{
			using (var wc = new WebClient())
			{
				wc.DownloadStringCompleted += (wc_sender, wc_e) =>
				{
					try
					{
						if (wc_e.Cancelled)
							return;

						var result = wc_e.Result;
						if (!string.IsNullOrEmpty(result))
							ParseData(result);
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex.Message);
					}
				};

				wc.Headers.Add("Accept", "text/xml");
				wc.Headers.Add("User-Agent", string.Format("Namiono - DNAS Client ({0})", this.uaid));

				wc.Encoding = Encoding.UTF8;
				wc.DownloadStringAsync(scURL);

				if (source != null)
					source.Download();
			}
		}

		void ParseData<X>(X data)
		{
			using (var bgw = new BackgroundWorker())
			{
				bgw.WorkerReportsProgress = true;
				bgw.WorkerSupportsCancellation = true;
				bgw.RunWorkerAsync(data);

				bgw.DoWork += (bgw_doWorkSender, bgw_e) =>
				{
					var response = new XmlDocument();
					response.LoadXml((string)bgw_e.Argument);

					var totalStreams = byte.Parse(response.DocumentElement.
						SelectSingleNode("STREAMSTATS/ACTIVESTREAMS").InnerText);

					if (totalStreams != byte.MinValue && totalStreams < byte.MaxValue)
						for (var i = (byte)1; i <= totalStreams; i++)
						{
							var id = (T)Convert.ChangeType(i, typeof(T));
							lock (Members)
							{
								if (Members.ContainsKey(id))
									continue;

								var stream = new ShoutcastStream<T>(ref fs, ref geodb, ref users, hostname, port, password);
								stream.Id = id;

								if (Members.ContainsKey(stream.Id))
									continue;



								if (!string.IsNullOrEmpty(stream.Clients(users.GetUserIDbyName("Krawall-Ralf"))))
								{
									if (!Members.ContainsKey(stream.Id))
										Members.Add(stream.Id, stream);

									// Members[stream.Id]?.Update();
								}
							}
						}
				};

				bgw.RunWorkerCompleted += (bgw_done_sender, done_e) =>
				{
					//lock (Members)
					//	{
					//	foreach (var stream in Members.Values)
					//if (stream.Active)
					//stream?.Update();
					//}
				};
			}
		}

		public string ServerID { get; private set; }

        // NEU SCHREIBEN!!!! Hier ist ein Bug!
		public string ListCurrentStreams()
		{
			var box = Dispatcher.ReadTemplate(ref fs, "content_box");

			var tmp = "<div class=\"tr sp_day\"><div class=\"th sc_col_id\">Addresse</div><div class=\"th sc_col_id\">Modus</div>" +
				"<div class=\"th sc_col_id\">\"Statistic Relay\"-Rolle</div><div class=\"th sc_col_id\">Optionen</div></div>";

			using (var db = new SQLDatabase<T>("database.sqlite"))
			{
				var streams = db.SQLQuery("SELECT * FROM shoutcast");
				for (var i = uint.MinValue; i < streams.Count; i++)
				{
                    var _i = (T)Convert.ChangeType(i, typeof(T));
                    tmp += string.Format("<div class=\"tr sp_row\"><div class=\"td sc_col_id\"><a href=\"#\"onclick=\"Window('http://{0}:{1}/admin.cgi', 'Shoutcast Server')\">{0}</a></div>" +
                        "<div class=\"td sc_col_id\">Shoutcast2 & Proxy</div>" +
						"<div class=\"td sc_col_id\">{2}</div><div class=\"td sc_col_id\"><a href=\"#\" onclick=\"LoadDocument('/providers/shoutcast/','#content','Starten'," +
							"'execute','serverid={1}&sid={2}&param=start', '')\">Starten</a> - <a href=\"#\" onclick=\"LoadDocument('/providers/shoutcast/','#content','Stoppen'," +
							"'execute','serverid={1}&sid={2}&param=stop', '')\">Stop</a></div> - </div>", streams[_i]["hostname"], streams[_i]["port"], IsStateServer ? "Proxy" : "Client");
				}
			}

			return box.Replace("[[box-content]]", tmp).Replace("[[box-title]]", "Shoutcast Streams").Replace("[[content]]", "sc_streamcenter");
		}

		public void Heartbeat()
		{
			Download(ref autodj);

			lock (Members)
			{
				if (Members.Count == 0)
					return;

				// foreach (var member in Members.Values)
				// member?.Heartbeat();
			}
		}
	}
	*/
}
