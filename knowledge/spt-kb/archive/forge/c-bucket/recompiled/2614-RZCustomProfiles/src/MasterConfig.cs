using System;
using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;

namespace RZCustomProfiles;

public record MasterConfig
{
	public HashSet<int> EnabledBaseProfiles { get; set; } = new HashSet<int>();

	public bool UnlockAllOutfits { get; set; }

	public Dictionary<string, bool>? UnlockHideoutCustomizations { get; set; }

	public bool UnlockJaeger { get; set; }

	public bool UnlockRef { get; set; }

	public List<CategoryEntry> ExaminedCategoryBlacklist { get; set; } = new List<CategoryEntry>();

	public List<string> ExaminedBlacklist { get; set; } = new List<string>();

	public const string FileName = "masterConfig.json";

	public static readonly Dictionary<int, string> BaseProfiles = new Dictionary<int, string>
	{
		[0] = "Standard",
		[1] = "Left Behind",
		[2] = "Prepare To Escape",
		[3] = "Edge Of Darkness",
		[4] = "Unheard",
		[5] = "Tournament",
		[6] = "SPT Developer",
		[7] = "SPT Easy start",
		[8] = "SPT Zero to hero"
	};

	public static readonly Dictionary<int, string> SecureContainers = new Dictionary<int, string>
	{
		[1] = ItemTpl.SECURE_WAIST_POUCH,
		[2] = ItemTpl.SECURE_CONTAINER_ALPHA,
		[3] = ItemTpl.SECURE_CONTAINER_BETA,
		[4] = ItemTpl.SECURE_CONTAINER_EPSILON,
		[5] = ItemTpl.SECURE_CONTAINER_GAMMA,
		[6] = ItemTpl.SECURE_CONTAINER_THETA,
		[7] = ItemTpl.SECURE_CONTAINER_KAPPA,
		[8] = ItemTpl.SECURE_CONTAINER_KAPPA_DESECRATED,
		[9] = ItemTpl.SECURE_CONTAINER_BOSS,
		[10] = ItemTpl.SECURE_CONTAINER_GAMMA_TUE,
		[11] = ItemTpl.SECURE_DEVELOPER_SECURE_CONTAINER,
		[12] = ItemTpl.SECURE_TOURNAMENT_SECURED_CONTAINER
	};

	public static readonly Dictionary<string, (string CategoryId, string CustomisationType)> HideoutCategories = new Dictionary<string, (string, string)>
	{
		["Wall"] = ("67373f1e5a5ee73f2a081baf", "wall"),
		["Floor"] = ("67373f170eca6e03ab0d5391", "floor"),
		["Ceiling"] = ("673b3f595bf6b605c90fcdc2", "ceiling"),
		["Light"] = ("67373f286cadad262309e862", "light"),
		["MannequinPose"] = ("675ff48ce8d2356707079617", "mannequinPose"),
		["ShootingRangeMark"] = ("67373f330eca6e03ab0d5394", "shootingRangeMark")
	};

	public static readonly HashSet<string> ProtectedSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SecuredContainer", "Pockets", "Dogtag" };
}
