namespace ThatsLit;

public struct TerrainDetailScore(bool item1, float item2, float item3)
{
	public bool cached = item1;

	public float prone = item2;

	public float regular = item3;

	public override bool Equals(object obj)
	{
		if (obj is TerrainDetailScore terrainDetailScore && cached == terrainDetailScore.cached && prone == terrainDetailScore.prone)
		{
			return regular == terrainDetailScore.regular;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return unchecked((1044908159 * -1521134295 + cached.GetHashCode()) * -1521134295 + prone.GetHashCode()) * -1521134295 + regular.GetHashCode();
	}

	public void Deconstruct(out bool item1, out float item2, out float item3)
	{
		item1 = cached;
		item2 = prone;
		item3 = regular;
	}

	public static implicit operator (bool, float, float)(TerrainDetailScore value)
	{
		return (value.cached, value.prone, value.regular);
	}

	public static implicit operator TerrainDetailScore((bool, float, float, float, float) value)
	{
		return new TerrainDetailScore(value.Item1, value.Item2, value.Item3);
	}
}
