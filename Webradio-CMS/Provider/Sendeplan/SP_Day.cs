using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Namiono
{
    public class SP_Day<T>
    {
        List<SendeplanEntry<T>> _entries;
        Guid spid;

       public SP_Day(Guid spident, int day, double ts)
        {
            spid = spident;
            _entries = new List<SendeplanEntry<T>>();
            DayOfWeek = day;
            Timestamp = ts;
        }

        public void Add_Entry(SendeplanEntry<T> entry)
        {
            _entries.Add(entry);
        }

        public int DayOfWeek { get; set; }

        public double Timestamp { get; private set; }

        public Guid SP_Ident { get; private set; }

        /// <summary>
        /// Gibt die Sendungen des Tages zurück;
        /// </summary>
        public List<SendeplanEntry<T>> Entries
        {
            get
            {
                return (from e in _entries
                        where e.Day == DayOfWeek
                        where e.TimeStamp >= Timestamp
                        select e).ToList();
            }
        }
    }
}
