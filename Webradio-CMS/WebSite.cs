using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Namiono
{
	public enum Realm
	{
		Public,
		Private,
		api,
		Admin
	}

	public class WebSite<T> : IDisposable
	{

		Dictionary<Guid, WebServer> Webservers;
		Filesystem fs;
		SQLDatabase<T> db;
		Realm realm;
		Common common;

		string design;
		string title;

		Guid Get_UserID_From_Cookie(ref HttpListenerContext context)
		{
			if (context.Request.Cookies["User"] != null)
			{
				var uid = Guid.Parse(context.Request.Cookies["User"].Value);
				if (!context.Request.Cookies["User"].Expired)
				{
					if (Common.UserProvider.Contains(uid) && !context.Request.Cookies["User"].Expired)
						return uid;
				}
			}

			return Guid.Empty;
		}

		void Handle_Provider_Request(ref Common common, ref WebServer.Provider provider, ref WebServer.SiteAction action,
			ref HttpListenerContext context, ref Guid userid, ref Dictionary<string, string> parameters, Guid instance)
		{
			var error = ERROR_CODES.NOT_HANDLED;
			var output = "";
			var data = "";
			userid = Get_UserID_From_Cookie(ref context);

			if (context.Request.Headers["UAgent"] == null)
			{
				error = ERROR_CODES.INVALID_IDENTITY;
				if (context.Request.Headers["UAgent"] != "Namiono_new")
				{
					data = JsonConvert.SerializeObject(output);

					context.Response.ContentType = "Application/json";
					context.Response.StatusCode = 511;
					context.Response.StatusDescription = "Network Authentication Required!";

					Webservers[instance].Send(ref data, ref context, Encoding.UTF8);
					return;
				}
			}

			var _ws = Webservers[instance];
			switch (provider)
			{
				case WebServer.Provider.Navigation:
					Common.NavigationProvider.Request(ref common, action, ref _ws, userid, ref context, ref parameters);
					break;
				case WebServer.Provider.News:
					Common.NewsProvider.Request(ref common, action, ref _ws, userid, ref context, ref parameters);
					break;
				case WebServer.Provider.User:
					Common.UserProvider.Request(ref common, action, ref _ws, userid, ref context, ref parameters);
					break;
				default:
					break;
			}
		}

		public WebSite(ref Common common, ref SQLDatabase<T> db, string name, string title, ushort port, Realm realm, string hostname, string design = "default")
		{
			this.title = title;
			this.db = db;
			this.common = common;
			this.design = design;
			this.realm = realm;

			Webservers = new Dictionary<Guid, WebServer>();
			fs = new Filesystem(Path.Combine(Environment.CurrentDirectory, string.Format("{0}_website", name)));

			foreach (var type in new[] { WebServer.RequestTarget.Site, WebServer.RequestTarget.api })
			{
				var _guid = Guid.NewGuid();

				var ws = new WebServer(_guid, type, this.title, ref fs, port, hostname);
				ws.DirectRequestReceived += (sender, e) =>
				{
					var p = e.Path.Split('.')[0].ToLower();
					if (p.StartsWith("/"))
						p = p.Remove(0, 1);

					var userid = Get_UserID_From_Cookie(ref e.Context);

					foreach (var item in e.Params)
					{
						Console.WriteLine("{0} : {1} ", item.Key, e.Params[item.Key]);
					}

					switch (e.Target)
					{
						case WebServer.RequestTarget.api:
							if (e.Context.User.Identity.IsAuthenticated)
							{
								var identity = (HttpListenerBasicIdentity)e.Context.User.Identity;
								Console.WriteLine(identity.Name);
								Console.WriteLine(identity.Password);
							}

							if (e.Context.User.Identity.IsAuthenticated)
							{
								Handle_Provider_Request(ref this.common, ref e.Provider, ref e.Action, ref e.Context, ref userid, ref e.Params, e.Instance);
							}
							else
							{
								e.Context.Response.StatusCode = 401;
								e.Context.Response.StatusDescription = "Network Authentication Required!";

								var data = new byte[0];
								Webservers[e.Instance].Send(ref data, ref e.Context);
							}
							break;
						case WebServer.RequestTarget.Site:
								Handle_Site_Request(ref userid, ref e.Context, e.Instance, ref e.Params);
							break;
					}
				};

				Webservers.Add(_guid, ws);
			}
		}

		private void Handle_Site_Request(ref Guid userid, ref HttpListenerContext context, Guid instance, ref Dictionary<string, string> @params)
		{
			var output = Dispatcher.ReadTemplate(ref fs, "site-content");

			output = output.Replace("[#YEAR#]", string.Format("{0}", DateTime.Now.Year));
			output = output.Replace("[#MONTH#]", string.Format("{0}", DateTime.Now.Month));
			output = output.Replace("[#DESIGN#]", Common.UserProvider.Contains(userid) ?
				Common.UserProvider.Get_Member(userid).Design : this.design);

			output = output.Replace("[#SITE_TITLE#]", this.title);
			output = output.Replace("[#APPNAME#]", "Namiono");
#if DEBUG
			output = output.Replace("[#DEBUG_WARN#]", "<p class=\"developer\">Debug-Version!" +
				" Die Version befindet sich (noch) in Entwicklung. Fehler sind zu erwarten.</p>");
#else
			site = site.Replace("[#DEBUG_WARN#]", string.Empty);
#endif
			context.Response.ContentType = "text/html";
			context.Response.StatusCode = 200;
			context.Response.StatusDescription = "OK";
			Webservers[instance].Send(ref output, ref context, Encoding.UTF8);
		}

		public void Start()
		{
			foreach (var ws in Webservers.Values)
				ws.Start();
		}

		public void HeartBeat()
		{
			db.HeartBeat();
		}

		public void Close()
		{
			foreach (var ws in Webservers.Values)
				ws.Close();
		}

		public void Dispose()
		{
			foreach (var ws in Webservers.Values)
				ws.Dispose();
		}
	}
}
