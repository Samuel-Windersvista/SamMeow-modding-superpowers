using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace ThatsLit;

public class PlayerLitScoreProfile : IDisposable
{
	public struct CountPixelsJob : IJobParallelFor, IDisposable
	{
		[NativeSetThreadIndex]
		public int threadIndex;

		[ReadOnly]
		public NativeArray<Color32> tex;

		[WriteOnly]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<int> counted;

		[WriteOnly]
		[NativeDisableContainerSafetyRestriction]
		public NativeArray<float> lum;

		[ReadOnly]
		public NativeArray<float> thresholds;

		public void Dispose()
		{
			if (tex.IsCreated)
			{
				tex.Dispose();
			}
			if (counted.IsCreated)
			{
				counted.Dispose();
			}
			if (lum.IsCreated)
			{
				lum.Dispose();
			}
			if (thresholds.IsCreated)
			{
				thresholds.Dispose();
			}
		}

		public void Execute(int index)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			Color32 val = tex[index];
			if (!((UnityEngine.Color)(val) == Color.white) && !((float)(int)val.a <= 0.5f))
			{
				float num = (float)(val.r + val.g + val.b) / 765f;
				int num2 = threadIndex * 8;
				ref NativeArray<float> reference = ref lum;
				int num3 = num2;
				reference[num3] += num;
				ref NativeArray<int> reference2 = ref counted;
				if (num < thresholds[5])
				{
					reference2 = ref counted;
					num3 = num2 + 6;
					reference2[num3] += 1;
					reference = ref lum;
					num3 = num2 + 1;
					reference[num3] += num;
				}
				else if (num >= thresholds[0])
				{
					reference2 = ref counted;
					num3 = num2;
					reference2[num3] += 1;
				}
				else if (num >= thresholds[1])
				{
					reference2 = ref counted;
					num3 = num2 + 1;
					reference2[num3] += 1;
				}
				else if (num >= thresholds[2])
				{
					reference2 = ref counted;
					num3 = num2 + 2;
					reference2[num3] += 1;
				}
				else if (num >= thresholds[3])
				{
					reference2 = ref counted;
					num3 = num2 + 3;
					reference2[num3] += 1;
				}
				else if (num >= thresholds[4])
				{
					reference2 = ref counted;
					num3 = num2 + 4;
					reference2[num3] += 1;
				}
				else if (num >= thresholds[5])
				{
					reference2 = ref counted;
					num3 = num2 + 5;
					reference2[num3] += 1;
				}
				reference2 = ref counted;
				num3 = num2 + 7;
				reference2[num3] += 1;
			}
		}
	}

	internal float lum3s;

	internal float lum1s;

	internal float lum10s;

	public FrameStats frame0;

	public FrameStats frame1;

	public FrameStats frame2;

	public FrameStats frame3;

	public FrameStats frame4;

	public FrameStats frame5;

	internal float detailBonusSmooth;

	internal float litScoreFactor;

	public bool IsProxy { get; set; }

	public ThatsLitPlayer Player { get; }

	public CountPixelsJob PixelCountingJob { get; set; }

	public JobHandle CountingJobHandle { get; set; }

	internal object ScoreCalcData { get; set; }

	public PlayerLitScoreProfile(ThatsLitPlayer player)
	{
		Player = player;
	}

	internal void UpdateLumTrackers(float avgLumMultiFrames)
	{
		lum1s = Mathf.Lerp(lum1s, avgLumMultiFrames, Time.deltaTime);
		lum3s = Mathf.Lerp(lum3s, avgLumMultiFrames, Time.deltaTime / 3f);
		lum10s = Mathf.Lerp(lum10s, avgLumMultiFrames, Time.deltaTime / 10f);
	}

	internal float FindHighestAvgLumRecentFrame(bool includeThis, float thisframe)
	{
		float num = (includeThis ? thisframe : frame1.avgLum);
		if (frame1.avgLum > num)
		{
			num = frame1.avgLum;
		}
		if (frame2.avgLum > num)
		{
			num = frame2.avgLum;
		}
		if (frame3.avgLum > num)
		{
			num = frame3.avgLum;
		}
		if (frame4.avgLum > num)
		{
			num = frame4.avgLum;
		}
		if (frame5.avgLum > num)
		{
			num = frame5.avgLum;
		}
		return num;
	}

	internal float FindLowestAvgLumRecentFrame(bool includeThis, float calculating)
	{
		float num = (includeThis ? calculating : frame1.avgLum);
		if (frame1.avgLum < num)
		{
			num = frame1.avgLum;
		}
		if (frame2.avgLum < num)
		{
			num = frame2.avgLum;
		}
		if (frame3.avgLum < num)
		{
			num = frame3.avgLum;
		}
		if (frame4.avgLum < num)
		{
			num = frame4.avgLum;
		}
		if (frame5.avgLum < num)
		{
			num = frame5.avgLum;
		}
		return num;
	}

	internal float FindHighestMFAvgLumRecentFrame(bool includeThis, float thisframe)
	{
		float num = (includeThis ? thisframe : frame1.avgLumMultiFrames);
		if (frame1.avgLumMultiFrames > num)
		{
			num = frame1.avgLumMultiFrames;
		}
		if (frame2.avgLumMultiFrames > num)
		{
			num = frame2.avgLumMultiFrames;
		}
		if (frame3.avgLumMultiFrames > num)
		{
			num = frame3.avgLumMultiFrames;
		}
		if (frame4.avgLumMultiFrames > num)
		{
			num = frame4.avgLumMultiFrames;
		}
		if (frame5.avgLumMultiFrames > num)
		{
			num = frame5.avgLumMultiFrames;
		}
		return num;
	}

	internal float FindLowestMFAvgLumRecentFrame(bool includeThis, float calculating)
	{
		float num = (includeThis ? calculating : frame1.avgLumMultiFrames);
		if (frame1.avgLumMultiFrames < num)
		{
			num = frame1.avgLumMultiFrames;
		}
		if (frame2.avgLumMultiFrames < num)
		{
			num = frame2.avgLumMultiFrames;
		}
		if (frame3.avgLumMultiFrames < num)
		{
			num = frame3.avgLumMultiFrames;
		}
		if (frame4.avgLumMultiFrames < num)
		{
			num = frame4.avgLumMultiFrames;
		}
		if (frame5.avgLumMultiFrames < num)
		{
			num = frame5.avgLumMultiFrames;
		}
		return num;
	}

	internal float FindHighestScoreRecentFrame(bool includeThis, float calculating)
	{
		float num = (includeThis ? calculating : frame1.score);
		if (frame1.score > num)
		{
			num = frame1.score;
		}
		if (frame2.score > num)
		{
			num = frame2.score;
		}
		if (frame3.score > num)
		{
			num = frame3.score;
		}
		if (frame4.score > num)
		{
			num = frame4.score;
		}
		if (frame5.score > num)
		{
			num = frame5.score;
		}
		return num;
	}

	internal float FindLowestScoreRecentFrame(bool includeThis, float calculating)
	{
		float num = (includeThis ? calculating : frame1.score);
		if (frame1.score < num)
		{
			num = frame1.score;
		}
		if (frame2.score < num)
		{
			num = frame2.score;
		}
		if (frame3.score < num)
		{
			num = frame3.score;
		}
		if (frame4.score < num)
		{
			num = frame4.score;
		}
		if (frame5.score < num)
		{
			num = frame5.score;
		}
		return num;
	}

	public void Dispose()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		JobHandle countingJobHandle = CountingJobHandle;
		countingJobHandle.Complete();
		PixelCountingJob.Dispose();
		PixelCountingJob = default(CountPixelsJob);
	}
}
