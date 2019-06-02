using System;
using System.Collections.Generic;
using System.Globalization;

using Namiono;
using System.Net;
using System.Collections.Specialized;
using System.Threading.Tasks;

public sealed partial class Sendeplan<T> : Provider<SendeplanEntry<T>>, IDisposable
{
	/*
	static SQLDatabase<T> db;
	//Users<T> users;
    Filesystem fs;
	Guid spident;
	string name;

	public Sendeplan(Guid guid, string name, ref Filesystem fs, ref Users<T> users, ref Sp_settings settings)
	{
		this.Settings = settings;
		this.name = name;
		this.spident = guid;
		this.fs = fs;
		//this.users = users;
        db = new SQLDatabase<T>("Sendeplan.db");
        var createDB_Ident = "CREATE TABLE IF NOT EXISTS `sendeplan_ident` (" +
            "`id`	INTEGER PRIMARY KEY AUTOINCREMENT," +
            "`spident`	TEXT NOT NULL)";

        db.SQLInsert(createDB_Ident);

        var sp_insert = string.Format("INSERT INTO 'sendeplan_ident' ('spident') VALUES('{0}')", guid);
        db.SQLInsert(sp_insert);

        var createDB = string.Format("CREATE TABLE IF NOT EXISTS 'sendeplan' (" +
            "`id`	INTEGER PRIMARY KEY AUTOINCREMENT," +
            "`day`	INTEGER," +
            "`hour`	INTEGER DEFAULT 18," +
            "`description`	TEXT DEFAULT 'Querbeet'," +
            "`uid`	INTEGER," +
            "`timestamp`	INTEGER," +
            "`banner`	TEXT," +
            "`spident`	TEXT DEFAULT '{0}')", guid);

        db.SQLInsert(createDB);

        var createDB_Replay = "CREATE TABLE IF NOT EXISTS 'sendeplan_replay' (" +
	        "`id`	INTEGER PRIMARY KEY AUTOINCREMENT," +
	        "`day`	TEXT, " +
	        "`hour`	TEXT, " +
	        "`uid`	TEXT, " +
	        "`description`	TEXT DEFAULT 'Querbeet', " +
	        "`spident`	TEXT)";

        db.SQLInsert(createDB_Replay);
    }

    public static void LoadSendePlan(ref Dictionary<Guid, Sendeplan<T>> splist, ref Filesystem fs, ref Users<T> users)
    {
        try
        {
            var sp_sendeplaene = db.SQLQuery("SELECT spident FROM sendeplan_ident");
            if (sp_sendeplaene.Count == 0)
                return;

            lock (splist)
            {
                for (var i = uint.MinValue; i < sp_sendeplaene.Count; i++)
                {
                    var _i = (T)Convert.ChangeType(i, typeof(T));
                    var spid = sp_sendeplaene[_i]["spident"];
                    if (string.IsNullOrEmpty(spid))
                        continue;

                    var guid = Guid.Parse(spid);

                    if (splist.ContainsKey(guid))
                        continue;

                    var settings = new Sendeplan<T>.Sp_settings();
                    splist.Add(guid, new Sendeplan<T>(guid, "Sendeplan", ref fs, ref users, ref settings));
                }
            }
        }
        catch (Exception)
        {
            var settings = new Sendeplan<T>.Sp_settings();
            var _guid = Guid.NewGuid();
            splist.Add(_guid, new Sendeplan<T>(_guid, "Sendeplan", ref fs, ref users, ref settings));
        }
        
    }
    public static DateTime GetMonday()
	{
		var today = (int)DateTime.Now.DayOfWeek;
		return DateTime.Today.AddDays((today == 0) ? -6 : -today + (int)DayOfWeek.Monday);
	}

	int GetEntryCount(int day, double timestamp, byte start, byte end)
	{
		var entryCount = db.SQLQuery(string.Format("SELECT * FROM sendeplan WHERE day='{0}' AND timestamp >='{1}' AND hour >='{2}' AND hour <='{3}' AND spident ='{4}'",
			day, timestamp, start, end, this.spident));

		var entryCount2 = db.SQLQuery(string.Format("SELECT * FROM sendeplan_replay WHERE day='{0}' AND hour >='{1}' AND hour <='{2}' AND spident ='{3}'",
		day, start, end, this.spident));

		return entryCount.Count + entryCount2.Count;
	}

	string getSPDay(byte day, ref T userid)
	{
		var curDay = GetMonday().AddDays(day);
		var timestamp = curDay.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
		var tpl = Dispatcher.ReadTemplate(ref fs, "sp_day");

		tpl = tpl.Replace("[#DAY#]", string.Format("{0}", day));
		tpl = tpl.Replace("[#SDATE#]", curDay.ToShortDateString());
		tpl = tpl.Replace("[#DAYNAME#]", curDay.ToString("dddd", new CultureInfo("de-DE")));

		var entries = string.Empty;

		for (var i_time = Settings.Start; i_time <= Settings.End; i_time++)
			entries += GetSPEntry(ref curDay, day, i_time, timestamp + (i_time * 3600 * 86400), timestamp);

		var entry_counts = GetEntryCount(day, timestamp, Settings.Start, Settings.End);
		var addLink = string.Empty;

		if ((users.CanAddSendeplan(userid) && users.GetModerators().Count != 0)
			&& entry_counts != (Settings.End - Settings.Start))
		{
			addLink = string.Format("<div class=\"sp_row\"><a href=\"#\" onclick=\"LoadDocument('/providers/sendeplan/'," +
		  "'#content','Sendeplan','add','day={0}&spident={1}', '')\"><p class=\"exclaim\">Eintragen</p></a></div>", day, spident);

			tpl += addLink;
		}

		tpl = tpl.Replace("[#SP_ENTRIES#]", entries);

		return tpl;
	}

	public string GetCurWeekPlan(T userid)
	{
		var cw_tmp = string.Empty;
		var nw_tmp = string.Empty;

		for (var i_day = byte.MinValue; i_day < 7; i_day++)
			cw_tmp += getSPDay(i_day, ref userid);

		var cw_box = Dispatcher.ReadTemplate(ref fs, "content-box")
		.Replace("[[box-content]]", cw_tmp)
		.Replace("[[content]]", "sendeplan")
		.Replace("[[box-title]]", this.name);

		for (var i_day = (byte)7; i_day < 14; i_day++)
			nw_tmp += getSPDay(i_day, ref userid);

		var nw_box = Dispatcher.ReadTemplate(ref fs, "content-box")
				.Replace("[[box-content]]", nw_tmp)
				.Replace("[[content]]", "sendeplan")
				.Replace("[[box-title]]", string.Format("{0} - Vorschau", name));

		return cw_box += nw_box;
	}

	public string GetSendeBanner()
	{
		var banners = new List<string>();

		for (var i = 0; i < 14; i++)
		{
			var d_ts = GetMonday().AddDays(i).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
			var dbEntry = db.SQLQuery(string.Format("SELECT banner FROM sendeplan WHERE spident = '{0}'", spident));

			if (dbEntry.Count == 0)
				continue;

            for (var ib = uint.MinValue; ib < dbEntry.Count; ib++)
            {
                var _ib = (T)Convert.ChangeType(ib, typeof(T));
                if (!string.IsNullOrEmpty(dbEntry[_ib]["banner"]))
                    if (fs.Exists(Filesystem.Combine("uploads/events/", dbEntry[_ib]["banner"])))
                        banners.Add(string.Format("<img src=\"uploads/events/{0}\" />", dbEntry[_ib]["banner"]));
            }
        }

		return banners.Count != 0 ? banners[new Random().Next(0, banners.Count)] : string.Empty;
	}

	Dictionary<T, NameValueCollection> Get_Replay_Entries(int day, byte hour)
		=> db.SQLQuery(string.Format("SELECT * FROM sendeplan_replay WHERE day='{0}'" +
			" AND hour='{1}' AND spident='{2}'", day, hour, spident));

	Dictionary<T, NameValueCollection> Get_Entries(int day, byte hour, double ts, double d_ts)
		=> db.SQLQuery(string.Format("SELECT * FROM sendeplan WHERE timestamp >='{0}'" +
			" AND day='{1}' AND hour='{2}' AND timestamp < '{3}' AND spident='{4}'",
				d_ts, day, hour, ts, spident));

	string enumEntry(Dictionary<T, NameValueCollection> collection, int day, byte hour)
	{
		var entry = Dispatcher.ReadTemplate(ref fs, "sp_entry");

		for (var i = uint.MinValue; i < collection.Count; i++)
		{
            var _i = (T)Convert.ChangeType(i, typeof(T));
            var uid = (T)Convert.ChangeType(uint.Parse(collection[_i]["uid"]), typeof(T));

			if (!users.UserExists(uid))
				continue;

			entry = entry.Replace("[#SPID#]", collection[_i]["id"]);
			entry = entry.Replace("[#TS#]", collection[_i]["timestamp"]);
			entry = entry.Replace("[#AVATAR#]", users.GetUserAvatarbyID(uid));

			var topic = (collection[_i]["description"].Length > 32) ?
				collection[_i]["description"].Substring(0, 31) : collection[_i]["description"];

			entry = entry.Replace("[#TOPIC#]", topic);
			entry = entry.Replace("[#MOD#]", users.GetUserNamebyID(uid));
			entry = entry.Replace("[#UID#]", string.Format("{0}", uid));
			entry = entry.Replace("[#SPIDENT#]", string.Format("{0}", spident));

			entry = entry.Replace("[#DAY#]", string.Format("{0}", day));
			entry = entry.Replace("[#HOUR#]", string.Format("{0}", hour));
		}

		return entry;
	}

	string GetSPEntry(ref DateTime week, int day, byte hour, double time, double d_ts)
	{
		var entry = string.Empty;

		var ts = GetMonday().AddDays(day).Subtract(new DateTime(1970, 1, 1)).TotalSeconds + 86400;
        var uid = users.Get_ServiceProfile();

        var dbEntry = Get_Entries(day, hour, ts, d_ts);
		if (dbEntry.Count != 0)
		{
			entry = enumEntry(dbEntry, day, hour);
			return entry;
		}

		var dbEntry_replay = Get_Replay_Entries(day, hour);
		if (dbEntry_replay.Count != 0)
			entry = enumEntry(dbEntry_replay, day, hour);
		else
		{
			entry = Dispatcher.ReadTemplate(ref fs, "sp_entry");
			entry = entry.Replace("[#TOPIC#]", "Playlist");
			entry = entry.Replace("[#MOD#]", users.GetUserNamebyID(uid));
			entry = entry.Replace("[#UID#]", string.Format("{0}", uid));
			entry = entry.Replace("[#AVATAR#]", users.GetUserAvatarbyID(uid));
		}

		entry = entry.Replace("[#DAY#]", string.Format("{0}", day));
		entry = entry.Replace("[#HOUR#]", string.Format("{0}", hour));
		entry = entry.Replace("[#TS#]", string.Format("{0}", d_ts));
		entry = entry.Replace("[#SPID#]", string.Format("{0}", 0));
		entry = entry.Replace("[#SPIDENT#]", string.Format("{0}", spident));

		return entry;
	}

	public string Edit_Form(int day, byte hour, double timestamp, T userid)
	{
		var sp_entry = db.SQLQuery(string.Format("SELECT * FROM sendeplan WHERE day='{0}'" +
			" AND hour='{1}' AND timestamp='{2}' AND spident='{3}' LIMIT '1'",
			day, hour, timestamp, this.spident));

		var output = string.Empty;

		if (sp_entry.Count == 0)
			return output;

		var tpl = Dispatcher.ReadTemplate(ref fs, "sp_form_edit");
		for (var i = uint.MinValue; i < sp_entry.Count; i++)
		{
            var _i = (T)Convert.ChangeType(i, typeof(T));
            var mods = users.GetModerators();
			if (mods.Count != 0)
			{
				var modEntries = string.Empty;
				for (var i_mod = uint.MinValue; i_mod < mods.Count; i_mod++)
				{
					var t = (T)Convert.ChangeType(i_mod, typeof(T));
					modEntries += string.Format("\t<option value=\"{0}\">{1}</option>",
						mods[t]["id"], mods[t]["username"]);
				}

				tpl = tpl.Replace("[#SP_MODS#]", modEntries);
			}

			tpl = tpl.Replace("[#DAY#]", sp_entry[_i]["day"]);
			tpl = tpl.Replace("[#UID#]", sp_entry[_i]["uid"]);
			tpl = tpl.Replace("[#TS#]", sp_entry[_i]["timestamp"]);
			tpl = tpl.Replace("[#HOUR#]", sp_entry[_i]["hour"]);
			tpl = tpl.Replace("[#TOPIC#]", sp_entry[_i]["description"]);
			tpl = tpl.Replace("[#SPIDENT#]", spident.ToString());

			output = tpl;
		}

		output = output.Replace("[[box-content]]", tpl).Replace("[[box-title]]",
			"Sendung bearbeiten").Replace("[[content]]", "sendeplan-edit");
		return output;
	}

	public string Add_Form(int day, T userid)
	{
		var week = GetMonday().AddDays(day);
		var day_timestamp = week.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

		var tpl = Dispatcher.ReadTemplate(ref fs, "content_box");
		var title = string.Format("Sendung für {0} eintragen...", week.ToString("dddd", new CultureInfo("de-DE")));
		var addFormTpl = string.Empty;

		if (users.CanAddSendeplan(userid))
		{
			addFormTpl = Dispatcher.ReadTemplate(ref fs, "sp_form_add");

			addFormTpl = addFormTpl.Replace("[#DAY#]", string.Format("{0}", day));
			addFormTpl = addFormTpl.Replace("[#TS#]", string.Format("{0}", day_timestamp));
			addFormTpl = addFormTpl.Replace("[#UID#]", string.Format("{0}", userid));
			addFormTpl = addFormTpl.Replace("[#TOPIC#]", Settings.Topic);
			addFormTpl = addFormTpl.Replace("[#SPIDENT#]", spident.ToString());

			var hours = db.SQLQuery(string.Format("SELECT hour FROM sendeplan WHERE day='{0}'" +
				" AND timestamp >'{1}' and spident='{2}'", day, day_timestamp, this.spident));
			if (hours.Count == 0)
			{
				var hours_replay = db.SQLQuery(string.Format("SELECT hour FROM sendeplan_replay WHERE day='{0}'", day));
				if (hours_replay.Count == 0)
				{
					var hourEntries = string.Empty;
					for (var i_entry = Settings.Start; i_entry <= Settings.End; i_entry++)
					{
						var replays = db.SQLQuery(string.Format("SELECT hour FROM sendeplan_replay WHERE day='{0}' AND hour='{1}'", day, i_entry));
						if (replays.Count == 0)
							hourEntries += string.Format("\t<option value=\"{0}\">{0}:00</option>\n", i_entry);
					}

					addFormTpl = addFormTpl.Replace("[#SP_HOURS#]", hourEntries);
				}
			}
			else
			{
				var hourEntries = string.Empty;
				for (var i_entry = Settings.Start; i_entry <= Settings.End; i_entry++)
				{
					var hCount = db.SQLQuery(string.Format("SELECT * FROM sendeplan WHERE day='{0}' AND hour=\"{1}\" AND timestamp > '{2}' AND spident='{3}'",
						day, i_entry, day_timestamp, this.spident));

					if (hCount.Count == 0)
						hourEntries += string.Format("\t<option value=\"{0}\">{0}:00</option>\n", i_entry);
				}

				addFormTpl = addFormTpl.Replace("[#SP_HOURS#]", hourEntries);
			}

			var mods = users.GetModerators();
			if (mods.Count != 0)
			{
				var modEntries = string.Empty;
				for (var i_mod = uint.MinValue; i_mod < mods.Count; i_mod++)
				{
					var t = (T)Convert.ChangeType(i_mod, typeof(T));
					modEntries += string.Format("\t<option value=\"{0}\">{1}</option>",
						mods[t]["id"], mods[t]["username"]);
				}

				addFormTpl = addFormTpl.Replace("[#SP_MODS#]", modEntries);
			}
			else
				addFormTpl = "Keine Moderatoren gefunden!";
		}
		else
			addFormTpl = "Keine Berechtigung!";

		return tpl.Replace("[[box-content]]", addFormTpl).Replace("[[box-title]]", title).Replace("[[content]]", "sendeplan-add");
	}

	public bool CleanupDatabase()
	{
		var users = db.SQLQuery("SELECT id FROM users WHERE moderator='0'");
		var result = false;

		for (var i = uint.MinValue; i < users.Count; i++)
		{
            var _i = (T)Convert.ChangeType(i, typeof(T));

            if (db.Count("sendeplan", "uid", (T)Convert.ChangeType(users[_i]["id"], typeof(T))) != 0)
				result = db.SQLInsert(string.Format("DELETE FROM sendeplan WHERE uid='{0}' AND spident='{1}'", users[_i]["id"], this.spident));

			if (db.Count("sendeplan_replay", "uid", (T)Convert.ChangeType(users[_i]["id"], typeof(T))) != 0)
				result = db.SQLInsert(string.Format("DELETE FROM sendeplan_replay WHERE uid='{0}' AND spident='{1}'", users[_i]["id"], this.spident));
		}

		if ((int)DateTime.Now.DayOfWeek == 1 && DateTime.Now.Hour == 1 && DateTime.Now.Minute < 2)
		{
			var ts = GetMonday().Subtract(new DateTime(1970, 1, 1)).TotalSeconds + Settings.Start * 3600;
			result = db.SQLInsert(string.Format("DELETE FROM sendeplan WHERE timestamp < '{0}' AND spident='{1}'", ts, spident));
		}

		return result;
	}

	public bool DeleteSPEntry(double day, byte hour, double timestamp)
	{
		var result = false;

		if (db.Count("sendeplan", "timestamp", (T)Convert.ChangeType(string.Format("{0}", timestamp), typeof(T))) != 0)
		{
			result = db.SQLInsert(string.Format("DELETE FROM sendeplan WHERE day='{0}'" +
				" AND hour='{1}' AND timestamp='{2}' AND spident='{3}'",
			  day, hour, timestamp, spident));
		}

		return result;
	}

	public bool Update_SPEntry(string day, string hour, string timestamp, string user, string description, T userid)
	{
		var res = false;
		if (users.CanEditSendeplan(userid) && users.IsModerator((T)Convert.ChangeType(user, typeof(T))))
		{
			res = db.SQLInsert(string.Format("UPDATE sendeplan SET day='{0}' WHERE timestamp='{1}'" +
				" AND spident='{2}'", day, timestamp, spident));
			res = db.SQLInsert(string.Format("UPDATE sendeplan SET hour='{0}' WHERE timestamp='{1}'" +
				" AND spident='{2}'", hour, timestamp, spident));
			res = db.SQLInsert(string.Format("UPDATE sendeplan SET uid='{0}' WHERE timestamp='{1}'" +
				" AND spident='{2}'", user, timestamp, spident));
			res = db.SQLInsert(string.Format("UPDATE sendeplan SET description='{0}' WHERE timestamp='{1}'" +
				" AND spident='{2}'", description, timestamp, spident));
		}

		return res;
	}

	public bool Insert_SPEntry(string day, string hour, string timestamp, string user, string description, T userid)
	{
		var ts = double.Parse(timestamp) + double.Parse(hour) * 3600;
		var t = db.SQLQuery(string.Format("SELECT * FROm sendeplan WHERE day='{0}' AND hour='{1}'" +
			" AND timestamp >='{2}' AND spident='{3}'",
			day, hour, ts, this.spident));

		var tmp = false;

		if (t.Count != 0)
			return tmp;

		var ismod = db.SQLQuery(string.Format("SELECT moderator FROM users WHERE id='{0}' LIMIT '1'", user), "moderator");
		if (ismod == "1")
		{
			tmp = db.SQLInsert(string.Format("INSERT INTO sendeplan (day,hour,description,uid,timestamp,spident,banner)" +
			"VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','')", day, hour,
			string.IsNullOrEmpty(description) ? Settings.Topic : description, user, ts, spident));
		}

		return tmp;
	}

    public Sp_settings Settings { get; }
	*/
}
