using System.Collections.Generic;
using System.Linq;

namespace Jishi.SonosUPnP
{
	public class SonosZone
	{
		private IList<SonosPlayer> members = new List<SonosPlayer>();

		public string Name
		{
			get { return string.Join("+", Members.Select(p => p.RoomName)); }
		}

		public IList<SonosPlayer> Members
		{
			get { return members; }
			set { members = value; }
		}

		public SonosPlayer Coordinator { get; set; }
	}
}