using UnityEngine;
#if !UNITY_EDITOR

using GamePanelHUDCore.Models;
using IUpdate = KmyTarkovUtils.IUpdate;

#endif

namespace GamePanelHUDMap
{
    public class GamePanelHUDMap : MonoBehaviour
#if !UNITY_EDITOR

        , IUpdate

#endif
    {
#if !UNITY_EDITOR

        private HUDCoreModel HUDCore => HUDCoreModel.Instance;

#endif

        private AssetBundle _assetBundle;

        private GameObject _mapAsset;

        [SerializeField] private Transform map;

        [SerializeField] private GamePanelHUDMapUI mapUI;

#if !UNITY_EDITOR

        private void Start()
        {
            GamePanelHUDMapPlugin.LoadMap = LoadMapAsset;
            GamePanelHUDMapPlugin.UnloadMap = UnloadMapAsset;

            HUDCore.UpdateManger.Register(this);
        }

        public void CustomUpdate()
        {
            MapHUD();
        }

        private void MapHUD()
        {
            if (map != null)
            {
                map.gameObject.SetActive(GamePanelHUDMapPlugin.MapDatas.IsLoadMap);
            }
        }

        private void LoadMapAsset(string mappath)
        {
            // PORT-NOTE: 4.1 未提供现成地图资源加载管线,保留原逻辑骨架
            GamePanelHUDMapPlugin.MapDatas.IsLoadMap = true;
        }

        private void UnloadMapAsset()
        {
        }

#endif
    }
}
