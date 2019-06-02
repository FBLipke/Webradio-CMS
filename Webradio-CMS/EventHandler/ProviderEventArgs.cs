using System;
using System.Collections.Generic;
using System.Net;

namespace Namiono
{
	public class ProviderEventArgs<T> : IDisposable
	{
		public ProviderEventArgs(ProviderAction action, ERROR_CODES error, T userid, 
			Dictionary<string, string> parameters, string response, WebServer listener, ref HttpListenerContext context)
		{
			Action = action;
			ErrorCode = error;
			Response = response;
			Parameters = parameters;
			Socket = listener;
			Context = context;
			UserId = userid;
		}

		public void Dispose()
		{
		}

		public T UserId;
		public Dictionary<string, string> Parameters;
		public WebServer Socket;
		public ProviderAction Action;
		public HttpListenerContext Context;
		public ERROR_CODES ErrorCode;
		public string Response;
	}
}