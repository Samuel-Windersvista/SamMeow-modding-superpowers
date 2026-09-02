namespace ThatsLit;

public struct LightAndLaserState
{
	public long storage;

	private static readonly string[] _formatCache;

	public ThatsLitCompat.DeviceMode deviceStateCache;

	public ThatsLitCompat.DeviceMode deviceStateCacheSub;

	public bool VisibleLight
	{
		get
		{
			return (storage & 0x80) > 0;
		}
		set
		{
			storage = (value ? (storage | 0x80) : (storage & -129));
		}
	}

	public bool VisibleLaser
	{
		get
		{
			return (storage & 0x40) > 0;
		}
		set
		{
			storage = (value ? (storage | 0x40) : (storage & -65));
		}
	}

	public bool IRLight
	{
		get
		{
			return (storage & 0x20) > 0;
		}
		set
		{
			storage = (value ? (storage | 0x20) : (storage & -33));
		}
	}

	public bool IRLaser
	{
		get
		{
			return (storage & 0x10) > 0;
		}
		set
		{
			storage = (value ? (storage | 0x10) : (storage & -17));
		}
	}

	public bool VisibleLightSub
	{
		get
		{
			return (storage & 8) > 0;
		}
		set
		{
			storage = (value ? (storage | 8) : (storage & -9));
		}
	}

	public bool VisibleLaserSub
	{
		get
		{
			return (storage & 4) > 0;
		}
		set
		{
			storage = (value ? (storage | 4) : (storage & -5));
		}
	}

	public bool IRLightSub
	{
		get
		{
			return (storage & 2) > 0;
		}
		set
		{
			storage = (value ? (storage | 2) : (storage & -3));
		}
	}

	public bool IRLaserSub
	{
		get
		{
			return (storage & 1) > 0;
		}
		set
		{
			storage = (value ? (storage | 1) : (storage & -2));
		}
	}

	public bool AnyVisible => (storage & 0xCC) > 0;

	public bool AnyVisibleLight => (storage & 0x88) > 0;

	public bool AnyVisibleLaser => (storage & 0x44) > 0;

	public bool AnyIRLight => (storage & 0x22) > 0;

	public bool AnyIRLaser => (storage & 0x11) > 0;

	public bool AnyVisibleMain => (storage & 0xC0) > 0;

	public bool AnyIRMain => (storage & 0x30) > 0;

	public bool AnyVisibleSub => (storage & 0xC) > 0;

	public bool AnyIRSub => (storage & 3) > 0;

	public bool AnyIR => (storage & 0x33) > 0;

	public bool AnyLight => (storage & 0xAA) > 0;

	public bool AnyLightMain => (storage & 0xA0) > 0;

	public bool AnyLaser => (storage & 0x55) > 0;

	public bool AnyMain => (storage & 0xF0) > 0;

	public bool AnySub => (storage & 0xF) > 0;

	public bool Any => (storage & 0xFF) > 0;

	static LightAndLaserState()
	{
		_formatCache = new string[256];
		for (int i = 0; i < 256; i++)
		{
			LightAndLaserState lightAndLaserState = new LightAndLaserState
			{
				storage = i
			};
			_formatCache[i] = lightAndLaserState.FormatRaw();
		}
	}

	public string Format()
	{
		int num = (int)(storage & 0xFF);
		return _formatCache[num];
	}

	private string FormatRaw()
	{
		return "  V " + (VisibleLight ? "◆" : "◇") + (VisibleLaser ? "◆" : "◇") + " IR " + (IRLight ? "◆" : "◇") + (IRLaser ? "◆" : "◇") + " / V " + (VisibleLightSub ? "◆" : "◇") + (VisibleLaserSub ? "◆" : "◇") + " IR " + (IRLightSub ? "◆" : "◇") + (IRLaserSub ? "◆" : "◇") + " ";
	}
}
