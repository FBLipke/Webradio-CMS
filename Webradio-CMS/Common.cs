using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace Namiono
{
	public class Common
	{
		public static Servers<ServerEntry<Guid>> ServerProvider = new Servers<ServerEntry<Guid>>();
		public static Group<GroupEntry<Guid>> AccessProvider = new Group<GroupEntry<Guid>>();
		public static Users<UserEntry<Guid>> UserProvider = new Users<UserEntry<Guid>>();
		public static Clients<ClientEntry<Guid>> ClientProvider = new Clients<ClientEntry<Guid>>();
		public static News<NewsEntry<Guid>> NewsProvider = new News<NewsEntry<Guid>>();
		public static Navigation<NavigationEntry<Guid>> NavigationProvider = new Navigation<NavigationEntry<Guid>>();

		Dictionary<string, WebSite<uint>> webSites;

		SQLDatabase<uint> db;

		Thread heartbeat;
		bool running;

		public void Handle_Provider_Event(object sender, ProviderEventArgs<Guid> e)
		{
			var _caller = this;

			var response = e.Response;
			switch (e.Action)
			{
				case ProviderAction.Started:
					Console.WriteLine("Der Provider meldet dass er gestartet wurde...");
					break;
				case ProviderAction.Closed:
					Console.WriteLine("Der Provider meldet dass er beendet wurde...");
					break;
				case ProviderAction.RequestProceeded:
					Console.WriteLine("Der Provider meldet dass die Anforderung verarbeitet wurde...");
					break;
				case ProviderAction.RequestStarted:
					Console.WriteLine("Der Provider meldet dass die Anforderung verarbeitet wird...");
					break;
				case ProviderAction.MemberAdded:
					Console.WriteLine("Der Provider meldet dass das Objekt hinzugefügt wurde...");
					break;
				case ProviderAction.MemberRemoved:
					Console.WriteLine("Der Provider meldet dass das Objekt entfernt wurde...");
					break;
				case ProviderAction.ProviderLogin:
					Console.WriteLine("Der Provider meldet dass die Sitzung begonnen wurde...");
					break;
				case ProviderAction.ProviderLogout:
					Console.WriteLine("Der Provider meldet dass die Sitzung beendet wurde...");
					break;
				default:
					break;
			}

			switch (e.ErrorCode)
			{
				case ERROR_CODES.SUCCESS:
					e.Context.Response.StatusCode = 200;
					e.Context.Response.StatusDescription = "OK!";
					break;
				case ERROR_CODES.NOTFOUND:
					e.Context.Response.StatusCode = 404;
					e.Context.Response.StatusDescription = "Das angeforderte Object wurde nicht gefunden!";
					break;
				case ERROR_CODES.PERMISSIONS:
					e.Context.Response.StatusCode = 403;
					e.Context.Response.StatusDescription = "Der angemeldete Benutzer verfügt nicht über ausreichende Berechtigungen um die angeforderte Aktion auszuführen.";
					break;
				case ERROR_CODES.NOOBJECTS:
					e.Context.Response.StatusCode = 204;
					e.Context.Response.StatusDescription = "Der kontaktierte Provider meldet, dass dieser keine Objekte enthält";
					break;
				case ERROR_CODES.REQUIRED_ARGS_MISSED:
					e.Context.Response.StatusCode = 510;
					e.Context.Response.StatusDescription = "Erforderliche Parameter fehlen, um die Anfrage verarbeiten zu können.";
					break;
				case ERROR_CODES.NOT_HANDLED:
					e.Context.Response.StatusCode = 304;
					e.Context.Response.StatusDescription = "Die Anfrage wurde vom Server nicht verarbeitet.";
					break;
				case ERROR_CODES.INVALID_IDENTITY:
					e.Context.Response.StatusCode = 511;
					e.Context.Response.StatusDescription = "Der Client sendete falsche Daten!";
					break;
				case ERROR_CODES.REDIRECT_TO_NEW_LOCATION:
					e.Context.Response.StatusCode = 308;
					e.Context.Response.StatusDescription = "Redirected to new Location.";
					e.Context.Response.AddHeader("Location", "/");
					break;
			}

			e.Context.Response.ContentType = "Application/json";
			e.Socket.Send(ref response, ref e.Context, Encoding.UTF8);
		}

		public Common(string[] args)
		{
			webSites = new Dictionary<string, WebSite<uint>>();
			db = new SQLDatabase<uint>("database.sqlite");

			var websites = db.SQLQuery("SELECT * FROM websites");
			if (websites.Count != 0)
			{
				for (var i = uint.MinValue; i < websites.Count; i++)
				{
					var _i = i;
					AddWebSite(websites[_i]["name"], websites[_i]["title"], ushort.Parse(websites[_i]["port"]),
						(Realm)ushort.Parse(websites[_i]["realm"]), websites[_i]["hostname"], ref db);
				}
			}

			running = true;
			heartbeat = new Thread(new ParameterizedThreadStart(Heartbeat))
			{
				IsBackground = true
			};

		}

		void Heartbeat(object param)
		{
			var timer = 300 * 1000;

			while (running)
			{
				Thread.Sleep(timer);
				AccessProvider.HeartBeat();
				ClientProvider.HeartBeat();
				ServerProvider.HeartBeat();
				UserProvider.HeartBeat();
				NewsProvider.HeartBeat();
				NavigationProvider.HeartBeat();

				foreach (var website in webSites.Values)
					website.HeartBeat();

				Console.WriteLine("Heartbeat!...");
			}
		}

		public void Bootstrap()
		{
			var _fs = new Filesystem(Directory.GetCurrentDirectory());
			Console.WriteLine("[I] Bootstrap...");
			AccessProvider.ProviderResponse += Handle_Provider_Event;
			ServerProvider.ProviderResponse += Handle_Provider_Event;
			ClientProvider.ProviderResponse += Handle_Provider_Event;
			UserProvider.ProviderResponse += Handle_Provider_Event;
			NewsProvider.ProviderResponse += Handle_Provider_Event;
			NavigationProvider.ProviderResponse += Handle_Provider_Event;

			if (!Directory.Exists(Path.Combine(_fs.Root, "Providers")))
			{
				Directory.CreateDirectory(Path.Combine(_fs.Root, "Providers"));
				Console.WriteLine("[I] Provider installation needed..., Bootstrapping them...");
			}

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "System",
				Level = ulong.MaxValue,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "Server-Administratoren",
				Level = byte.MaxValue,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "Server-Operatoren",
				Level = 250,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "Seiten-Administroren",
				Level = 245,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "Seiten-Operatoren",
				Level = 240,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "News-Administratoren",
				Level = 235,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "News-Operatoren",
				Level = 230,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "Sendeplan-Administratoren",
				Level = 225,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "Sendeplan-Operatoren",
				Level = 220,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "Stream-Administratoren",
				Level = 215,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "Stream-Operatoren",
				Level = 210,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "Seiten-Mitglied",
				Level = 1,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			AccessProvider.Add(Guid.NewGuid(), new GroupEntry<Guid>()
			{
				Active = true,
				Name = "Unregistriert",
				Level = byte.MinValue,
				Locked = true,
				Provider = AccessProvider.Name,
			});

			#region "News Provider - Installation"


			var news_user = Guid.Empty;
			var xmlDoc = new XmlDocument();
			xmlDoc.Load(NewsProvider.InstallScript);
			var newsEntries = xmlDoc.SelectNodes("Namiono/Install/Users/User");

			for (var i = 0; i < newsEntries.Count; i++)
			{
				var res_level = ulong.MinValue;


				var user = new UserEntry<Guid>
				{
					Id = newsEntries[i].Attributes["Id"] == null ? Guid.NewGuid() : Guid.Parse(newsEntries[i].Attributes["Id"].Value),
					Name = newsEntries[i].Attributes["Name"].Value,
					Service = bool.Parse(newsEntries[i].Attributes["Service"].Value == "0" ? "false" : "true"),
					Moderator = bool.Parse(newsEntries[i].Attributes["Moderator"].Value == "0" ? "false" : "true"),
					Locked = bool.Parse(newsEntries[i].Attributes["Locked"].Value == "0" ? "false" : "true"),
					Password = MD5.GetMD5Hash(newsEntries[i].Attributes["Password"].Value),
					Provider = UserProvider.Name
				};

				if (newsEntries[i].Attributes["Level"] != null)
				{
					ulong.TryParse(newsEntries[i].Attributes["Level"].Value, out res_level);
					user.Level = res_level;
				}

				news_user = user.Id;

				UserProvider.Add(user.Id, user);
			}

			newsEntries = xmlDoc.SelectNodes("Namiono/Install/Entries/Entry");

			for (var i = 0; i < newsEntries.Count; i++)
			{
				var res_level = ulong.MinValue;

				var news = new NewsEntry<Guid>
				{
					Name = newsEntries[i].Attributes["Name"].Value,
					Service = bool.Parse(newsEntries[i].Attributes["Service"].Value == "0" ? "false" : "true"),
					Moderator = bool.Parse(newsEntries[i].Attributes["Moderator"].Value == "0" ? "false" : "true"),
					Locked = bool.Parse(newsEntries[i].Attributes["Locked"].Value == "0" ? "false" : "true"),
					OutPut = newsEntries[i].Attributes["Output"].Value.Replace("[[NL]]", "<br />"),
					Url = newsEntries[i].Attributes["Url"].Value,
					Author = news_user
				};

				if (newsEntries[i].Attributes["Level"] != null)
				{
					ulong.TryParse(newsEntries[i].Attributes["Level"].Value, out res_level);
					news.Level = res_level;
				}


				NewsProvider.Add(Guid.NewGuid(), news);
			}

			#endregion

			#region "Navigation Provider - Installation"
			xmlDoc.Load(NavigationProvider.InstallScript);

			var navEntries = xmlDoc.SelectNodes("Namiono/Install/Entries/Entry");

			for (var i = 0; i < navEntries.Count; i++)
			{
				var res_level = ulong.MinValue;

				var nav = new NavigationEntry<Guid>
				{
					Name = navEntries[i].Attributes["Name"].Value,
					Service = bool.Parse(navEntries[i].Attributes["Service"].Value == "0" ? "false" : "true"),
					Moderator = bool.Parse(navEntries[i].Attributes["Moderator"].Value == "0" ? "false" : "true"),
					Locked = bool.Parse(navEntries[i].Attributes["Locked"].Value == "0" ? "false" : "true"),
					OutPut = navEntries[i].Attributes["Output"].Value.Replace("[[NL]]", "<br />"),
					Url = navEntries[i].Attributes["Url"].Value,
					Author = Guid.Empty
				};

				if (navEntries[i].Attributes["Level"] != null)
				{
					ulong.TryParse(navEntries[i].Attributes["Level"].Value, out res_level);
					nav.Level = res_level;
				}


				NavigationProvider.Add(Guid.NewGuid(), nav);
			}


			NavigationProvider.Add(Guid.NewGuid(), new NavigationEntry<Guid>() { Name = "Mitglieder", Active = true, Locked = false, Level = 1, Frame = "#content", Provider = UserProvider.Name, Action = "show" });
			NavigationProvider.Add(Guid.NewGuid(), new NavigationEntry<Guid>() { Name = "News", Active = true, Locked = false, Level = 1, Frame = "#content", Provider = NewsProvider.Name, Action = "show" });

			#endregion
			AccessProvider.Bootstrap();
			ServerProvider.Bootstrap();
			ClientProvider.Bootstrap();
			UserProvider.Bootstrap();
			NewsProvider.Bootstrap();
			NavigationProvider.Bootstrap();





			Console.WriteLine("[I] Starting ...");
		}

		public void AddWebSite(string name, string title, ushort port, Realm realm, string hostname, ref SQLDatabase<uint> db)
		{
			var _caller = this;
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("A Website must have a unique name!");

			if (!webSites.ContainsKey(name))
				webSites.Add(name, new WebSite<uint>(ref _caller, ref db, name, title, port, realm, hostname));
		}

		public void RemoveWebSite(string name)
		{
			if (!webSites.ContainsKey(name))
				return;

			webSites[name]?.Close();
			webSites.Remove(name);
		}

		public void Start()
		{
			AccessProvider.Start();
			ServerProvider.Start();
			ClientProvider.Start();
			UserProvider.Start();
			NewsProvider.Start();
			NavigationProvider.Start();

			heartbeat.Start(45);
			foreach (var ws in webSites.Values)
				ws?.Start();
		}

		public void Close(bool remove = true)
		{
			running = false;

			foreach (var ws in webSites)
			{
				ws.Value?.Close();

				if (remove)
					webSites.Remove(ws.Key);
			}

			AccessProvider.Close();
			NavigationProvider.Close();
			ServerProvider.Close();
			ClientProvider.Close();
			NewsProvider.Close();
			UserProvider.Close();

			db.Close();
		}

		public void Dispose()
		{
			foreach (var ws in webSites.Values)
				ws?.Dispose();

			webSites.Clear();
			AccessProvider.Dispose();
			ServerProvider.Dispose();
			ClientProvider.Dispose();
			NewsProvider.Dispose();
			UserProvider.Dispose();
			NavigationProvider.Dispose();

			db.Dispose();
		}
	}
}
