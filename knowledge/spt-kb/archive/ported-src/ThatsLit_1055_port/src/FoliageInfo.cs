using System;
using UnityEngine;

namespace ThatsLit;

public struct FoliageInfo(string name, Vector2 dir, float dis) : IComparable<FoliageInfo>
{
	public string name = name;

	public Vector2 dir = dir;

	public float dis = dis;

	public int CompareTo(FoliageInfo other)
	{
		return Math.Sign(dis - other.dis);
	}

	public static bool operator ==(FoliageInfo x, FoliageInfo y)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (x.name == y.name && x.dis == y.dis)
		{
			return x.dir == y.dir;
		}
		return false;
	}

	public static bool operator !=(FoliageInfo x, FoliageInfo y)
	{
		return !(x == y);
	}

	public override bool Equals(object obj)
	{
		return this == (FoliageInfo)obj;
	}

	public override int GetHashCode()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		return (name, dir, dis).GetHashCode();
	}
}
