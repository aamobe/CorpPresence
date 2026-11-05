using System.Collections.Generic;
using Newtonsoft.Json;

namespace CorpPresence;

internal sealed class CompanyState
{
	[JsonIgnore]
	public int CardCourierDay = -1;

	public int Coop { get; set; }

	public double CoopFraction { get; set; }

	public int CoopGainDay { get; set; } = -1;

	public double CoopGainToday { get; set; }

	public List<string> Brand { get; set; } = new List<string>();

	public bool CardGiven { get; set; }

	public List<Order> Orders { get; set; } = new List<Order>();

	public int LastOrderDay { get; set; } = -1;

	public Dictionary<string, int> LimitedBought { get; set; } = new Dictionary<string, int>();

	public bool DealerJoined { get; set; }

	public int DealerSinceDay { get; set; }

	public int LooseSold { get; set; }

	public int PacksMade { get; set; }

	public bool Bribed { get; set; }

	public double WeekSales { get; set; }

	public bool TrafficOn { get; set; }

	public bool VipOn { get; set; }

	public int VipBookedDay { get; set; } = -1;

	public int LabWeek { get; set; } = -1;

	public List<string> LabList { get; set; } = new List<string>();

	public bool LabBooked { get; set; }

	public int LabDay { get; set; } = -1;

	public int GiftWeek { get; set; } = -1;

	public int StaminaLastDay { get; set; } = -1;

	public int SwapLastDay { get; set; } = -1;

	public int EscapeDay { get; set; } = -1;

	public bool CreatureGiven { get; set; }

	public int CreatureLostDay { get; set; } = -1;

	public bool Licensed { get; set; }

	public int BloodDrawn { get; set; }

	public int BloodRecoveryLastDay { get; set; } = -1;

	public int ParcelsDue { get; set; }

	public int RefurbishLastDay { get; set; } = -1;

	public bool CanningOwned { get; set; }

	public bool ForgeOwned { get; set; }

	public double StockBought { get; set; }

	public bool RepMalice { get; set; }

	public int RepSum { get; set; } = -1;

	public List<Visitor> Visitors { get; set; } = new List<Visitor>();
}
