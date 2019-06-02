using System;

namespace Namiono
{
    public sealed partial class ShoutcastServer<T>
    {
        public class ShoutcastStartedEventArgs : EventArgs
		{
			public string Message;

			public ShoutcastStartedEventArgs(string message)
			{
				this.Message = message;
			}
		}
	}
}
