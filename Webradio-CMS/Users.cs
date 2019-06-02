using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Namiono
{
    public sealed class _Users<T> : Provider<UserEntry<T>>
    {
        SQLDatabase<T> db;

        public _Users()
        {
            this.db = new SQLDatabase<T>("Users.db");
            var createDB = "CREATE TABLE IF NOT EXISTS 'users' (" +
                "`id`	INTEGER PRIMARY KEY AUTOINCREMENT," +
                "`username`	TEXT NOT NULL," +
                "`password`	TEXT NOT NULL," +
                "`usergroup`	INTEGER DEFAULT 8," +
                "`moderator`	INTEGER DEFAULT 0," +
                "`locked`	INTEGER DEFAULT 0," +
                "`avatar`	TEXT DEFAULT ''," +
                "`can_delete_user`	INTEGER DEFAULT 0," +
                "`can_edit_user`	INTEGER DEFAULT 0," +
                "`can_add_sp`	INTEGER DEFAULT 0," +
                "`can_del_sp`	INTEGER DEFAULT 0," +
                "`can_edit_sp`	INTEGER DEFAULT 0," +
                "`can_see_sc_stats`	INTEGER DEFAULT 0," +
                "`can_create_user`	INTEGER DEFAULT 0," +
                "`can_see_admincp`	INTEGER DEFAULT 0," +
                "`design`	TEXT DEFAULT 'default'," +
                "`can_see_sitesettings`	INTEGER DEFAULT 0," +
                "`service_profile`	INTEGER DEFAULT 0," +
                "`djpass`	TEXT DEFAULT '-'," +
                "`djpriority`	INTEGER DEFAULT 1," +
                "`djname`	TEXT DEFAULT '-')";
            this.db.SQLInsert(createDB);

            var autoDJ_insert = "INSERT INTO 'users'('username', 'password', 'djpass', 'djname', 'moderator', 'locked','service_profile') " +
            "VALUES('Auto DJ', '-', NULL, NULL, '1', '1', '1')";
            this.db.SQLInsert(autoDJ_insert);


            var create_groups = "CREATE TABLE IF NOT EXISTS 'usergroups' (" +
            "`id`	INTEGER PRIMARY KEY AUTOINCREMENT," +
            "`name`	TEXT NOT NULL," +
            "`level`	INTEGER DEFAULT 1," +
            "`protected`	INTEGER DEFAULT 0)";
            this.db.SQLInsert(create_groups);

            var create_group = "INSERT INTO 'usergroups'('name', 'level', 'protected') VALUES('System', '255', '1')";
            this.db.SQLInsert(create_group);

            create_group = "INSERT INTO 'usergroups'('name', 'level', 'protected') VALUES('Server-Administratoren', '254', '1')";
            this.db.SQLInsert(create_group);
        }

        public void UpdatePassword(T id, string password)
        {
            if (string.IsNullOrEmpty(password))
                return;

            db.SQLInsert(string.Format("UPDATE users SET password='{1}' WHERE id='{0}'",
                id, MD5.GetMD5Hash(password)));
        }

        public bool UserExists(T id)
            => db.Count("users", "id", id) != 0 && string.Format("{0}", id) != "0";

        public T Get_ServiceProfile()
        {
            var uid = db.SQLQuery("SELECT id FROM users WHERE service_profile='1' LIMIT '1'", "id");
            return (T)Convert.ChangeType(uint.Parse(uid), typeof(T));
        }

        public Dictionary<string, string> GetDJAccounts()
        {
            var SQLres = db.SQLQuery("SELECT djname, djpass, djpriority FROM users WHERE moderator='1'" +
                " AND locked !='1' AND djpass !='-' AND service_profile !='1'");

            var res = new Dictionary<string, string>();
            if (SQLres.Count == 0)
                return res;

            for (var i = uint.MinValue; i < SQLres.Count; i++)
            {
                var _i = (T)Convert.ChangeType(i, typeof(T));
                if (res.ContainsKey(SQLres[_i]["djname"]))
                    continue;

                res.Add(SQLres[_i]["djname"], string.Join(";",
                    SQLres[_i]["djname"], SQLres[_i]["djpass"],
                        SQLres[_i]["djpriority"]));
            }

            return res;
        }

        public bool CanEditUsers(T id)
            => Have_Enough_Rights(id, "edit_user", 235);

        public bool CanDeleteUsers(T id)
            => Have_Enough_Rights(id, "delete_user", 235);

        public bool IsServiceProfile(T id)
            => !string.IsNullOrEmpty(db.SQLQuery(string.Format("SELECT service_profile FROM users WHERE id='{0}'", id),
                "service_profile"));

        public bool CanDeleteSendeplan(T id)
            => Have_Enough_Rights(id, "del_sp", 230);

        public bool CanEditSendeplan(T id)
           => Have_Enough_Rights(id, "edit_sp", 230);

        public bool CanSeeStreamStats(T id)
            => Have_Enough_Rights(id, "see_sc_stats", 225);

        public bool CanSeeSiteSettings(T id)
            => Have_Enough_Rights(id, "see_sitesettings", 225);

        public bool CanAddSendeplan(T id)
            => Have_Enough_Rights(id, "add_sp", 230);

        public bool CanCreateUsers(T id)
         => Have_Enough_Rights(id, "create_user", 235);

        public bool CanSeeAdminCenter(T id)
            => Have_Enough_Rights(id, "see_admincp", 230);

        bool Have_Enough_Rights(T id, string value, byte reqLevel)
        {
            if (!UserExists(id) || string.IsNullOrEmpty(value))
                return false;

            var groupid = db.SQLQuery(string.Format("SELECT usergroup FROM users WHERE id='{0}'", id), "usergroup");
            var can = GetGroupLevelByID((T)Convert.ChangeType(byte.Parse(groupid), typeof(T))) >= reqLevel;
            
            if (!can)
                bool.TryParse(db.SQLQuery(string.Format("SELECT can_{1} FROM users WHERE id='{0}'",
                    id, value), string.Format("can_{0}", value)), out can);

            return can;
        }

        public string GetGroupNameByLevel(T level)
            => db.SQLQuery(string.Format("SELECT name FROM usergroups WHERE level='{0}'", level), "name");

        public string GetGroupNameByID(T id)
            => db.SQLQuery(string.Format("SELECT name FROM usergroups WHERE id='{0}'", id), "name");

        public byte GetGroupLevelByID(T id)
        {
            var lvl = db.SQLQuery(string.Format("SELECT level FROM usergroups WHERE id='{0}'", id), "level");

            return string.IsNullOrEmpty(lvl) ? byte.MinValue : byte.Parse(lvl);
        }

        public bool UpdateUserName(T id, string username)
        {
            if (!UserExists(id) || string.IsNullOrEmpty(username))
                return false;

            return db.SQLInsert(string.Format("UPDATE users SET username='{1}' WHERE id='{0}'", id, username));
        }

        public bool SetModeratorState(T id, string value)
        {
            if (!UserExists(id) || string.IsNullOrEmpty(value))
                return false;

            return db.SQLInsert(string.Format("UPDATE users SET moderator='{1}' WHERE id='{0}'", id, value == "on" ? "1" : "0"));
        }

        public Dictionary<T, NameValueCollection> GetModerators(int locked = 0)
            => db.SQLQuery(string.Format("SELECT * FROM users WHERE moderator='1' AND service_profile='0'", locked));

        public string GetUserNamebyID(T id) => db.SQLQuery(string.Format("SELECT username FROM users WHERE id='{0}'", id), "username");

        public T GetUserIDbyName(string username)
            => (T)Convert.ChangeType(db.SQLQuery(string.Format("SELECT id FROM users WHERE username='{0}'", username), "id"), typeof(T));

        public string GetUserDesignbyID(T id, string design)
        {
            var d = db.SQLQuery(string.Format("SELECT design FROM users WHERE id='{0}'", id), "design");

            return !string.IsNullOrEmpty(d) ? d : design;
        }

        public bool IsModerator(T id) => bool.Parse(db.SQLQuery(string.Format("SELECT moderator FROM users WHERE id='{0}'", id), "moderator"));

        public bool IsService(T id) => bool.Parse(db.SQLQuery(string.Format("SELECT service_profile FROM users WHERE id='{0}'", id), "service_profile"));

        public bool IsLocked(T id) => bool.Parse(db.SQLQuery(string.Format("SELECT locked FROM users WHERE id='{0}'", id), "locked"));

        public int GetUserLevelbyID(T id)
        {
            var uid = string.Format("{0}", id);
            if (uid == "0")
                return 0;

            var x = db.SQLQuery(string.Format("SELECT usergroup FROM users WHERE id='{0}'", uid), "usergroup");

            return (!string.IsNullOrEmpty(x) ? int.Parse(x) : 0);
        }

        public string GetUserAvatarbyID(T id) =>
            db.SQLQuery(string.Format("SELECT avatar FROM users WHERE id='{0}'", id), "avatar");

        public bool HandleRegisterRequest(string username, string password, string password_agree)
        {
            var result = false;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(password_agree))
                return result;

            if (username.Length <= 4)
                return result;

            if (password.Length < 6 || password_agree.Length < 6)
                return result;

            if (db.Count("users", "username", username) != 0)
                return result;

            if (MD5.GetMD5Hash(password) == MD5.GetMD5Hash(password_agree))
                if (db.SQLInsert(string.Format("INSERT INTO users (username, password)" +
                    " VALUES ('{0}','{1}')", username, MD5.GetMD5Hash(password))))
                    result = true;

            return result;
        }

        public T HandleLoginRequest(string username, string password)
        {
            var nouser = (T)Convert.ChangeType(ulong.Parse("0"), typeof(T));

            var uid = GetUserIDbyName(username);
            if (uid.Equals(nouser))
                return nouser;
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return nouser;

            var pwhash = MD5.GetMD5Hash(password);

            var usrs = db.SQLQuery(string.Format("SELECT id FROM users WHERE password='{0}'" +
                " AND username='{1}' AND service_profile != '1' LIMIT '1'", pwhash, username), "id");

            return !string.IsNullOrEmpty(usrs) ? (T)Convert.ChangeType(usrs, typeof(T)) : nouser;
        }

        public string GetUserInfo(T id, ref Filesystem fs)
        {
            var box = Dispatcher.ReadTemplate(ref fs, "content-box");
            var picture = GetUserAvatarbyID(id);
            var output = "<ul>";

            if (fs.Exists(picture))
                output += string.Format("<li class=\"user_image\"><img src=\"{0}\" /></li>", picture);

            var grouplevel = (T)Convert.ChangeType(GetGroupLevelByID((T)Convert.ChangeType(GetUserLevelbyID(id), typeof(T))), typeof(T));

            output += string.Format("<li><a href=\"#\" onclick=\"LoadDocument('/providers/users/'," +
                "'.userinfo-box','Logout','logout','')\">Abmelden</a></li>", id);
            output += "</ul>";

            var res = box.Replace("[[box-content]]", output).Replace("[[box-title]]",
                GetUserNamebyID(id)).Replace("[[content]]", "userinfo");
            return res;
        }

        public bool DeleteUser(T UserToDelete, T userid)
        {
            var result = false;

            if (db.Count("users", "id", UserToDelete) != 0 && CanDeleteUsers(userid) && !IsService(UserToDelete))
            {
                result = db.SQLInsert(string.Format("DELETE FROM users WHERE id='{0}' AND service_profile != '1'", UserToDelete, userid));
                result = db.SQLInsert(string.Format("DELETE FROM sendeplan WHERE uid='{0}'", UserToDelete, userid));
                result = db.SQLInsert(string.Format("DELETE FROM sendeplan_replay WHERE uid='{0}'", UserToDelete, userid));
            }

            return result;
        }

        public string GetRegisterForm(ref Filesystem fs)
        {
            var tpl = "user_register";
            var output = Dispatcher.ReadTemplate(ref fs, tpl);

            return output;
        }

        public string GetUserList(T userid, ref Filesystem fs)
        {
            var box = Dispatcher.ReadTemplate(ref fs, "content-box");

            var usrs = db.SQLQuery("SELECT * FROM users WHERE service_profile = '0'");
            var tmp = "<div class=\"tr sp_day\"><div class=\"th\">Name</div><div class=\"th\">Gruppe</div>" +
                "<div class=\"th\">Options</div>";

            tmp += "</div>";

            var options = string.Empty;

            for (var i = uint.MinValue; i < usrs.Count; i++)
            {
                var _i = (T)Convert.ChangeType(i, typeof(T));
                options = string.Format("<a href=\"#\" onclick=\"LoadDocument('/providers/users/','#content','Profil','profile','uid={0}','')\"><img src=\"images/show.png\" height=\"24px\" /></a>", usrs[_i]["id"]);

                if (CanDeleteUsers(userid) && usrs.Count >= 2)
                    options += string.Format(" <a href=\"#\" onclick=\"LoadDocument('/providers/users/','#content','Löschen','del','uid={0}','')\"><img src=\"images/delete.png\" height=\"24px\" /></a>", usrs[_i]["id"]);

                if (CanEditUsers(userid) || usrs[_i]["id"] == string.Format("{0}", userid))
                    options += string.Format(" <a href=\"#\" onclick=\"LoadDocument('/providers/users/','#content','Bearbeitem','edit','uid={0}','')\"><img src=\"images/edit.png\" height=\"24px\" /></a>", usrs[_i]["id"]);

                tmp += string.Format("<div class=\"ul_row\"><div class=\"td\"><img src=\"{3}\" height=\"24px\" /></div><div class=\"td\">{0}</div><div class=\"td\">{1}</div><div class=\"td\">{2}</div></div>\n",
                    usrs[_i]["username"], GetGroupNameByID((T)Convert.ChangeType(usrs[_i]["usergroup"], typeof(T))), options, GetUserAvatarbyID((T)Convert.ChangeType(usrs[_i]["id"], typeof(T))));
            }

            return box.Replace("[[box-content]]", tmp).Replace("[[box-title]]", "Benutzer Liste").Replace("[[content]]", "userlist");
        }

        public string GetProfile(ref Filesystem fs, T profile_id, T userid)
        {
            var output = string.Empty;

            if (UserExists(profile_id))
            {
                output = Dispatcher.ReadTemplate(ref fs, "users_profile");
                var profile = ulong.Parse(string.Format("{0}", (T)Convert.ChangeType(profile_id, typeof(T))));
                var user = ulong.Parse(string.Format("{0}", (T)Convert.ChangeType(userid, typeof(T))));
                var modform = string.Empty;

                if (CanEditUsers(userid) && profile != user)
                {
                    modform = "<label for=\"ismod\">Moderator </label><input type = " +
                        "\"checkbox\" name=\"ismod\" id=\"ismod\" class=\"input_text\" [#USER_ISMOD#] />";

                    var is_mod = IsModerator(profile_id);
                    modform = modform.Replace("[#USER_ISMOD#]", is_mod ? "checked" : string.Empty);
                }
                else
                    modform = string.Format("<input type=\"hidden\" name=\"ismod\"" +
                        " id=\"ismod\" value=\"{0}\" />", IsModerator(profile_id) ? 1 : 0);

                output = output.Replace("[#USER_MODSETTINGS#]", modform);
                output = output.Replace("[#USER_AVATAR#]", GetUserAvatarbyID(profile_id));

                output = output.Replace("[#USER_NAME#]", GetUserNamebyID(profile_id));
                var level = (T)Convert.ChangeType(GetUserLevelbyID(profile_id), typeof(T));

                output = output.Replace("[#USER_LEVEL#]", string.Format("{0}", GetGroupNameByID(level)));
                output = output.Replace("[#USER_ID#]", string.Format("{0}", profile_id));
            }

            return output;
        }

        public string GenerateTeamlist(ref Filesystem fs)
        {
            var result = Dispatcher.ReadTemplate(ref fs, "content-box");
            var sql_query = db.SQLQuery("SELECT * FROM users WHERE moderator = '1' AND service_profile = '0'");
            if (sql_query.Count != 0)
            {
                var j = 0;
                var tmp = string.Empty;

                for (var i = 0; i < sql_query.Count; i++)
                {
                    var _i = (T)Convert.ChangeType(i, typeof(T));
                    var user_name = sql_query[_i]["username"];
                    var user_avatar = sql_query[_i]["avatar"];
                    var id = sql_query[_i]["id"];

                    if (j == 0)
                        tmp += "<div class=\"tr\">";
                    tmp += "<div class=\"td\">";

                    tmp += string.Format("<h3>{0}</h3><br />", user_name);
                    tmp += string.Format("<img src=\"{0}\" class=\"user_image\"/>", user_avatar);

                    tmp += "</div>";

                    j++;

                    if (j >= 2)
                    {
                        tmp += "</div><br />";
                        j = 0;
                    }
                }

                result = result.Replace("[[box-content]]", tmp).Replace("[[content]]", "teamlist")
                    .Replace("[[box-title]]", "Das Team");
            }

            return result;
        }

        public string GetLoginForm(ref Filesystem fs)
        {
            var output = Dispatcher.ReadTemplate(ref fs, "user_login");
            return output;
        }

        public string Get_admincp_user_settings(Filesystem fs, T user)
        {
            var output = Dispatcher.ReadTemplate(ref fs, "user_admin_settings");

            return output;
        }

    }
}
