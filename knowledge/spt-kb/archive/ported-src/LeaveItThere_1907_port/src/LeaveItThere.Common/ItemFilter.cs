using System.Collections.Generic;
using Newtonsoft.Json;

namespace LeaveItThere.Common;

internal class ItemFilter
{
	public bool WhitelistEnabled;

	public bool BlacklistEnabled;

	public List<string> Whitelist = new List<string>();

	public List<string> Blacklist = new List<string>();

	[JsonIgnore]
	public HashSet<string> WhitelistSet = new HashSet<string>();

	[JsonIgnore]
	public HashSet<string> BlacklistSet = new HashSet<string>();

	public void BuildLookups()
	{
		WhitelistSet = new HashSet<string>(Whitelist);
		BlacklistSet = new HashSet<string>(Blacklist);
	}
}
