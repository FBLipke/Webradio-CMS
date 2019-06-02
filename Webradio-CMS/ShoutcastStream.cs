using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Xml;
using MaxMind.GeoIP2;
using System.Linq;
using System.Globalization;

namespace Namiono
{
	/*
	public sealed class ShoutcastStream<T> : Member<T>
    {
        string songTitle;
        string password;
        string serverName;
        string streamPath;
        string serverUrl;
        string contentType;

        Dictionary<T, Listener<T>> listeners;
        Filesystem fs;
        Users<T> users;

        int port;
        string hostname;
        string version;

        XmlDocument xmldoc;
        bool active;
        DatabaseReader geodb;

        public ShoutcastStream(ref Filesystem fs, ref DatabaseReader geodb,
            ref Users<T> users, string hostname, int port, string password = "")
        {
            this.password = password;
            this.hostname = hostname;
            this.port = port;
            this.fs = fs;
            this.users = users;
            this.geodb = geodb;
            this.version = string.Empty;
            this.listeners = new Dictionary<T, Listener<T>>();
        }

        void ParseData(string response)
        {
            if (string.IsNullOrEmpty(response))
                return;

            var xml = new XmlDocument();
            xml.LoadXml(response);

            xml.CreateXmlDeclaration("1.0", "UTF-8", "yes");
            serverName = xml.DocumentElement.SelectSingleNode("SERVERTITLE").InnerText;
            serverUrl = xml.DocumentElement.SelectSingleNode("SERVERURL").InnerText;
            songTitle = xml.DocumentElement.SelectSingleNode("SONGTITLE").InnerText;
            contentType = xml.DocumentElement.SelectSingleNode("CONTENT").InnerText;

            streamPath = string.Format("http://{0}:{1}{2}", hostname, port,
                xml.DocumentElement.SelectSingleNode("STREAMPATH").InnerText);

            active = int.Parse(xml.DocumentElement.SelectSingleNode("STREAMSTATUS").InnerText) != 0;

            lock (listeners)
            {
                listeners.Clear();

                var ls = xml.DocumentElement.SelectNodes("LISTENERS/LISTENER");
                for (var i = 0; i < ls.Count; i++)
                {
                    if (string.IsNullOrEmpty(ls.Item(i).SelectSingleNode("UID").InnerText))
                        continue;

                    var uid = (T)Convert.ChangeType(ulong.Parse(ls.Item(i).SelectSingleNode("UID").InnerText), typeof(T));

                    if (!listeners.ContainsKey(uid))
                    {
                        var hostEbtry = Dns.GetHostEntry(ls.Item(i).SelectSingleNode("HOSTNAME").InnerText).AddressList;
                        if (hostEbtry.Count() != 0)
                        {
                            var listener = new Listener<T>();
                            listener.IP = hostEbtry[0];

                            var ctime = ulong.Parse(ls.Item(i).SelectSingleNode("CONNECTTIME").InnerText);
                            listener.ConnectionTime = (T)Convert.ChangeType(ctime, typeof(T));

                            var grid = ulong.Parse(ls.Item(i).SelectSingleNode("GRID").InnerText);
                            listener.GRID = (T)Convert.ChangeType(grid, typeof(T));

                            var triggers = ulong.Parse(ls.Item(i).SelectSingleNode("TRIGGERS").InnerText);
                            listener.Triggers = (T)Convert.ChangeType(triggers, typeof(T));

                            var ua = ls.Item(i).SelectSingleNode("USERAGENT").InnerText;
                            listener.UserAgent = !string.IsNullOrEmpty(ua) ? ua : "Unbekannt";

                            var type = ulong.Parse(ls.Item(i).SelectSingleNode("TYPE").InnerText);
                            listener.Type = (T)Convert.ChangeType(type, typeof(T));

                            listener.UID = uid;

                            var referer = ls.Item(i).SelectSingleNode("REFERER").InnerText;
                            listener.RREFERES = string.IsNullOrEmpty(referer) ? string.Empty : referer;

                            var city = geodb.City(listener.IP).City.Name;
                            listener.City = string.IsNullOrEmpty(city) ? string.Empty : city;

                            var country = geodb.City(listener.IP).Country.Name;
                            listener.Country = string.IsNullOrEmpty(country) ? string.Empty : country;

                            listeners.Add(uid, listener);
                        }
                    }
                }
            }
        }

        void Download()
        {
            if (string.IsNullOrEmpty(password))
                return;

            using (var wc = new WebClient())
            {
                wc.DownloadStringCompleted += (sender, e) =>
                {
                    try
                    {
                        if (e.Cancelled)
                            return;

                        var result = e.Result;
                        if (!string.IsNullOrEmpty(result))
                            ParseData(result);
                    }
                    catch (Exception ex)
                    {
                        if (ex.InnerException != null)
                            Console.WriteLine(ex.InnerException);

                        Console.WriteLine(ex.Message);
                    }
                };

                var u = new Uri(string.Format("http://{0}:{1}/admin.cgi?mode=viewxml&pass={2}&sid={3}", hostname, port, password, null));
                wc.DownloadStringAsync(u);
            }
        }

        public string MetaData
        {
            get
            {
                var output = string.Format("<ul id=\"playerinfo_{0}\">", Id);
                output += string.Format("\t<li><img src=\"images/autodj.gif\" height=\"64px\" class=\"user_image\" /></li>");
                output += string.Format("\t<li class=\"metadata\"><p>{0}</p></li>", songTitle);
                output += "</ul>";

                return output;
            }
        }

        public string ContentType
        {
            get => contentType;

            private set => contentType = value;
        }

        public string StreamPath
        {
            get => streamPath;

            private set => streamPath = value;
        }

        public string ServerURL
        {
            get => serverUrl;

            private set => serverUrl = value;
        }

        public string Clients(T userid)
        {

            var tmp = "<div class=\"tr sp_day\"><div class=\"th  sc_col_id\">Dauer (HH:mm)</div>" +
                "<div class=\"th sc_col_ip\">IP</div><div class=\"th sc_col_ua\">Player</div></div>";

            lock (listeners)
            {
                if (listeners.Count != 0)
                {
                    foreach (var l in listeners.Values)
                    {
                        var ctime = Convert.ToInt32((T)Convert.ChangeType(l.ConnectionTime, typeof(T)));
                        var ts = new DateTime(new TimeSpan(ctime / 3600, ctime / 60, ctime / 60 / 60).Ticks).ToString("HH:mm", new CultureInfo("de-DE"));

                        if (users.CanSeeStreamStats(userid))
                        {
                            tmp += string.Format("<div class=\"tr sp_row\"><div class=\"td sc_col_id\"><h3>{0}</h3></div>" +
                            "<div class=\"td sc_col_ip\"><h3>{1}<br />({3}, {4})</h3></div><div class=\"td sc_col_ua\">{2}</div></div>",
							  ts, l.IP, l.UserAgent, l.Country, l.City);
                        }
                        else
                        {
                            tmp += string.Format("<div class=\"tr sp_row\"><div class=\"td sc_col_id\"><h3>{0}</h3></div>" +
                            "<div class=\"td sc_col_ip\"><h3>{1}</h3></div><div class=\"td sc_col_ua\">{2}</div></div>",
                              ts, l.UserAgent);
                        }
                    }
                }
                else
                    tmp += "<div class=\"tr sp_row\"><p class=\"exclaim\">Zur Zeit keine Zuhörer verfügbar.</p></div>";
            }

            var box = Dispatcher.ReadTemplate(ref fs, "content_box");

            return box.Replace("[[box-content]]", tmp)
                .Replace("[[box-title]]", "Aktive Zuhörer")
                .Replace("[[content]]", "sc_listeners");
        }

        public int Listeners => listeners.Count;

        public string SongTitle
        {
            get => songTitle;

            private set => songTitle = value;
        }

        public string ServerName
        {
            get => serverName;

            private set => serverName = value;
        }

        public XmlDocument XML
        {
            get => xmldoc;

            set => xmldoc = value;
        }
    }
	*/
}
