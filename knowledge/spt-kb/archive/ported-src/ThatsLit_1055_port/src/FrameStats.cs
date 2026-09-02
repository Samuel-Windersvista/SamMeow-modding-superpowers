namespace ThatsLit;

public struct FrameStats
{
	public int pxS;

	public int pxH;

	public int pxHM;

	public int pxM;

	public int pxML;

	public int pxL;

	public int pxD;

	public int brighterPixels;

	public int darkerPixels;

	public float lum;

	public float avgLum;

	public float avgLumMultiFrames;

	public float avgLumNonDark;

	public int pixels;

	public float score;

	public float ambienceScore;

	public float baseAmbienceScore;

	public float multiFrameLitScore;

	public float cloudiness;

	public float RatioShinePixels => (float)pxS / (float)pixels;

	public float RatioHighPixels => (float)pxH / (float)pixels;

	public float RatioHighMidPixels => (float)pxHM / (float)pixels;

	public float RatioMidPixels => (float)pxM / (float)pixels;

	public float RatioMidLowPixels => (float)pxML / (float)pixels;

	public float RatioLowPixels => (float)pxL / (float)pixels;

	public float RatioDarkPixels => (float)pxD / (float)pixels;

	public float RatioLowAndDarkPixels => (float)(pxL + pxD) / (float)pixels;

	public int BrighterPixels => pxS + pxH + pxHM + pxM / 2;
}
