using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Namiono
{
	public interface IProvider<I>
	{
		ERROR_CODES Remove(Guid id);
		bool Contains(Guid id);
		void Close();
		void Start();
		void HeartBeat();
		void Update();
		void Dispose();
        void Install(string script);
        I Get_Member(Guid id);

        SQLDatabase<uint> Database { get; set; }
        Filesystem FileSystem { get; set; }
        Dictionary<Guid, I> Members { get; set; }
    }
}
