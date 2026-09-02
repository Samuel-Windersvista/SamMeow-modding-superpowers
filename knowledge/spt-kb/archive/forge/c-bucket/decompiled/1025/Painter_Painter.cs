using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using WTTServerCommonLib;

namespace Painter;

[Injectable(/*Could not decode attribute arguments.*/)]
public class Painter(ISptLogger<Painter> logger, ModHelper modHelper, ImageRouter imageRouter, ConfigServer configServer, TimeUtil timeUtil, EpicTraderHelper traderHelper, WTTServerCommonLib wttCommon) : IOnLoad
{
	[StructLayout((LayoutKind)3)]
	[CompilerGenerated]
	private struct <OnLoad>d__9 : IAsyncStateMachine
	{
		public int <>1__state;

		public AsyncTaskMethodBuilder <>t__builder;

		public Painter <>4__this;

		private TaskAwaiter <>u__1;

		private void MoveNext()
		{
			//IL_0150: Unknown result type (might be due to invalid IL or missing references)
			//IL_0155: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0189: Unknown result type (might be due to invalid IL or missing references)
			//IL_018e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0135: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			int num = <>1__state;
			Painter painter = <>4__this;
			try
			{
				TaskAwaiter val;
				TaskAwaiter val2;
				if (num != 0)
				{
					if (num == 1)
					{
						val = <>u__1;
						<>u__1 = default(TaskAwaiter);
						num = (<>1__state = -1);
						goto IL_01d7;
					}
					Assembly executingAssembly = Assembly.GetExecutingAssembly();
					string absolutePathToModFolder = painter.modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
					string text = Path.Combine(absolutePathToModFolder, "res/painter.jpg");
					TraderBase jsonDataFromFile = painter.modHelper.GetJsonDataFromFile<TraderBase>(absolutePathToModFolder, "db/base.json");
					painter.imageRouter.AddRoute(jsonDataFromFile.Avatar.Replace(".jpg", ""), text);
					painter.traderHelper.SetTraderUpdateTime(painter._traderConfig, jsonDataFromFile, painter.timeUtil.GetHoursAsSeconds(1), painter.timeUtil.GetHoursAsSeconds(2));
					painter._ragfairConfig.Traders.TryAdd(jsonDataFromFile.Id, true);
					painter.traderHelper.AddTraderWithEmptyAssortToDb(jsonDataFromFile);
					painter.traderHelper.AddTraderToLocales(jsonDataFromFile, "Painter", "Painter is a trader who sells unique weapon mods and gear. Also painted weapon parts (incl. whole weapons).");
					TraderAssort jsonDataFromFile2 = painter.modHelper.GetJsonDataFromFile<TraderAssort>(absolutePathToModFolder, "db/assort.json");
					painter.traderHelper.OverwriteTraderAssort(MongoId.op_Implicit(jsonDataFromFile.Id), jsonDataFromFile2);
					val2 = painter.wttCommon.CustomQuestService.CreateCustomQuests(executingAssembly, (string)null).GetAwaiter();
					if (!((TaskAwaiter)(ref val2)).IsCompleted)
					{
						num = (<>1__state = 0);
						<>u__1 = val2;
						((AsyncTaskMethodBuilder)(ref <>t__builder)).AwaitUnsafeOnCompleted<TaskAwaiter, <OnLoad>d__9>(ref val2, ref this);
						return;
					}
				}
				else
				{
					val2 = <>u__1;
					<>u__1 = default(TaskAwaiter);
					num = (<>1__state = -1);
				}
				((TaskAwaiter)(ref val2)).GetResult();
				painter.logger.Success("[Painter] Mod loaded successfully.", (global::System.Exception)null);
				val = global::System.Threading.Tasks.Task.CompletedTask.GetAwaiter();
				if (!((TaskAwaiter)(ref val)).IsCompleted)
				{
					num = (<>1__state = 1);
					<>u__1 = val;
					((AsyncTaskMethodBuilder)(ref <>t__builder)).AwaitUnsafeOnCompleted<TaskAwaiter, <OnLoad>d__9>(ref val, ref this);
					return;
				}
				goto IL_01d7;
				IL_01d7:
				((TaskAwaiter)(ref val)).GetResult();
			}
			catch (global::System.Exception exception)
			{
				<>1__state = -2;
				((AsyncTaskMethodBuilder)(ref <>t__builder)).SetException(exception);
				return;
			}
			<>1__state = -2;
			((AsyncTaskMethodBuilder)(ref <>t__builder)).SetResult();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			((AsyncTaskMethodBuilder)(ref <>t__builder)).SetStateMachine(stateMachine);
		}
	}

	private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();

	private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();

	[AsyncStateMachine(typeof(<OnLoad>d__9))]
	public global::System.Threading.Tasks.Task OnLoad()
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		<OnLoad>d__9 <OnLoad>d__10 = default(<OnLoad>d__9);
		<OnLoad>d__10.<>t__builder = AsyncTaskMethodBuilder.Create();
		<OnLoad>d__10.<>4__this = this;
		<OnLoad>d__10.<>1__state = -1;
		((AsyncTaskMethodBuilder)(ref <OnLoad>d__10.<>t__builder)).Start<<OnLoad>d__9>(ref <OnLoad>d__10);
		return ((AsyncTaskMethodBuilder)(ref <OnLoad>d__10.<>t__builder)).Task;
	}
}
