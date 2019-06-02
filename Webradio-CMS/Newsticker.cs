using System;

namespace Namiono
{
	public class Newsticker<T>
	{
		SQLDatabase<T> db;
		Filesystem fs;

		public Newsticker(ref Filesystem fs)
		{
			this.fs = fs;
            this.db = new SQLDatabase<T>("Newsticker.db");

            var createNews = "CREATE TABLE IF NOT EXISTS 'newsticker' (" +
            "`id`	INTEGER PRIMARY KEY AUTOINCREMENT," +
            "`title`	TEXT NOT NULL DEFAULT 'Kein Titel'," +
            "`content`	TEXT DEFAULT '-'," +
            "`author`	INTEGER DEFAULT 1," +
            "`cat`	INTEGER DEFAULT 0," +
            "`date`	TEXT DEFAULT 0)";

            this.db.SQLInsert(createNews);

            var create_group = "INSERT INTO 'newsticker' ('title', 'content', 'author') VALUES('Webseite - Neuinstallation'," +
                "'Die Installation wurde abgeschlossen." + "<br /> DB-Schema: 120<br />DB-Version: 05_00400 <br />" +
                    "<br />Bitte bearbeiten Sie den (erstellten) Administrator-Account per Kommandozeile!', '1')";

            this.db.SQLInsert(create_group);
        }
		/*
		public string GetNews(ref Users<T> users)
		{
			var result = string.Empty;

			var sql_query = db.SQLQuery("SELECT * FROM newsticker");
			if (sql_query.Count != 0)
			{
				for (var i = 0; i < sql_query.Count; i++)
				{
                    var _i = (T)Convert.ChangeType(i, typeof(T));
                    var id = sql_query[_i]["id"];
					var title = sql_query[_i]["title"];
					var content = sql_query[_i]["content"];
					var author = (T)Convert.ChangeType(ulong.Parse(sql_query[_i]["author"]),typeof(T));
					var tpl = Dispatcher.ReadTemplate(ref fs, "news-content")
							.Replace("[#ID#]", id)
								.Replace("[#NEWS_TITLE#]", title)
									.Replace("[#NEWS_CONTENT#]", content)
                                        .Replace("[#NEWS_AUTHOR#]", users.GetUserNamebyID(author));

					result += tpl;
				}
			}

			return result;
		}
		*/

		public void Close()
		{
            this.db.Close();
		}

        public string News_AddForm()
        {
            var tpl = Dispatcher.ReadTemplate(ref fs, "news-form-add");
            return tpl;
        }

        public void Dispose()
		{
            this.db.Dispose();
		}
	}
}
