using System.Collections.Generic;
using Namiono;
using static Namiono.WebServer;

namespace Namiono.Widgets
{
	public class Form
	{
		public List<Control> Controls;

		public virtual string ContentType { get; set; }
			= "application/x-www-form-urlencoded; charset=UTF-8";

		public virtual string Name { get; set; } = "";

		public virtual ControlType ControlType { get; set; } = ControlType.Form;
		public virtual FormatType FormatType  { get; set; } = FormatType.Widget; 
		
		public virtual string Url { get; set; } = "";

		public virtual SiteAction Action { get; set; } = SiteAction.None;

		public virtual string Frame { get; set; } = "";

		public virtual string Provider { get; set; } = null;

		public virtual string Method { get; set; } = "POST";

		public virtual string Id { get; set; } = "";

		public Form(string provider, SiteAction action, string frame, string name, string id, string url, string method)
		{
			Provider = provider;
			Frame = frame;
			Controls = new List<Control>();
			Name = name;
			Id = id;
			ControlType = ControlType.Form;
			FormatType = FormatType.Widget;

			Method = method;
			Action = action;
			Url = url;
		}
	}
}
