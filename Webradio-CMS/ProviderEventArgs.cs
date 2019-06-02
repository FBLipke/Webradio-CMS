using System;
using System.Net;

namespace Namiono
{
	public class ProviderEventArgs : IDisposable
	{
		public ProviderEventArgs(ProviderAction action, ERROR_CODES error,
			string response, WebServer listener , ref HttpListenerContext context)
		{
			Action = action;
			ErrorCode = error;
			Response = response;
			Socket = listener;
			Context = context;
		}

		public void Dispose()
		{
		}

		public WebServer Socket;
		public ProviderAction Action;
		public HttpListenerContext Context;
		public ERROR_CODES ErrorCode;
		public string Response;
	}
}