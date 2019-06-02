using System;

public sealed partial class Sendeplan<T>
{
    public class Sp_settings : IDisposable
	{
		byte startTime;
		byte endTime;
		string topic;
		string autodj_avatar = "images/autodj.gif";

		public byte Start
		{
			get => startTime;

			set => startTime = value;
		}

		public string AutoDJAvatar
		{
			get => autodj_avatar;

			set => autodj_avatar = value;
		}

		public string Topic
		{
			get => topic;

			set => topic = value;
		}

		public byte End
		{
			get => endTime;

			set => endTime = value;
		}

		public Sp_settings(byte start = 18, byte end = 23, string topic = "Querbeet")
		{
			this.startTime = start;
			this.endTime = end;
			this.topic = topic;
		}

		public void Dispose()
		{
		}
	}
}
