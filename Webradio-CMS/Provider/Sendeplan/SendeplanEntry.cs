using System;
using Namiono;

public class SendeplanEntry<T>
{
    public SendeplanEntry(int spid, Guid ident, string topic, string desc, T moderator, int day, int hour)
	{
        SP_ID = spid;
        Ident = ident;
        Topic = topic;
        Description = desc;
        Moderator = moderator;
        Hour = hour;
        Day = day;
	}

    public string Topic
    {
        get; private set;
    }

    public string Description
    {
        get; private set;
    }

    public T Moderator
    {
        get; private set;
    }

    public int Day
    {
        get; private set;
    }

    public int Hour
    {
        get; private set;
    }

    public int TimeStamp
    {
        get; private set;
    }

    public int SP_ID
    {
        get; private set;
    }

    public Guid Ident
    {
        get; private set;
    }
}
