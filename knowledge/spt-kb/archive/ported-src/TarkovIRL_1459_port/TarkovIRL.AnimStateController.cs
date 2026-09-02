namespace TarkovIRL;

internal class AnimStateController
{
	public enum EWeaponState
	{
		INTO_RELOAD,
		MID_RELOAD,
		MID_RELOAD_2,
		OUT_OF_RELOAD,
		IDLE,
		CHECK_MAG,
		RELOAD_FAST,
		UNKNOWN_STATE,
		ORDER_ARM,
		PRESENT_ARM,
		TRIGGER_PULL,
		CHECK_CHAMBER,
		SHOOT
	}

	private static EWeaponState _weaponState;

	public static readonly int _Blindfire1 = -1271366218;

	public static readonly int _Blindfire2 = 1276948056;

	public static readonly int _TurnState1 = -31136456;

	public static readonly int _TurnState2 = 287005718;

	public static readonly int _LeftShoulderStance = 0;

	public static readonly int _SideStepLeft = 731208140;

	public static readonly int _SideStepRight = 1696653403;

	public static readonly int _Running = -244792566;

	public static readonly int _WeaponIdle = -1479210739;

	public static readonly int _CheckMag = 351790385;

	public static readonly int _EjectLastRound = -1717115204;

	public static readonly int _ReachForMag = 1898839857;

	public static readonly int _HandOnMag = 1051831248;

	public static readonly int _PullMag = -1191338705;

	public static readonly int _ReplaceMag = -1821557201;

	public static readonly int _ReturnHandAfterCharging = -507049588;

	public static readonly int _ReturnHandWithoutCharging = 32228103;

	public static readonly int _ReturnHandNoMag = 1392741434;

	public static readonly int _FastReloadDumpMag = -1162171678;

	public static readonly int _WithdrawWeapon = -1205675207;

	public static readonly int _WithdrawWeaponQuick = -896401840;

	public static readonly int _PresentWeapon = -965243548;

	public static readonly int _Shoot = 590329303;

	public static readonly int _TriggerPull = -1160784120;

	public static readonly int _CheckChamber = -131286203;

	private static int _currentBodyStateHash = 0;

	private static int _currentWeaponStateHash = 0;

	public static bool IsTurning => _currentBodyStateHash == _TurnState1 || _currentBodyStateHash == _TurnState2;

	public static bool IsBlindfire => _currentBodyStateHash == _Blindfire1 || _currentBodyStateHash == _Blindfire2;

	public static bool IsLeftShoulder => _currentBodyStateHash == _LeftShoulderStance;

	public static bool IsSideStep => _currentBodyStateHash == _SideStepLeft || _currentBodyStateHash == _SideStepRight;

	public static bool IsRunning => _currentBodyStateHash == -244792566;

	public static EWeaponState WeaponState => _weaponState;

	public static int WeaponStateHash => _currentWeaponStateHash;

	public static void SetCurrentBodyAnimState(int state)
	{
		_currentBodyStateHash = state;
	}

	public static void SetCurrentWeaponAnimState(int state)
	{
		_currentWeaponStateHash = state;
		if (state == _HandOnMag)
		{
			_weaponState = EWeaponState.INTO_RELOAD;
		}
		else if (state == _PullMag)
		{
			_weaponState = EWeaponState.MID_RELOAD;
		}
		else if (state == _ReplaceMag)
		{
			_weaponState = EWeaponState.MID_RELOAD_2;
		}
		else if (state == _ReturnHandAfterCharging || state == _ReturnHandWithoutCharging || state == _ReturnHandNoMag)
		{
			_weaponState = EWeaponState.OUT_OF_RELOAD;
		}
		else if (state == _CheckMag)
		{
			_weaponState = EWeaponState.CHECK_MAG;
		}
		else if (state == _FastReloadDumpMag)
		{
			_weaponState = EWeaponState.RELOAD_FAST;
		}
		else if (state == _WeaponIdle)
		{
			_weaponState = EWeaponState.IDLE;
		}
		else if (state == _WithdrawWeapon || state == _WithdrawWeaponQuick)
		{
			_weaponState = EWeaponState.ORDER_ARM;
		}
		else if (state == _PresentWeapon)
		{
			_weaponState = EWeaponState.PRESENT_ARM;
		}
		else if (state == _Shoot)
		{
			_weaponState = EWeaponState.SHOOT;
		}
		else if (state == _TriggerPull)
		{
			_weaponState = EWeaponState.TRIGGER_PULL;
		}
		else if (state == _CheckChamber)
		{
			_weaponState = EWeaponState.CHECK_CHAMBER;
		}
		else
		{
			_weaponState = EWeaponState.UNKNOWN_STATE;
		}
	}
}
