using System;
using System.IO;

namespace Namiono
{
	class Program
	{
		static void CatchUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			DateTime dtLogFileCreated = DateTime.Now;

			try
			{
				using (var sw = new StreamWriter("crash-" + dtLogFileCreated.Day + dtLogFileCreated.Month
						   + dtLogFileCreated.Year + "-" + dtLogFileCreated.Second
							+ dtLogFileCreated.Minute + dtLogFileCreated.Hour + ".txt"))
				{
					var ex = (Exception)e.ExceptionObject;

					sw.WriteLine("### NAMIONO crashlog ### ");
					sw.WriteLine("Exception: {0}", e.ExceptionObject.ToString());

					sw.WriteLine(ex);

					if (ex.InnerException != null)
						sw.WriteLine(ex.Message + ex.StackTrace);

					sw.Close();
				}
			}
			finally
			{

			}
		}

		static void Main(string[] args)
		{
			//AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CatchUnhandledException);
			var x = string.Empty;
			var core = new Common(args);
			core.Bootstrap();

			core.Start();

			while (x != "!exit")
				x = Console.ReadLine();

			core.Close();
			core.Dispose();
		}
	}
}
