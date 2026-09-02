using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using WTTServerCommonLib;

namespace Artem;

[Injectable(/*Could not decode attribute arguments.*/)]
public class WTTArtem(ModHelper modHelper, ImageRouter imageRouter, ConfigServer configServer, TimeUtil timeUtil, WTTArtemHelper wttArtemHelper, WTTServerCommonLib wttCommon) : IOnLoad
{
	[CompilerGenerated]
	private sealed class <OnLoad>d__8 : IAsyncStateMachine
	{
		public int <>1__state;

		public AsyncTaskMethodBuilder <>t__builder;

		public WTTArtem <>4__this;

		private string <pathToMod>5__1;

		private Assembly <assembly>5__2;

		private string <traderImagePath>5__3;

		private TraderBase <traderBase>5__4;

		private TraderAssort <assort>5__5;

		private TaskAwaiter <>u__1;

		private void MoveNext()
		{
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_012c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0131: Unknown result type (might be due to invalid IL or missing references)
			//IL_0138: Unknown result type (might be due to invalid IL or missing references)
			//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_02af: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0323: Unknown result type (might be due to invalid IL or missing references)
			//IL_0328: Unknown result type (might be due to invalid IL or missing references)
			//IL_0330: Unknown result type (might be due to invalid IL or missing references)
			//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_03da: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0211: Unknown result type (might be due to invalid IL or missing references)
			//IL_0271: Unknown result type (might be due to invalid IL or missing references)
			//IL_0276: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_0379: Unknown result type (might be due to invalid IL or missing references)
			//IL_0394: Unknown result type (might be due to invalid IL or missing references)
			//IL_0399: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_010f: Unknown result type (might be due to invalid IL or missing references)
			//IL_028b: Unknown result type (might be due to invalid IL or missing references)
			//IL_028d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0304: Unknown result type (might be due to invalid IL or missing references)
			//IL_0306: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
			int num = <>1__state;
			try
			{
				TaskAwaiter val5;
				TaskAwaiter val4;
				TaskAwaiter val3;
				TaskAwaiter val2;
				TaskAwaiter val;
				switch (num)
				{
				default:
					<pathToMod>5__1 = <>4__this.modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
					<assembly>5__2 = Assembly.GetExecutingAssembly();
					val5 = <>4__this.wttCommon.CustomItemServiceExtended.CreateCustomItems(<assembly>5__2, (string)null).GetAwaiter();
					if (!((TaskAwaiter)(ref val5)).IsCompleted)
					{
						num = (<>1__state = 0);
						<>u__1 = val5;
						<OnLoad>d__8 <OnLoad>d__9 = this;
						((AsyncTaskMethodBuilder)(ref <>t__builder)).AwaitUnsafeOnCompleted<TaskAwaiter, <OnLoad>d__8>(ref val5, ref <OnLoad>d__9);
						return;
					}
					goto IL_00d1;
				case 0:
					val5 = <>u__1;
					<>u__1 = default(TaskAwaiter);
					num = (<>1__state = -1);
					goto IL_00d1;
				case 1:
					val4 = <>u__1;
					<>u__1 = default(TaskAwaiter);
					num = (<>1__state = -1);
					goto IL_0147;
				case 2:
					val3 = <>u__1;
					<>u__1 = default(TaskAwaiter);
					num = (<>1__state = -1);
					goto IL_02c6;
				case 3:
					val2 = <>u__1;
					<>u__1 = default(TaskAwaiter);
					num = (<>1__state = -1);
					goto IL_033f;
				case 4:
					{
						val = <>u__1;
						<>u__1 = default(TaskAwaiter);
						num = (<>1__state = -1);
						break;
					}
					IL_033f:
					((TaskAwaiter)(ref val2)).GetResult();
					<assort>5__5 = <>4__this.modHelper.GetJsonDataFromFile<TraderAssort>(<pathToMod>5__1, "db/assort.json");
					<>4__this.wttArtemHelper.OverwriteTraderAssort(MongoId.op_Implicit(<traderBase>5__4.Id), <assort>5__5);
					val = global::System.Threading.Tasks.Task.CompletedTask.GetAwaiter();
					if (!((TaskAwaiter)(ref val)).IsCompleted)
					{
						num = (<>1__state = 4);
						<>u__1 = val;
						<OnLoad>d__8 <OnLoad>d__9 = this;
						((AsyncTaskMethodBuilder)(ref <>t__builder)).AwaitUnsafeOnCompleted<TaskAwaiter, <OnLoad>d__8>(ref val, ref <OnLoad>d__9);
						return;
					}
					break;
					IL_00d1:
					((TaskAwaiter)(ref val5)).GetResult();
					val4 = <>4__this.wttCommon.CustomQuestZoneService.CreateCustomQuestZones(<assembly>5__2, (string)null).GetAwaiter();
					if (!((TaskAwaiter)(ref val4)).IsCompleted)
					{
						num = (<>1__state = 1);
						<>u__1 = val4;
						<OnLoad>d__8 <OnLoad>d__9 = this;
						((AsyncTaskMethodBuilder)(ref <>t__builder)).AwaitUnsafeOnCompleted<TaskAwaiter, <OnLoad>d__8>(ref val4, ref <OnLoad>d__9);
						return;
					}
					goto IL_0147;
					IL_02c6:
					((TaskAwaiter)(ref val3)).GetResult();
					val2 = <>4__this.wttCommon.CustomClothingService.CreateCustomClothing(<assembly>5__2, (string)null).GetAwaiter();
					if (!((TaskAwaiter)(ref val2)).IsCompleted)
					{
						num = (<>1__state = 3);
						<>u__1 = val2;
						<OnLoad>d__8 <OnLoad>d__9 = this;
						((AsyncTaskMethodBuilder)(ref <>t__builder)).AwaitUnsafeOnCompleted<TaskAwaiter, <OnLoad>d__8>(ref val2, ref <OnLoad>d__9);
						return;
					}
					goto IL_033f;
					IL_0147:
					((TaskAwaiter)(ref val4)).GetResult();
					<traderImagePath>5__3 = Path.Combine(<pathToMod>5__1, "res/66bf757f27d0b097db0acea5.jpg");
					<traderBase>5__4 = <>4__this.modHelper.GetJsonDataFromFile<TraderBase>(<pathToMod>5__1, "db/base.json");
					<>4__this.imageRouter.AddRoute(<traderBase>5__4.Avatar.Replace(".jpg", ""), <traderImagePath>5__3);
					<>4__this.wttArtemHelper.SetTraderUpdateTime(<>4__this._traderConfig, <traderBase>5__4, <>4__this.timeUtil.GetHoursAsSeconds(1), <>4__this.timeUtil.GetHoursAsSeconds(2));
					<>4__this._ragfairConfig.Traders.TryAdd(<traderBase>5__4.Id, true);
					<>4__this.wttArtemHelper.AddTraderWithEmptyAssortToDb(<traderBase>5__4);
					<>4__this.wttArtemHelper.AddTraderToLocales(<traderBase>5__4, "Artem", "[REDACTED]");
					val3 = <>4__this.wttCommon.CustomQuestService.CreateCustomQuests(<assembly>5__2, (string)null).GetAwaiter();
					if (!((TaskAwaiter)(ref val3)).IsCompleted)
					{
						num = (<>1__state = 2);
						<>u__1 = val3;
						<OnLoad>d__8 <OnLoad>d__9 = this;
						((AsyncTaskMethodBuilder)(ref <>t__builder)).AwaitUnsafeOnCompleted<TaskAwaiter, <OnLoad>d__8>(ref val3, ref <OnLoad>d__9);
						return;
					}
					goto IL_02c6;
				}
				((TaskAwaiter)(ref val)).GetResult();
			}
			catch (global::System.Exception exception)
			{
				<>1__state = -2;
				<pathToMod>5__1 = null;
				<assembly>5__2 = null;
				<traderImagePath>5__3 = null;
				<traderBase>5__4 = null;
				<assort>5__5 = null;
				((AsyncTaskMethodBuilder)(ref <>t__builder)).SetException(exception);
				return;
			}
			<>1__state = -2;
			<pathToMod>5__1 = null;
			<assembly>5__2 = null;
			<traderImagePath>5__3 = null;
			<traderBase>5__4 = null;
			<assort>5__5 = null;
			((AsyncTaskMethodBuilder)(ref <>t__builder)).SetResult();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
		}
	}

	private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();

	private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();

	[AsyncStateMachine(typeof(<OnLoad>d__8))]
	[DebuggerStepThrough]
	public global::System.Threading.Tasks.Task OnLoad()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		<OnLoad>d__8 <OnLoad>d__9 = new <OnLoad>d__8();
		<OnLoad>d__9.<>t__builder = AsyncTaskMethodBuilder.Create();
		<OnLoad>d__9.<>4__this = this;
		<OnLoad>d__9.<>1__state = -1;
		((AsyncTaskMethodBuilder)(ref <OnLoad>d__9.<>t__builder)).Start<<OnLoad>d__8>(ref <OnLoad>d__9);
		return ((AsyncTaskMethodBuilder)(ref <OnLoad>d__9.<>t__builder)).Task;
	}
}
