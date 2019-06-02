using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Namiono
{
	public class Dispatcher
	{
		public static string ReadTemplate(ref Filesystem fs, string name)
		{
			var tpl_path = string.Format("templates/{0}.tpl", name.Replace("-", "_"));
			if (!fs.Exists(tpl_path))
				return string.Format("<p class=\"exclaim\">Dispatcher (Template file for \"{0}\" was not found!</p>", name);
			else
			{
				var output = fs.Read(tpl_path);
				return Encoding.UTF8.GetString(output, 0, output.Length);
			}
		}

		static string ExtractString(string input, string startDel, string endDel)
		{
			var start = input.IndexOf(startDel) + startDel.Length;
			var end = input.IndexOf(endDel);
			var length = end - start;

			return input.Substring(start, length);
		}

		public static string GetTPLName(string input, string startDel = "[[", string endDel = "]]")
			=> ExtractString(input, startDel, endDel);
	}
}
