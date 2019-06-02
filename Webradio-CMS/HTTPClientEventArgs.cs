using System;
using System.Net;
using System.Text;

namespace Namiono
{
	public class HTTPClientEventArgs : IDisposable
	{
		public HTTPClientEventArgs(HttpStatusCode status, string description, byte[] response, string contentType, long contentLength)
		{
			Response = response;
			StatusCode = status;
			StatusDescription = description;
			ContentType = contentType;
			ContentLength = contentLength;
		}

		public HttpStatusCode StatusCode;
		public string StatusDescription = "";

		public byte[] Response;
		public long ContentLength = 0;
		public string ContentType = "";

		public void Dispose()
		{
			Array.Clear(Response, 0, Response.Length);
		}
	}
}