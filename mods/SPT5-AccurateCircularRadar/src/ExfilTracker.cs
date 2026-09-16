using EFT;
using EFT.Interactive;
using System.Collections.Generic;

namespace Radar
{
    /// <summary>
    /// Pins the extracts available to this player, plus every transit, to the rim of the radar.
    /// </summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配：<c>LocationScene.GetAllObjects&lt;T&gt;()</c> 返回 Il2Cpp 序列，去掉了
    /// 托管 LINQ；<c>ExfiltrationPoint.Id</c> 仍是 <c>MongoID</c>，经其隐式 string 转换传给
    /// <see cref="BlipOther"/>。
    /// </remarks>
    internal sealed class ExfilTracker
    {
        private readonly Player _player;
        private readonly List<BlipOther> _blips = new List<BlipOther>();

        public ExfilTracker(Player player)
        {
            _player = player;
        }

        public void Rebuild()
        {
            Clear();

            if (!RadarConfig.ExfilEnabled.Value)
                return;

            foreach (ExfiltrationPoint point in LocationScene.GetAllObjects<ExfiltrationPoint>())
            {
                if (point == null)
                    continue;

                // Extracts are side and spawn specific; only show the ones this player can actually use.
                if (!point.InfiltrationMatch(_player))
                    continue;

                RadarPlugin.Log.LogDebug($"Exfil {point.Settings.Name} ({point.Status}) at {point.transform.position}");
                _blips.Add(new BlipOther(point.Id, point.transform, false, BlipKind.Exfil));
            }

            foreach (TransitPoint transit in LocationScene.GetAllObjects<TransitPoint>())
            {
                if (transit == null)
                    continue;

                RadarPlugin.Log.LogDebug($"Transit {transit.name} (enabled: {transit.Enabled}) at {transit.transform.position}");
                _blips.Add(new BlipOther(transit.name, transit.transform, false, BlipKind.Transit));
            }
        }

        public void Clear()
        {
            foreach (BlipOther blip in _blips)
                blip.DestroyBlip();

            _blips.Clear();
        }

        public void Render()
        {
            foreach (BlipOther blip in _blips)
                blip.Update();
        }
    }
}
