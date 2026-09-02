using System.Diagnostics;
using EFT.Communications;

namespace ThatsLit;

public struct ManagedStopWatch
{
	private Stopwatch sw;

	private string id;

	public ManagedStopWatch(string id)
	{
		this = default(ManagedStopWatch);
		this.id = id;
	}

	public void MaybeResume()
	{
		if (ThatsLitPlugin.EnableBenchmark.Value && ThatsLitPlugin.DebugInfo.Value)
		{
			if (sw == null)
			{
				sw = new Stopwatch();
			}
			if (sw.IsRunning)
			{
				string text = "[That's Lit] Benchmark stopwatch is not stopped! (" + id + ")";
				NotificationManager.DisplayWarningNotification(text, (ENotificationDurationType)0);
				Logger.LogWarning(text);
			}
			sw.Start();
		}
		else
		{
			sw = null;
		}
	}

	public void Stop()
	{
		sw?.Stop();
	}

	public float ConcludeMs()
	{
		if (sw == null)
		{
			return 0f;
		}
		long elapsedMilliseconds = sw.ElapsedMilliseconds;
		sw.Reset();
		return elapsedMilliseconds;
	}
}
