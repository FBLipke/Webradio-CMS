using System;

namespace Namiono
{
	public class Member<T> : IDisposable
	{
		public virtual double Created { get; set; }	= DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

		public virtual string Url { get; set; } = null;

		public virtual double Updated { get; set; } = DateTime.Now.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

		public virtual string OutPut { get; set; } = null;

        public virtual Guid Author { get; set; } = Guid.Empty;

		public virtual FormatType FormatType { get; set; } = FormatType.Object;

		public virtual ControlType ControlType { get; set; } = ControlType.None;

		public virtual string Frame { get; set; } = null;

		public virtual string Provider { get; set; } = null;

		public virtual string Design { get; set; } = "default";

		public virtual string Image { get; set; } = "/styles/default/images/big_circle.png";

		public virtual int Width { get; set; } = 18;

		public virtual int Height { get; set; } = 18;

		public virtual string Name { get; set; } = null;

		public virtual string Password { get; set; } = null;

		public virtual bool Active { get; set; } = true;

		public virtual string Action { get; set; } = "show";

		public virtual ulong Level { get; set; } = 0;

        public virtual Guid Group { get; set; } = Guid.Empty;

		public virtual bool Locked { get; set; } = false;

		public virtual bool Service { get; set; } = false;

        public virtual bool Moderator { get; set; } = false;

		public virtual string Target { get; set; } = null;

		public virtual short Port { get; set; } = 0;

		public virtual string IPAddress { get; set; } = "0.0.0.0";

        public virtual Guid Id { get; set; } = Guid.Empty;

		public virtual string ExtraData { get; set; } = null;

		public virtual void Dispose()
		{
		}
	}
}
