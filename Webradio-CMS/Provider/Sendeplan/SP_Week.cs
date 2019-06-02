using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Namiono
{
    public class SP_Week<T>
    {
        List<SP_Day<T>> _days = new List<SP_Day<T>>();
        Guid spident;

        public SP_Week(Guid spident, double timestamp)
        {
            Timestamp = timestamp;
            this.spident = spident;
        }

        public void Add_Day(SP_Day<T> day)
        {
            _days.Add(day);
        }

        public IEnumerable<SP_Day<T>> Days
            => (from d in _days
                where Timestamp >= d.Timestamp
                where spident == d.SP_Ident
                select d).AsEnumerable();

        public double Timestamp { get; private set; }
    }
}
