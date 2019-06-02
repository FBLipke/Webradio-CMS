using System.Net;

namespace Namiono
{
	public struct Listener<T>
    {
        IPAddress ipaddress;

        string useragent;
        string referer;

        string city;
        string country;

        T connectionTime;
        T uid;
        T type;
        T grid;
        T triggers;

        public IPAddress IP
        {
            get => ipaddress;
            set => ipaddress = value;
        }

        public string Country
        {
            get => country;
            set => country = value;
        }

        public string City
        {
            get => city;
            set => city = value;
        }

        public string UserAgent
        {
            get => useragent;

            set
            {
                if (value.StartsWith("NSPlayer"))
                    useragent = "Windows Media Player";
                else
                {
                    if (value.StartsWith("Winamp"))
                        useragent = "Winamp";
                    else
                    {
                        if (value.StartsWith("BASS") || value.StartsWith("VLC"))
                            useragent = "VLC Player";
                        else
                        {
                            if (value.StartsWith("iTunes"))
                                useragent = "iTunes";
                            else
                            {
                                if (value.StartsWith("AppleCoreMedia"))
                                    useragent = value.Contains("(iPhone") ?
                                    "ITunes (Iphone)" : value;
                                else
                                {
                                    if (value.StartsWith("Mozilla/5.0"))
                                    {
                                        if (value.Contains("Microsoft Edge"))
                                            useragent = "Microsoft Edge";

                                        if (value.Contains("Trident"))
                                            useragent = "Internet Explorer";

                                        if (value.Contains("Firefox"))
                                            useragent = "Mozilla Firefox";

                                        if (value.Contains("Chrome"))
                                            useragent = "Google Chrome";

                                    }
                                    else
                                        useragent = value;
                                }
                            }
                        }
                    }
                }
            }
        }

        public T ConnectionTime
        {
            get => connectionTime;
            set => connectionTime = value;
        }

        public T UID
        {
            get => uid;
            set => uid = value;
        }

        public T GRID
        {
            get => grid;
            set => grid = value;
        }

        public T Type
        {
            get => type;
            set => type = value;
        }

        public string RREFERES
        {
            get => referer;
            set => referer = value;
        }

        public T Triggers
        {
            get => triggers;
            set => triggers = value;
        }
    }
}
