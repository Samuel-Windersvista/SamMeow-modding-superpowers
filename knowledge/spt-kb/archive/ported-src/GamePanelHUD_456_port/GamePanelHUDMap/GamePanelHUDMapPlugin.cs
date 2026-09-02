#if !UNITY_EDITOR

using System;
using System.IO;
using BepInEx;
using GamePanelHUDCore.Models;
using UnityEngine;
using IUpdate = KmyTarkovUtils.IUpdate;

namespace GamePanelHUDMap
{
    [BepInPlugin("com.kmyuhkyuk.GamePanelHUDMap", "GamePanelHUDMap", "3.4.0")]
    [BepInDependency("com.kmyuhkyuk.GamePanelHUDCore")]
    public class GamePanelHUDMapPlugin : BaseUnityPlugin, IUpdate
    {
        private HUDCoreModel HUDCore => HUDCoreModel.Instance;

        // PORT-NOTE: 4.1 移除了旧版通用 HUDClass<,> 机制,Map 数据直接存于插件字段
        internal static MapData MapDatas = new MapData();

        private string _mapPath;

        private bool _mapHudsw;

        private bool _hasMap;

        private string _infiltration;

        internal static Action<string> LoadMap;

        internal static Action UnloadMap;

        private void Awake()
        {
            HUDCore.LoadHUD("gamepanelmaphud.bundle", "gamepanelmaphud");
        }

        private void Start()
        {
            _mapPath = Path.Combine(HUDCore.ModPath, "map");

            HUDCore.UpdateManger.Register(this);
        }

        public void CustomUpdate()
        {
            MapPlugin();
        }

        private void MapPlugin()
        {
            _mapHudsw = HUDCore.AllHUDSw && _hasMap && !MapDatas.IsLoadMap && HUDCore.HasPlayer;

            if (HUDCore.HasPlayer)
            {
                _infiltration = HUDCore.YourPlayer.Infiltration;

                if (!_hasMap)
                {
                    LoadMap(Path.Combine(_mapPath, string.Concat(_infiltration, ".json")));

                    _hasMap = true;
                }

                MapDatas.PlayerPosition = HUDCore.YourPlayer.Position;

                MapDatas.PlayerRotation = HUDCore.YourPlayer.CameraPosition.eulerAngles;
            }
            else
            {
                UnloadMap();

                _hasMap = false;
            }
        }

        public class MapData
        {
            public Vector3 PlayerPosition;

            public Vector3 PlayerRotation;

            public bool IsLoadMap;
        }
    }
}

#endif
