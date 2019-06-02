using Namiono.Widgets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Namiono
{
	public enum ERROR_CODES
	{
		SUCCESS = 0,
		NOTFOUND = 1,
		PERMISSIONS = 2,
		NOOBJECTS = 3,
		REQUIRED_ARGS_MISSED = 4,
		NOT_HANDLED = 5,
		INVALID_IDENTITY = 6,
		OPERATION_IN_PROGRESS = 7,
		REDIRECT_TO_NEW_LOCATION = 8
	}

	public enum FormatType
	{
		Object = 0,
		List = 1,
		Widget = 2
	}

	public enum ProviderAction
	{
		Started,
		Closed,
		RequestProceeded,
		RequestStarted,
		MemberAdded,
		MemberRemoved,
		ProviderLogin,
		ProviderLogout
	}

	public static class Provider
	{
		/// <summary>
		/// Determines whether this instance has method the specified name.
		/// </summary>
		/// <returns><c>true</c> if this instance has method the specified obj name; otherwise, <c>false</c>.</returns>
		/// <param name="obj">Object.</param>
		/// <param name="name">Name.</param>
		public static bool HasMethod(Type obj, string name) => obj.GetMethod(name) != null;

		/// <summary>
		/// Determines whether this instance has property the specified name.
		/// </summary>
		/// <returns><c>true</c> if this instance has property the specified obj name; otherwise, <c>false</c>.</returns>
		/// <param name="obj">Object.</param>
		/// <param name="name">Name.</param>
		public static bool HasProperty(Type obj, string name) => obj.GetProperty(name) != null;

		/// <summary>
		/// Determines whether this instance has field the specified name.
		/// </summary>
		/// <returns><c>true</c> if this instance has field the specified obj name; otherwise, <c>false</c>.</returns>
		/// <param name="obj">Object.</param>
		/// <param name="name">Name.</param>
		public static bool HasField(Type obj, string name) => obj.GetField(name) != null;

		/// <summary>
		/// Determines whether this instance has an event with the specified name.
		/// </summary>
		/// <returns><c>true</c> if this instance has event the specified obj name; otherwise, <c>false</c>.</returns>
		/// <param name="obj">Object.</param>
		/// <param name="name">Name.</param>
		public static bool HasEvent(Type obj, string name) => obj.GetEvent(name) != null;

		/// <summary>
		/// Returns the Value of Properties
		/// </summary>
		/// <typeparam name="Y"></typeparam>
		/// <param name="member">The Instance which contains the Property</param>
		/// <param name="propertyName">The Name of the Property</param>
		/// <returns>The Value from the Property</returns>
		public static Y GetPropertyValue<Y>(object member, string propertyName)
		{
			var value = member.GetType().GetProperties()
				.Single(pi => pi.Name == propertyName)
					.GetValue(member, null);

			return (Y)Convert.ChangeType(value, typeof(Y));
		}

		/// <summary>
		/// Sets or appends the Value for a Property.
		/// </summary>
		/// <typeparam name="S"></typeparam>
		/// <param name="obj"></param>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <param name="append"></param>
		/// <param name="delimeter"></param>
		public static void SetPropertyValue<S>(object obj, string name, S value, bool append = false, string delimeter = ";")
		{
			var prop = obj.GetType().GetProperties()
		   .Single(pi => pi.Name == name);

			if (typeof(S) == typeof(string))
			{
				if (append)
				{
					var curval = GetPropertyValue<S>(obj, name);
					var newval = string.Concat(curval, delimeter, value);

					prop.SetValue(obj, newval);
				}
				else
					prop.SetValue(obj, value);
			}
			else
				prop.SetValue(obj, value);
		}

		/// <summary>
		/// Invokes a Method
		/// </summary>
		/// <typeparam name="S"></typeparam>
		/// <param name="obj"></param>
		/// <param name="name"></param>
		/// <param name="parameters"></param>
		public static void InvokeMethod<S>(S obj, string name, object[] parameters = null)
		{
			var method = obj.GetType().GetMethod(name);
			method?.Invoke(typeof(S), parameters);
		}

		/// <summary>
		/// Subscriptes a Event on a Member.
		/// </summary>
		/// <typeparam name="S"></typeparam>
		/// <param name="obj"></param>
		/// <param name="origin"></param>
		/// <param name="name"></param>
		/// <param name="method"></param>
		public static void SubscribeEvent<S>(S obj, object origin, string name, string method)
		{
			var evt = obj.GetType().GetEvent(name);
			var del = Delegate.CreateDelegate(evt.EventHandlerType,
				obj, origin.GetType().GetMethod(method));
			evt.AddEventHandler(obj, del);
		}
	}

	public class Provider<I> : IProvider<I>
	{
		public delegate void ProviderEventHandler(object sender, ProviderEventArgs<Guid> e);
		public event ProviderEventHandler ProviderResponse;
		public bool Installed = false;

		public Provider()
		{
			Name = GetType().Name.Split('`')[0];

			FileSystem = new Filesystem(Filesystem.Combine("Providers", Name));
			InstallScript = Filesystem.Combine(FileSystem.Root, "Install.xml");
			var db_path = Filesystem.Combine(FileSystem.Root, "database.db");
			Installed = File.Exists(db_path);
			Database = new SQLDatabase<uint>(db_path);
			Members = new Dictionary<Guid, I>();
		}

		/// <summary>
		/// Bootstraps this Provider and its Members.
		/// </summary>
		public void Bootstrap()
		{
			Directory.CreateDirectory(Filesystem.Combine(FileSystem.Root, "Images"));

			if (!Installed)
				if (Name != "Provider")
					Install(InstallScript);
		}

		/// <summary>
		/// Returns the Name of this Provider.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Adds an Object to the Provider
		/// </summary>
		/// <param name="id"></param>
		/// <param name="member"></param>
		public void Add(Guid id, I member)
		{
			lock (Members)
				if (!Contains(id))
				{
					Provider.SetPropertyValue(member, "Id", id);
					Members.Add(id, member);
				}
		}

		/// <summary>
		/// Returns the Installer script for this Provider.
		/// </summary>
		public string InstallScript { get; private set; }

		/// <summary>
		/// Exist the specified Object.
		/// </summary>
		/// <param name="id">THe Id of the Object</param>
		public bool Contains(Guid id)
			=> Members.ContainsKey(id);

		/// <summary>
		/// Removes the specified Object.
		/// </summary>
		/// <param name="id">Id of the Objext</param>
		public ERROR_CODES Remove(Guid id)
		{
			var result = ERROR_CODES.NOTFOUND;
			lock (Members)
			{
				if (Contains(id))
				{
					Members.Remove(id);
					result = ERROR_CODES.SUCCESS;
				}
			}

			return result;
		}

		/// <summary>
		/// Closes all Members
		/// </summary>
		public void Close()
		{
			lock (Members)
				foreach (var member in Members.Values.Where(x => Provider.HasMethod(x.GetType(), "Close")))
					Provider.InvokeMethod(member, "Close", null);

			Database.Close();
		}

		/// <summary>
		/// Starts all members of this provider
		/// </summary>
		public void Start()
		{
			lock (Members)
				foreach (var member in Members.Values.Where(x => Provider.HasMethod(x.GetType(), "Start")))
					Provider.InvokeMethod(member, "Start", null);
		}

		/// <summary>
		/// Calls Heartbeat routines for this Profrovider and its Members.
		/// </summary>
		public void HeartBeat()
		{
			lock (Members)
				foreach (var item in Members.Values.Where(x => Provider.HasMethod(x.GetType(), "HeartBeat")))
					Provider.InvokeMethod(item, "HeartBeat", null);

			Database.HeartBeat();
		}

		/// <summary>
		/// Updates all Members of this Provider
		/// </summary>
		public void Update()
		{
			lock (Members)
			{
				foreach (var item in Members.Values.Where(x => Provider.HasMethod(x.GetType(), "Update")))
					Provider.InvokeMethod(item, "Update", null);

				Commit();
			}
		}

		/// <summary>
		/// Disposes all Members
		/// </summary>
		public void Dispose()
		{
			lock (Members)
				foreach (var member in Members.Values.Where(x => Provider.HasMethod(x.GetType(), "Dispose")))
					Provider.InvokeMethod(member, "Dispose", null);

			Members.Clear();
			Database.Dispose();
		}

		public IEnumerable<PropertyInfo> Properties =>
			typeof(I).GetProperties().Where(p => p.GetGetMethod().IsPublic)
				.Where(p => p.PropertyType.FullName.StartsWith("System"))
					.Where(p => !p.PropertyType.FullName.Contains("Collections"));

		/// <summary>
		/// Create Entries in Database 
		/// </summary>
		public void Insert()
		{
			if (!Properties.Any())
				return;

			using (var writer = new StreamWriter(Path.Combine(FileSystem.Root, "database_update.sql")))
			{
				writer.NewLine = Environment.NewLine;
				writer.AutoFlush = true;

				writer.WriteLine("INSERT INTO {0} ({1}) ", Name, string.Join(",", Properties.Select(p => p.Name)));

				var _iterates = 1;
				var line = "VALUES (";
				var _value = "";

				foreach (var __Property in Properties)
				{
					var _typeName = __Property.PropertyType.ToString();
					var _typeNameparts = _typeName.Split('.');
					_typeName = _typeNameparts[_typeNameparts.Length - 1];

					if (__Property.Name == "Id")
						continue;

					switch (_typeName.ToLower())
					{
						case "ipaddress":
						case "string":
							_value += string.Format("'{0}',", Provider.GetPropertyValue<string>(this, __Property.Name));
							break;
						case "double":
							_value += string.Format("'{0}',", Provider.GetPropertyValue<double>(this, __Property.Name));
							break;
						case "boolean":
							_value += string.Format("'{0}',", Provider.GetPropertyValue<bool>(this, __Property.Name));
							break;
						case "uint64":
							_value += string.Format("'{0}',", Provider.GetPropertyValue<ulong>(this, __Property.Name));
							break;
						case "uint32":
							_value += string.Format("'{0}',", Provider.GetPropertyValue<uint>(this, __Property.Name));
							break;
						case "uint16":
							_value += string.Format("'{0}',", Provider.GetPropertyValue<ushort>(this, __Property.Name));
							break;
						case "int16":
							_value += string.Format("'{0}',", Provider.GetPropertyValue<short>(this, __Property.Name));
							break;
						case "int32":
							_value += string.Format("'{0}',", Provider.GetPropertyValue<int>(this, __Property.Name));
							break;
						case "int64":
							_value += string.Format("'{0}',", Provider.GetPropertyValue<long>(this, __Property.Name));
							break;
						default:
							break;
					}

					line += _value;
					_iterates++;
				}

				line += ")";
				writer.WriteLine(line);
			}
		}

		/// <summary>
		/// Commits Data to Database
		/// </summary>
		public void Commit()
		{
			if (!Properties.Any())
				return;

			using (var writer = new StreamWriter(Path.Combine(FileSystem.Root, "database_update.sql")))
			{
				writer.NewLine = Environment.NewLine;
				writer.AutoFlush = true;

				foreach (var __Property in Properties)
				{
					var _typeName = __Property.PropertyType.ToString();
					var _typeNameparts = _typeName.Split('.');
					_typeName = _typeNameparts[_typeNameparts.Length - 1];

					switch (_typeName.ToLower())
					{
						case "ipaddress":
						case "string":
							writer.WriteLine("UPDATE {0} SET {1}='{2}' WHERE Id='{2}' LIMIT '1'",
								Name, __Property.Name, Provider.GetPropertyValue<string>(this, __Property.Name));
							break;
						case "double":
							writer.WriteLine("UPDATE {0} SET {1}='{2}' WHERE Id='{2}' LIMIT '1'",
								Name, __Property.Name, Provider.GetPropertyValue<double>(this, __Property.Name));
							break;
						case "boolean":
							writer.WriteLine("UPDATE {0} SET {1}='{2}' WHERE Id='{2}' LIMIT '1'",
								Name, __Property.Name, Provider.GetPropertyValue<bool>(this, __Property.Name));
							break;
						case "uint64":
							writer.WriteLine("UPDATE {0} SET {1}='{2}' WHERE Id='{2}' LIMIT '1'",
								Name, __Property.Name, Provider.GetPropertyValue<ulong>(this, __Property.Name));
							break;
						case "uint32":
							writer.WriteLine("UPDATE {0} SET {1}='{2}' WHERE Id='{2}' LIMIT '1'",
								Name, __Property.Name, Provider.GetPropertyValue<uint>(this, __Property.Name));
							break;
						case "uint16":
							writer.WriteLine("UPDATE {0} SET {1}='{2}' WHERE Id='{2}' LIMIT '1'",
								Name, __Property.Name, Provider.GetPropertyValue<ushort>(this, __Property.Name));
							break;
						case "int16":
							writer.WriteLine("UPDATE {0} SET {1}='{2}' WHERE Id='{2}' LIMIT '1'",
								Name, __Property.Name, Provider.GetPropertyValue<short>(this, __Property.Name));
							break;
						case "int32":
							writer.WriteLine("UPDATE {0} SET {1}='{2}' WHERE Id='{2}' LIMIT '1'",
								Name, __Property.Name, Provider.GetPropertyValue<int>(this, __Property.Name));
							break;
						case "int64":
							writer.WriteLine("UPDATE {0} SET {1}='{2}' WHERE Id='{2}' LIMIT '1'",
								Name, __Property.Name, Provider.GetPropertyValue<long>(this, __Property.Name));
							break;
					}
				}

				var SQL_UPDATE_STATEMENT = "";

				using (var reader = new StreamReader(Path.Combine(FileSystem.Root, "database_update.sql"), true))
					SQL_UPDATE_STATEMENT = reader.ReadToEnd();

				Database.SQLInsert(SQL_UPDATE_STATEMENT);

				File.Delete(Path.Combine(FileSystem.Root, "database_update.sql"));
			}
		}

		/// <summary>
		/// Installs this Provider
		/// </summary>
		public void Install(string script)
		{
			if (!Properties.Any())
				return;

			Console.WriteLine("[I] Installer for {0} started... (Script: {1})", Name, script);

			using (var writer = new StreamWriter(Path.Combine(FileSystem.Root, "database_create.sql")))
			{
				writer.WriteLine("CREATE TABLE IF NOT EXISTS '{0}' (", Name);
				writer.NewLine = Environment.NewLine;
				writer.AutoFlush = true;
				writer.WriteLine("\t'_id'\tINTEGER PRIMARY KEY AUTOINCREMENT,");
				var _iterates = 1;

				foreach (var __Property in Properties)
				{
					var _typeName = __Property.PropertyType.ToString();
					var _typeNameparts = _typeName.Split('.');
					_typeName = _typeNameparts[_typeNameparts.Length - 1];

					var _type = "TEXT";

					switch (_typeName.ToLower())
					{
						case "ipaddress":
						case "string":
						case "guid":
							_type = "TEXT";
							break;
						case "double":
						case "boolean":
						case "uint64":
						case "uint32":
						case "uint16":
						case "int16":
						case "int32":
						case "int64":
							_type = "INTEGER";
							break;
						default:
							throw new Exception("Dont know what to write for " + _type);
					}

					if (_iterates != Properties.Count())
						writer.WriteLine("\t'{0}'\t{1},", __Property.Name, _type);
					else
					{
						writer.WriteLine("\t'{0}'\t{1}", __Property.Name, _type);
						break;
					}

					_iterates++;
				}

				writer.WriteLine(")");
			}

			var SQL_CREATE_STATEMENT = "";

			using (var reader = new StreamReader(Path.Combine(FileSystem.Root, "database_create.sql"), true))
				SQL_CREATE_STATEMENT = reader.ReadToEnd();

			Database.SQLInsert(SQL_CREATE_STATEMENT);

		//	File.Delete(Path.Combine(FileSystem.Root, "database_create.sql"));

			Console.WriteLine("[I] done!!");

			Insert();

			lock (Members)
				foreach (var member in Members.Values.Where(x => Provider.HasMethod(x.GetType(), "Install")))
					Provider.InvokeMethod(member, "Install", null);
		}

		public virtual string Request(ref Common common, WebServer.SiteAction action, ref WebServer webserver, Guid userid,
			ref HttpListenerContext context, ref Dictionary<string, string> parameters, bool localRequest = false)
		{
			var error = ERROR_CODES.NOT_HANDLED;
			var result = string.Empty;
			var providerAction = ProviderAction.RequestStarted;
			var _user = userid;

			switch (action)
			{
				case WebServer.SiteAction.Add:

					break;
				case WebServer.SiteAction.Edit:
					result = JsonConvert.SerializeObject("");

					if (!parameters.ContainsKey("Id"))
					{
						error = ERROR_CODES.REQUIRED_ARGS_MISSED;
						break;
					}
					break;
				case WebServer.SiteAction.Remove:
					if (!parameters.ContainsKey("Id"))
					{
						error = ERROR_CODES.REQUIRED_ARGS_MISSED;
						break;
					}

					error = this.Remove(Guid.Parse(parameters["Id"]));
					break;
				case WebServer.SiteAction.Login:


					switch (context.Request.HttpMethod)
					{
						case "POST":
							var user = (I)Convert.ChangeType(null, typeof(I));

							var _uname = string.Empty;
							var _pass = string.Empty;

							if (parameters.ContainsKey("Id"))
							{
								var lid = Guid.Parse(parameters["Id"]);

								user = (from u in Members.Values
										where Provider.GetPropertyValue<Guid>(u, "Id") == lid
										select u).FirstOrDefault();

								error = ERROR_CODES.SUCCESS;
							}
							else
							{
								if (!parameters.ContainsKey("Name") || !parameters.ContainsKey("Password"))
								{
									error = ERROR_CODES.REQUIRED_ARGS_MISSED;
									break;
								}

								_uname = parameters["Name"];
								if (string.IsNullOrEmpty(_uname))
								{
									error = ERROR_CODES.REQUIRED_ARGS_MISSED;
									providerAction = ProviderAction.ProviderLogin;
									break;
								}

								if (string.IsNullOrEmpty(parameters["Password"]))
								{
									error = ERROR_CODES.REQUIRED_ARGS_MISSED;
									providerAction = ProviderAction.ProviderLogin;
									break;
								}

								_pass = MD5.GetMD5Hash(parameters["Password"]);

								user = (from u in Members.Values
										where Provider.GetPropertyValue<string>(u, "Name") == _uname
										where Provider.GetPropertyValue<string>(u, "Password") == _pass
										select u).FirstOrDefault();

								error = ERROR_CODES.REDIRECT_TO_NEW_LOCATION;
							}

							if (user != null)
							{

								Common.UserProvider.Members[Provider.GetPropertyValue<Guid>(user, "Id")].IPAddress
									= context.Request.RemoteEndPoint.Address.ToString();

								Common.UserProvider.Members[Provider.GetPropertyValue<Guid>(user, "Id")].Updated
									= DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

								var _u = Get_Member(Provider.GetPropertyValue<Guid>(user, "Id"));
								Provider.SetPropertyValue<string>(_u, "Password", null);

								result = JsonConvert.SerializeObject(_u);

								var loginCookie = new Cookie("User", Provider.
									GetPropertyValue<Guid>(_u, "Id").ToString(), "/")
								{
									Expired = false,
									Expires = DateTime.Now.AddHours(2),
									Discard = false,
									Comment = "Contains the UserID for this Session. This Cookie is required!"
								};

								context.Response.SetCookie(loginCookie);

							}
							else
								error = ERROR_CODES.NOTFOUND;

							providerAction = ProviderAction.ProviderLogin;
							break;
						case "GET":


							var frame = "#content";
							if (context.Request.Headers["Frame"] != null)
							{
								frame = context.Request.Headers["Frame"];
							}

							if (Members.ContainsKey(_user))
							{
								var user_result = Get_Member(_user);
								if (user_result != null)
								{
									Provider.SetPropertyValue<string>(user_result, "Password", null);
									result = JsonConvert.SerializeObject(user_result);
									providerAction = ProviderAction.RequestProceeded;
								}
							}
							else
							{
								var form = new Form(Name, action, frame, string.Format("{0}-{1}", Name, action), string.Format("{0}-{1}", Name, action),
																string.Format("/providers/{0}/", Name.ToLower()), "POST");

								form.Controls.Add(new Control(ControlType.Hidden, "Format", "Format", "0"));

								form.Controls.Add(new Control(ControlType.Hidden, "Frame", "Frame", frame));

								form.Controls.Add(new Control(ControlType.Hidden, "Provider", "Provider", Name));

								form.Controls.Add(new Control(ControlType.Hidden, "Action", "Action", string.Format("{0}", action)));

								form.Controls.Add(new Control("Name", ControlType.Label, "Name", "Benutzername:"));

								form.Controls.Add(new Control(ControlType.Input, "Name", "Name"));

								form.Controls.Add(new Control("Password", ControlType.Label, "Password", "Password:"));

								form.Controls.Add(new Control(ControlType.Password, "Password", "Password"));

								form.Controls.Add(new Control(ControlType.Button, "Submit", "Submit"));
								result = JsonConvert.SerializeObject(form);
								providerAction = ProviderAction.RequestProceeded;
							}

							error = ERROR_CODES.SUCCESS;
							break;
					}
					break;
				case WebServer.SiteAction.Logout:
					var cookie = new Cookie("User", Guid.Empty.ToString(), "/")
					{
						Expired = true,
						Expires = DateTime.Now.AddDays(-1),
						Discard = true
					};

					context.Response.SetCookie(cookie);
					error = ERROR_CODES.SUCCESS;
					providerAction = ProviderAction.ProviderLogout;
					break;
				case WebServer.SiteAction.Show:
					var level = 0UL;

					if (Name == "Navigation")
					{
						if (_user != Guid.Empty)
						{
							level = Common.UserProvider.Get_Member(_user).Level;
						}
					}

					var me = Members.Values
						.Where(m => !Provider.GetPropertyValue<bool>(m, "Service"))
							.Where(m => Provider.GetPropertyValue<bool>(m, "Active"))
								.Where(m => !Provider.GetPropertyValue<bool>(m, "Locked"))
									.Where(m => Provider.GetPropertyValue<byte>(m, "Level") <= level);

					foreach (var me_result in me)
					{
						Provider.SetPropertyValue(me_result, "Password", string.Empty);

						if (Name == "News")
						{
							var authorName = Common.UserProvider.Get_Member(Provider.GetPropertyValue<Guid>(me_result, "Author")).Name;

							if (!string.IsNullOrEmpty(authorName))
							{
								var authorID = Provider.GetPropertyValue<Guid>(me_result, "Author").ToString();

								Provider.SetPropertyValue(me_result, "ExtraData", string.Format("replace;{0}=>{1}", authorID, authorName));
							}
						}

						if (Common.AccessProvider.Contains(Provider.GetPropertyValue<Guid>(me_result, "Group")))
						{
							var _groupName = Common.AccessProvider.Get_Member(Provider.GetPropertyValue<Guid>(me_result, "Group")).Name;

							if (!string.IsNullOrEmpty(_groupName))
							{
								var groupID = Provider.GetPropertyValue<Guid>(me_result, "Group").ToString();

								Provider.SetPropertyValue(me_result, "ExtraData", string.Format("replace;{0}=>{1}", groupID, _groupName), true);
							}
						}

					}

					result = JsonConvert.SerializeObject(me);
					providerAction = ProviderAction.RequestProceeded;
					error = ERROR_CODES.SUCCESS;
					break;
				case WebServer.SiteAction.MetaData:
					if (!parameters.ContainsKey("Id"))
					{
						error = ERROR_CODES.REQUIRED_ARGS_MISSED;
						break;
					}

					error = ERROR_CODES.SUCCESS;
					break;
				case WebServer.SiteAction.Teamlist:

					var me_teamlist = Members.Values
						.Where(m => Provider.GetPropertyValue<bool>(m, "Moderator"))
							.Where(m => !Provider.GetPropertyValue<bool>(m, "Service"))
								.Where(m => Provider.GetPropertyValue<bool>(m, "Active"))
									.Where(m => !Provider.GetPropertyValue<bool>(m, "Locked"));

					foreach (var me_result in me_teamlist)
					{
						Provider.SetPropertyValue(me_result, "Password", string.Empty);
						Provider.SetPropertyValue(me_result, "Provider", Name);
					}

					result = JsonConvert.SerializeObject(me_teamlist);

					error = ERROR_CODES.SUCCESS;
					break;
				case WebServer.SiteAction.Clients:
					break;
				case WebServer.SiteAction.Profile:

					if (!parameters.ContainsKey("Id"))
					{
						error = ERROR_CODES.REQUIRED_ARGS_MISSED;
						break;
					}

					var id = Guid.Parse(parameters["Id"]);
					if (!Members.ContainsKey(id))
					{
						error = ERROR_CODES.NOTFOUND;
						break;
					}

					var me_profile = Get_Member(id);

					Provider.SetPropertyValue(me_profile, "Password", string.Empty);
					Provider.SetPropertyValue(me_profile, "Provider", Name);

					result = JsonConvert.SerializeObject(me_profile);
					error = ERROR_CODES.SUCCESS;
					break;
				case WebServer.SiteAction.Register:
					if (!parameters.ContainsKey("Id"))
					{
						error = ERROR_CODES.REQUIRED_ARGS_MISSED;
						break;
					}

					error = ERROR_CODES.SUCCESS;
					break;
				case WebServer.SiteAction.Execute:
					if (!parameters.ContainsKey("Id"))
					{
						error = ERROR_CODES.REQUIRED_ARGS_MISSED;
						break;
					}

					error = ERROR_CODES.SUCCESS;
					break;
			}

			if (!localRequest)
				using (var evArgs = new ProviderEventArgs<Guid>(providerAction,
					error, _user, parameters, result, webserver, ref context))
					Common.NavigationProvider.ProviderResponse?.Invoke(this, evArgs);

			return result;
		}

		/// <summary>
		/// Returns the specific Member Object from this Provider
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public I Get_Member(Guid id)
			=> Members[id];

		/// <summary>
		/// Gets the Number of Key/Value pairs in this Dictionary. 
		/// </summary>
		public int Count => Members.Count;

		/// <summary>
		/// Returns the Database for this Provider.
		/// </summary>
		public SQLDatabase<uint> Database { get; set; }

		/// <summary>
		/// Returns the attached Filesystem Instance for this Provider.
		/// </summary>
		public Filesystem FileSystem { get; set; }

		/// <summary>
		/// Returns all Member Instances from this Provider.
		/// </summary>
		public Dictionary<Guid, I> Members { get; set; }


	}
}
