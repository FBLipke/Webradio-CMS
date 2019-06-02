using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

using Namiono;
using System.Collections.Specialized;

public sealed class Sendeplan<T> : Provider<T, SendeplanEntry<T>>, IDisposable
{
    SQLDatabase<T> db;
    Users<T> users;
    Sp_settings settings;
    Filesystem fs;
    Guid spident;
    string name;

    List<SP_Week<T>> weeks;

    public void _Read_SP_From_DataBase(int start_day)
    {
        var ts_curweek = GetMonday().AddDays(start_day).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        var ts_nextweek = GetMonday().AddDays((start_day + 7)).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

        var curweekdays = db.SQLQuery<T>(string.Format("SELECT * FROM Sendeplan WHERE timestamp >= '{0}' AND timestamp < '{1}'",
           ts_curweek, ts_nextweek));


        var week = new SP_Week<T>(spident, ts_curweek);

        Dictionary<int, SP_Day<T>> _d = new Dictionary<int, SP_Day<T>>();
        foreach (var d in curweekdays.Values)
        {
            var __day = int.Parse(d["day"]);
            var _entry = new SendeplanEntry<T>(0, spident, d["description"], "",
                (T)Convert.ChangeType(d["uid"], typeof(T)), int.Parse(d["uid"]), int.Parse(d["hour"]));

            if (!_d.ContainsKey(int.Parse(d["day"])))
            {
                var _x = new SP_Day<T>(spident, __day, GetMonday().AddDays(__day)
                 .Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                _x.Add_Entry(_entry);
            }
            else
            {
                _d[__day].Add_Entry(_entry);
            }
        }

        foreach (var item in _d.Values)
            week.Add_Day(item);

        weeks.Add(week);
    }

    public Sendeplan(Guid guid, string name, ref SQLDatabase<T> db,
        ref Filesystem fs, ref Users<T> users, ref Sp_settings settings)
    {
        this.settings = settings;
        this.name = name;
        this.spident = guid;
        this.fs = fs;
        this.users = users;
        this.db = db;
        this.weeks = new List<SP_Week<T>>();


    }

    public void Start()
    {
        _Read_SP_From_DataBase(0);
        _Read_SP_From_DataBase(7);
    }

    public static DateTime GetMonday()
    {
        var today = (int)DateTime.Now.DayOfWeek;
        return DateTime.Today.AddDays((today == 0) ? -6 : -today + (int)DayOfWeek.Monday);
    }

    int GetEntryCount(int day, double timestamp, byte start, byte end)
    {
        var entryCount = db.SQLQuery<T>(string.Format("SELECT * FROM sendeplan WHERE day='{0}' AND timestamp >='{1}' AND hour >='{2}' AND hour <='{3}' AND spident ='{4}'",
            day, timestamp, start, end, this.spident));

        var entryCount2 = db.SQLQuery<T>(string.Format("SELECT * FROM sendeplan_replay WHERE day='{0}' AND hour >='{1}' AND hour <='{2}' AND spident ='{3}'",
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

        for (var i_time = settings.Start; i_time <= settings.End; i_time++)
            entries += GetSPEntry(ref curDay, day, i_time, timestamp + (i_time * 3600 * 86400), timestamp);

        var entry_counts = GetEntryCount(day, timestamp, settings.Start, settings.End);
        var addLink = string.Empty;

        if ((users.CanAddSendeplan(userid) && users.GetModerators().Count != 0)
            && entry_counts != (settings.End - settings.Start))
        {
            addLink = string.Format("<div class=\"sp_row\"><a href=\"#\" onclick=\"LoadDocument('/providers/sendeplan/'," +
          "'#content','Sendeplan','add','day={0}&spident={1}', '')\"><p class=\"exclaim\">Eintragen</p></a></div>", day, spident);

            tpl += addLink;
        }

        tpl = tpl.Replace("[#SP_ENTRIES#]", entries);

        return tpl;
    }

    public string Name => name;



    string Get_Week(int week)
    {
        return JsonConvert.SerializeObject(weeks);
    }

    public string GetCurWeekPlan(T userid)
    {
        string output = string.Empty;
        output += Get_Week(0);
        output += Get_Week(1);
        return output;
    }

    public string GetSendeBanner()
    {
        var banners = new List<string>();

        banners.Add("<img src=\"uploads/events/here_could.png\" />");
        for (var i = 0; i < 14; i++)
        {
            var d_ts = GetMonday().AddDays(i).Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            var dbEntry = db.SQLQuery<T>(string.Format("SELECT banner FROM sendeplan WHERE spident='{0}' AND timestamp >= '{1}'", spident, d_ts));

            if (dbEntry.Count == 0)
                continue;

            for (var ib = ulong.MinValue; ib < (ulong)dbEntry.Count; ib++)
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
        => db.SQLQuery<T>(string.Format("SELECT * FROM sendeplan_replay WHERE day='{0}'" +
            " AND hour='{1}' AND spident='{2}'", day, hour, spident));

    Dictionary<T, NameValueCollection> Get_Entries(int day, byte hour, double ts, double d_ts)
        => db.SQLQuery<T>(string.Format("SELECT * FROM sendeplan WHERE timestamp >='{0}'" +
            " AND day='{1}' AND hour='{2}' AND timestamp < '{3}' AND spident='{4}'",
                d_ts, day, hour, ts, spident));

    string enumEntry(Dictionary<T, NameValueCollection> collection, int day, byte hour)
    {
        var entry = Dispatcher.ReadTemplate(ref fs, "sp_entry");

        for (var i = ulong.MinValue; i < (ulong)collection.Count; i++)
        {
            var _i = (T)Convert.ChangeType(i, typeof(T));

            var uid = (T)Convert.ChangeType(ulong.Parse(collection[_i]["uid"]), typeof(T));

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
        var uid = (T)Convert.ChangeType(db.SQLQuery("SELECT id FROM users WHERE service_profile='1'" +
            " AND moderator='1' LIMIT '1'", "id"), typeof(T));

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
        var sp_entry = db.SQLQuery<T>(string.Format("SELECT * FROM sendeplan WHERE day='{0}'" +
            " AND hour='{1}' AND timestamp='{2}' AND spident='{3}' LIMIT '1'",
            day, hour, timestamp, this.spident));

        var output = string.Empty;

        if (sp_entry.Count == 0)
            return output;

        var tpl = Dispatcher.ReadTemplate(ref fs, "sp_form_edit");
        for (var i = ulong.MinValue; i < (ulong)sp_entry.Count; i++)
        {
            var _i = (T)Convert.ChangeType(i, typeof(T));
            var mods = users.GetModerators();
            if (mods.Count != 0)
            {
                var modEntries = string.Empty;
                for (var i_mod = ulong.MinValue; i_mod < (ulong)mods.Count; i_mod++)
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
            addFormTpl = addFormTpl.Replace("[#TOPIC#]", settings.Topic);
            addFormTpl = addFormTpl.Replace("[#SPIDENT#]", spident.ToString());

            var hours = db.SQLQuery<T>(string.Format("SELECT hour FROM sendeplan WHERE day='{0}'" +
                " AND timestamp >'{1}' and spident='{2}'", day, day_timestamp, this.spident));
            if (hours.Count == 0)
            {
                var hours_replay = db.SQLQuery<uint>(string.Format("SELECT hour FROM sendeplan_replay WHERE day='{0}'", day));
                if (hours_replay.Count == 0)
                {
                    var hourEntries = string.Empty;
                    for (var i_entry = settings.Start; i_entry <= settings.End; i_entry++)
                    {
                        var replays = db.SQLQuery<T>(string.Format("SELECT hour FROM sendeplan_replay WHERE day='{0}' AND hour='{1}'", day, i_entry));
                        if (replays.Count == 0)
                            hourEntries += string.Format("\t<option value=\"{0}\">{0}:00</option>\n", i_entry);
                    }

                    addFormTpl = addFormTpl.Replace("[#SP_HOURS#]", hourEntries);
                }
            }
            else
            {
                var hourEntries = string.Empty;
                for (var i_entry = settings.Start; i_entry <= settings.End; i_entry++)
                {
                    var hCount = db.SQLQuery<T>(string.Format("SELECT * FROM sendeplan WHERE day='{0}' AND hour=\"{1}\" AND timestamp > '{2}' AND spident='{3}'",
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
                for (var i_mod = ulong.MinValue; i_mod < (ulong)mods.Count; i_mod++)
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
        var users = db.SQLQuery<T>("SELECT id FROM users WHERE moderator='0'");
        var result = false;

        for (var i = ulong.MinValue; i < (ulong)users.Count; i++)
        {
            var _i = (T)Convert.ChangeType(i, typeof(T));
            if (db.Count("sendeplan", "uid", (T)Convert.ChangeType(users[_i]["id"], typeof(T))) != 0)
                result = db.SQLInsert(string.Format("DELETE FROM sendeplan WHERE uid='{0}' AND spident='{1}'", users[_i]["id"], this.spident));

            if (db.Count("sendeplan_replay", "uid", (T)Convert.ChangeType(users[_i]["id"], typeof(T))) != 0)
                result = db.SQLInsert(string.Format("DELETE FROM sendeplan_replay WHERE uid='{0}' AND spident='{1}'", users[_i]["id"], this.spident));
        }

        if ((int)DateTime.Now.DayOfWeek == 1 && DateTime.Now.Hour == 1 && DateTime.Now.Minute < 2)
        {
            var ts = GetMonday().Subtract(new DateTime(1970, 1, 1)).TotalSeconds + settings.Start * 3600;
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
        var t = db.SQLQuery<uint>(string.Format("SELECT * FROm sendeplan WHERE day='{0}' AND hour='{1}'" +
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
            string.IsNullOrEmpty(description) ? settings.Topic : description, user, ts, spident));
        }

        return tmp;
    }

    public void Heartbeat() => CleanupDatabase();

    public void Close()
    {
    }

    public void Dispose() => settings.Dispose();

    public Sp_settings Settings => settings;

    public override Dictionary<T, SendeplanEntry<T>> Members => members;
}
