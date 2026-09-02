using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateFollowerProgressBatchRequest : IRequestData
{
	public List<FriendlyTeammateFollowerProgressRequest> Entries { get; set; } = new List<FriendlyTeammateFollowerProgressRequest>();
}
