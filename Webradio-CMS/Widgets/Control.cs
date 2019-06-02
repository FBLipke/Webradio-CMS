namespace Namiono
{
	// Base class for widgets
	public enum ControlType
	{
		Input,
		Textbox,
		Button,
		Hidden,
		File,
		Password,
		Form,
		Label,
		None
	}

	public class Control
	{
		public Control(ControlType type, string name, string id, string value = "")
		{
			Type = type;
			Name = name;
			Id = id;
			Value = value;
		}

		public Control(string name, ControlType type, string id, string value = "")
		{
			Type = type;
			Value = value;
			Id = id;
			Name = name;
		}

		public virtual int Width { get; set; } = 0;

		public virtual int Height { get; set; } = 0;

		public virtual string Name { get; set; } = "";

		public virtual string Value { get; set; } = "";

		public virtual string Id { get; set; } = "";

		public virtual ControlType Type { get; set; }

		public virtual int MaxLength { get; set; } = 0;

		public virtual bool Enabled { get; set; } = true;
	}
}
