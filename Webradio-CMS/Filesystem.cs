using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Namiono
{
	public class Filesystem
	{
		string rootDir;
		int fscache;

		public Filesystem(string path, int cache = 4096)
		{
			rootDir = path;
			fscache = cache;

			Directory.CreateDirectory(rootDir);
		}

		static string ReplaceSlashes(string path, string curSlash, string newSlash)
		{
			while (path.Contains(curSlash))
				path = path.Replace(curSlash, newSlash);

			return path;
		}

		public static int ExecuteFile(string path, string arg)
		{
			var result = 1;
			try
			{
				using (var prc = new Process())
				{
					try
					{
						prc.StartInfo.Arguments = arg;
						prc.StartInfo.FileName = path;

						prc.Start();

						prc.WaitForExit();

						result = prc.ExitCode;
					}
					catch (Exception)
					{
						result = 1;
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}

			return result;
		}

		public static void Delete(string path)
		{
			if (File.Exists(path))
				File.Delete(path);
		}

		public string ResolvePath(string path, bool strip = true)
		{
			var p = path.Trim();

			if (p.StartsWith("/") && p.Length > 3 && strip)
				p = p.Remove(0, 1);

			p = ReplaceSlashes(Combine(rootDir, p), "\\", "/");
			Console.WriteLine(p.Trim());
			return p.Trim();
		}

		public bool Exists(string path)
		{
			var x = ResolvePath(path);
			return File.Exists(x);
		}

		public static string Combine(string p1, params string[] paths)
		{
			var path = p1;

			for (var i = 0; i < paths.Length; i++)
				path = ReplaceSlashes(Path.Combine(path, paths[i]), "\\", "/");

			return path;
		}

		public byte[] Read(string path, int offset = 0, int count = 0)
		{
			var data = new byte[0];

			using (var fs = new FileStream(ResolvePath(path), FileMode.Open, FileAccess.Read,
				FileShare.Read, this.fscache, FileOptions.Asynchronous))
                using(var bs = new BufferedStream(fs))
            {
				data = new byte[(count == 0 || fs.Length < count) ? fs.Length : count];
				var bytesRead = 0;

				do
				{
					bytesRead = fs.Read(data, offset, data.Length);
				} while (bytesRead > 0);
			}

			return data;
		}

		public async Task<bool> Write(string path, byte[] data, int offset = 0, int count = 0)
		{
			var result = false;

			using (var fs = new FileStream(ResolvePath(path),
				FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
			{
				await fs.WriteAsync(data, offset, count);
				result = true;
			}

			return result;
		}

		public async void Write(string path, string data, int offset = 0, int count = 0)
		{
			var tmp = Encoding.ASCII.GetBytes(data);

			await Task.Run(() => Write(path, tmp, offset, count));
		}

		public string Root => rootDir;
	}
}
