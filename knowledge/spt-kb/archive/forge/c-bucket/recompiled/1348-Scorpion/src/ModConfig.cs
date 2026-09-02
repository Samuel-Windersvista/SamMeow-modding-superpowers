namespace _scorpion;

public class ModConfig
{
    public double PriceMultiplier { get; set; }

    public int TraderRefreshMin { get; set; }

    public int TraderRefreshMax { get; set; }

    public bool AddTraderToFlea { get; set; }

    public bool RemoveLoyaltyRestriction { get; set; }

    public bool RandomizeBuyRestriction { get; set; }

    public bool RandomizeStockAvailable { get; set; }

    public int OutOfStockChance { get; set; }

    public bool EventQuestsAlwaysActive { get; set; }
}
