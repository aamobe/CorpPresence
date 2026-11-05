using System.Linq;
using CorpPresence.Malice;

namespace CorpPresence;

internal static class Companies
{
	public static readonly Company[] All = new Company[9]
	{
		new PastelCo(),
		new SolanaCo(),
		new CarmichaelCo(),
		new SpacerCo(),
		new MeridianCo(),
		new DavreCo(),
		new RebelCo(),
		new EnvoyCo(),
		new JacksonContact()
	};

	public static Company Find(string id)
	{
		return All.FirstOrDefault((Company c) => c.Id == id);
	}

	public static Company ByCharacter(string charId)
	{
		return All.FirstOrDefault((Company c) => c.CharacterId == charId);
	}

	public static Company ByNumber(long n)
	{
		Company company = All.FirstOrDefault((Company c) => c.PhoneNumber == n);
		if (company == null || !Phone.IsOurs(n, company))
		{
			return null;
		}
		return company;
	}

	public static Company ByLocId(string locId)
	{
		return All.FirstOrDefault((Company c) => locId == "corp_phone_" + c.Id);
	}

	public static CompanyState StateOf(Company c)
	{
		return CorpState.Get()?.CompanyOf(c.Id);
	}
}
