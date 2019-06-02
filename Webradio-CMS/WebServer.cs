using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Namiono
{
	public sealed class WebServer : IDisposable
	{
		public enum RequestType
		{
			Async,
			Sync
		}

		public enum SiteAction : byte
		{
			None = 0,
			Add = 1,
			Edit = 2,
			Remove = 3,
			Login = 4,
			Logout = 5,
			Show = 6,
			MetaData = 7,
			Teamlist = 8,
			Clients = 9,
			Profile = 10,
			Register = 11,
			Execute = 12
		}

		public enum HTTPMethod
		{
			POST,
			GET
		}

		public enum RequestTarget
		{
			Site,
			api
		}

		public enum Provider
		{
			Navigation,
			User,
			None,
			Admin,
			News,
			Client,
			Server,
			Content
		}

		public delegate void DirectRequestEventHandler(object sender, DirectRequestEventArgs e);
		public delegate void DataSendEventHandler(object sender, DataSendEventArgs e);
		public delegate void ErrorEventHandler(object sender, ErrorEventArgs e);

		public class DirectRequestEventArgs : EventArgs
		{
			public HttpListenerContext Context;
			public RequestType RequestType;
			public RequestTarget Target;
			public Guid Instance;
			public Provider Provider;
			public SiteAction Action;
			public HTTPMethod Method;
			public string Path;
			public Dictionary<string, string> Params;
		}

		public class DataSendEventArgs : EventArgs
		{
			public string Bytessend;
		}

		public class ErrorEventArgs : EventArgs
		{
			public Exception Exception;

			public ErrorEventArgs(Exception ex) => Exception = ex;
		}

		public event DirectRequestEventHandler DirectRequestReceived;
		public event DataSendEventHandler HTTPDataSend;

		HttpListener listener;
		Filesystem fs;
		string title;
		string hostname;
		Guid passKey = Guid.Empty;

		RequestTarget type;
		Guid instance;

		public WebServer(Guid id, RequestTarget type, string title, ref Filesystem fs, int port, string hostname)
		{
			this.instance = id;
			this.fs = fs;
			this.title = title;
			this.type = type;
			this.hostname = hostname;
			this.passKey = Guid.NewGuid();

			listener = new HttpListener
			{
				IgnoreWriteExceptions = true,
				AuthenticationSchemes = AuthenticationSchemes.Basic | AuthenticationSchemes.Anonymous


			};

			listener.Prefixes.Add(string.Format("http://*:{0}/",
				type == RequestTarget.Site ? port : (port + 1)));
		}

		public void Start()
		{
			listener.Start();
			listener.BeginGetContext(HandleClientConnections, null);
		}

		void HandleClientConnections(IAsyncResult ar)
		{
			var context = listener.EndGetContext(ar);
			var reqType = RequestType.Sync;
			var target = RequestTarget.Site;
			var action = SiteAction.None;
			var method = (context.Request.HttpMethod == "POST") ?
				HTTPMethod.POST : HTTPMethod.GET;

			var path = GetContentType(context.Request.Url.LocalPath == "/" ? "/index.html" :
				context.Request.Url.LocalPath, ref context);

			var formdata = GetPostData(fs, path, context, method).Result;

			var provider = Provider.None;

			if (path.StartsWith("/providers/"))
			{
				target = RequestTarget.api;

				if (path.EndsWith("/server/"))
					provider = Provider.Server;

				if (path.EndsWith("/users/"))
					provider = Provider.User;

				if (path.EndsWith("/navigation/"))
					provider = Provider.Navigation;

				if (path.EndsWith("/news/"))
					provider = Provider.News;

				if (path.EndsWith("/admincp/"))
					provider = Provider.Admin;
			}
			else
				target = RequestTarget.Site;

			var req = context.Request.Headers["Request-Type"];
			if (string.IsNullOrEmpty(req))
				req = (target == RequestTarget.Site) ? "sync" : "async";

			// In general we have this Header when we use Ajax!
			if (context.Request.Headers["X-Requested-With"] != null)
			{
				if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
				req = "async";
			}

			#region "Action"
			var act = context.Request.Headers["Action"];
			if (string.IsNullOrEmpty(act))
				act = "none";

			switch (act.ToLower())
			{
				case "add":
					action = SiteAction.Add;
					break;
				case "edit":
					action = SiteAction.Edit;
					break;
				case "del":
					action = SiteAction.Remove;
					break;
				case "login":
					action = SiteAction.Login;
					break;
				case "logout":
					action = SiteAction.Logout;
					break;
				case "clients":
					action = SiteAction.Clients;
					break;
				case "profile":
					action = SiteAction.Profile;
					break;
				case "show":
					action = SiteAction.Show;
					break;
				case "metadata":
					action = SiteAction.MetaData;
					break;
				case "register":
					action = SiteAction.Register;
					break;
				case "none":
					action = SiteAction.None;
					break;
				case "execute":
					action = SiteAction.Execute;
					break;
				case "teamlist":
					action = SiteAction.Teamlist;
					provider = Provider.User;
					break;
				default:
					if (reqType != RequestType.Sync)
						throw new Exception(string.Format("<void> HandleClientConnections() -> " +
							" (Get) SiteAction: Got unknown SiteAction \"{0}\"!", act.ToLower()));
					break;
			}
			#endregion

			switch (req.ToLower())
			{
				case "async":
					reqType = RequestType.Async;
					break;
				case "sync":
					reqType = RequestType.Sync;
					break;
				default:
					throw new Exception(string.Format("<void> HandleClientConnections() -> " +
						"(Get) RequestType: Got unknown request \"{0}\"!", req.ToLower()));
			}

			switch (target)
			{
				case RequestTarget.api:
				case RequestTarget.Site:
					if (path.EndsWith("/") || path == "/" || path.EndsWith(".html") || path.EndsWith(".htm"))
					{
						DirectRequestReceived(this,  new DirectRequestEventArgs
						{
							Method = method,
							Provider = provider,
							Context = context,
							Params = formdata,
							RequestType = reqType,
							Target = target,
							Instance = instance,
							Path = path.Split('?')[0],
							Action = action
						});
					}
					else
					{
						if (fs.Exists(path))
						{
							context.Response.StatusCode = 200;
							context.Response.StatusDescription = "OK";

							var data = fs.Read(path);

							if (path.EndsWith(".js"))
							{
								var _tmpText = Encoding.UTF8.GetString(data).Replace("[#XDMP_AUTH#]", this.passKey.ToString());
								data = Encoding.UTF8.GetBytes(_tmpText);
							}

							Send(ref data, ref context);
						}
						else
						{
							var data = SendErrorDocument(title, 404, 
								string.Format("Datei \"{0}\" nicht gefunden!", path),
									ref context);

							Send(ref data, ref context);
						}
					}
					break;
				default:
					throw new Exception(string.Format("<void> HandleClientConnections()" +
						" -> (Get) RequestTarget: Got unknown request target \"{0}\"!", target.ToString()));
			}

			listener.BeginGetContext(HandleClientConnections, null);
		}

		public byte[] SendErrorDocument(string title, int statusCode, string desc, ref HttpListenerContext context)
		{
			var output = Dispatcher.ReadTemplate(ref fs, "static_site");
			output = output.Replace("[#SITE_TITLE#]", string.Format("{0}", title));
			output = output.Replace("[#CONTENT#]", string.Format("{0}", desc));
			output = output.Replace("[#DESIGN#]", string.Format("{0}", "default"));

			context.Response.StatusCode = statusCode;
			context.Response.StatusDescription = desc;
			context.Response.ContentType = "text/html";

			return Encoding.UTF8.GetBytes(output);
		}

		public void Send(ref string data, ref HttpListenerContext context, Encoding encoding)
		{
			var bytes = encoding.GetBytes(data);
			Send(ref bytes, ref context);
		}

		public void Send(ref byte[] data, ref HttpListenerContext context)
		{
			context.Response.Headers.Add("Server", string.Empty);
			context.Response.Headers.Add("Date", string.Empty);

			context.Response.ContentLength64 = data.Length;
			context.Response.OutputStream.Write(data, 0, data.Length);

			context.Response.OutputStream.Close();
			context.Response.Close();
		}

		public void Close()
		{
			if (!listener.IsListening)
				return;

			listener.Stop();
			listener.Close();
		}

		public void Dispose()
			=> listener.Close();

		public static async Task<Dictionary<string, string>> GetPostData(Filesystem fs,
			string path, HttpListenerContext context, HTTPMethod method)
		{
			var formdata = new Dictionary<string, string>();

			switch (method)
			{
				case HTTPMethod.POST:
					var encoding = context.Request.ContentEncoding;
					var ctype = context.Request.ContentType;
					var line = string.Empty;

					using (var reader = new StreamReader(context.Request.InputStream, encoding))
						line = reader.ReadToEnd();

					if (string.IsNullOrEmpty(line))
						return formdata;

					if (!string.IsNullOrEmpty(ctype))
					{
						if (ctype.Split(';')[0] != "application/x-www-form-urlencoded")
						{
							var boundary = ctype.Split('=')[1];

							if (string.IsNullOrEmpty(line))
								return null;

							var start = line.IndexOf(boundary) + (boundary.Length + 2);
							var end = line.LastIndexOf(boundary) + boundary.Length;
							line = line.Substring(start, end - start);
							var formparts = new List<string>();

							while (line.Contains(boundary))
							{
								if (line.StartsWith("Content-Disposition:"))
								{
									start = line.IndexOf("Content-Disposition: form-data;") +
										"Content-Disposition: form-data;".Length;

									end = line.IndexOf(boundary);
									formparts.Add(line.Substring(start, end - start).TrimStart());
									line = line.Remove(0, end);
								}

								if (line.StartsWith(boundary))
								{
									if (line.Length > boundary.Length + 2)
										line = line.Remove(0, boundary.Length + 2);
									else
										break;
								}
							}

							foreach (var item in formparts)
								if (item.Contains("filename=\""))
								{
									var posttag = item.Substring(0, item.IndexOf(";"));
									if (string.IsNullOrEmpty(posttag))
										continue;

									var data = item;
									start = data.IndexOf("filename=\"") + "filename=\"".Length;
									data = data.Remove(0, start);
									end = data.IndexOf("\"");

									var filename = data.Substring(0, end);
									if (string.IsNullOrEmpty(filename))
										continue;

									if (filename.Contains("\\") || filename.Contains("/"))
									{
										var parts = filename.Split(filename.Contains("\\") ? '\\' : '/');
										filename = parts[parts.Length - 1];
									}

									start = data.IndexOf("Content-Type: ");
									data = data.Remove(0, start);
									end = data.IndexOf("\r\n");

									var cType = data.Substring(0, end + 2);
									data = data.Remove(0, end + 2);

									var filedata = context.Request.ContentEncoding
										.GetBytes(data.Substring(2, data.IndexOf("\r\n--")));

									if (filedata.Length != 0)
									{
										var uploadpath = Filesystem.Combine(path, filename);

										try
										{
											await fs.Write(uploadpath, filedata);

											if (!formdata.ContainsKey(posttag))
												formdata.Add(posttag, uploadpath);
										}
										catch
										{
											continue;
										}
									}
#if DEBUG
									else
										throw new Exception(string.Format("Die hochgeladene Datei \"{0}\", enthält keine Daten (leer?)!", filename));
#endif
								}
								else
								{
									var x = item.Replace("\r\n--", string.Empty).Replace("name=\"",
										string.Empty).Replace("\"", string.Empty).Replace("\r\n\r\n", "|").Split('|');
									x[0] = x[0].Replace(" file", string.Empty);

									if (!formdata.ContainsKey(x[0]))
										formdata.Add(x[0].Replace("\"",string.Empty), x[1].Replace("\"", string.Empty));
								}

							formparts.Clear();
							formparts = null;
						}
						else
						{
							var tmp = line.Split('&');
							for (var i = 0; i < tmp.Length; i++)
								if (tmp[i].Contains("="))
								{
									var p = tmp[i].Split('=');
									if (!formdata.ContainsKey(p[0]))
										formdata.Add(p[0].Replace("\"", string.Empty), HttpUtility.UrlDecode(p[1]).Replace("\"", string.Empty));
								}
						}
					}

					break;
				case HTTPMethod.GET:
					if (path.Contains("?") && path.Contains("&") && path.Contains("="))
					{
						var get_params = HttpUtility.UrlDecode(path).Split('?')[1].Split('&');
						for (var i = 0; i < get_params.Length; i++)
							if (get_params[i].Contains("="))
							{
								var p = get_params[i].Split('=');
								if (!formdata.ContainsKey(p[0]))
									formdata.Add(p[0], p[1]);
							}
					}

					break;
				default:
					break;
			}

			return formdata;
		}

		public static string GetContentType(string path, ref HttpListenerContext context)
		{
			if (path.EndsWith(".css"))
				context.Response.ContentType = "text/css";

			if (path.EndsWith(".js"))
				context.Response.ContentType = "text/javascript";

			if (path.EndsWith(".htm") || path.EndsWith(".html"))
				context.Response.ContentType = "text/html";

			if (path.EndsWith(".png"))
				context.Response.ContentType = "image/png";

			if (path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
				context.Response.ContentType = "image/jpg";

			if (path.EndsWith(".gif"))
				context.Response.ContentType = "image/gif";

			if (path.EndsWith(".appcache"))
				context.Response.ContentType = "text/cache-manifest";

			if (path.EndsWith(".woff2"))
				context.Response.ContentType = "application/font-woff2";

			if (path.EndsWith(".ico"))
				context.Response.ContentType = "image/x-icon";

			return path.ToLowerInvariant();
		}
	}
}
