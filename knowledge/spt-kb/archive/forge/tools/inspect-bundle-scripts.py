import pathlib, sys
sys.stdout.reconfigure(encoding='utf-8')

try:
    import UnityPy
except ImportError:
    print('UnityPy 未安装')
    sys.exit(1)

# 检查 UI bundle 的 MonoBehaviour script 引用
bundles = [
    (r'E:\Game\EFT_Offline\Inescapable Tarkov\mods\[10]仓库搜索框-StashSearch\BepInEx\plugins\StashSearch\stashsearch.bundle', 'StashSearch'),
    (r'E:\Game\EFT_Offline\Inescapable Tarkov\mods\[11]声音可视化-accessibilityindicators\BepInEx\plugins\assets\accessibilityindicators.bundle', 'accessibility'),
    (r'E:\Game\EFT_Offline\Inescapable Tarkov\mods\[11]更多界面信息-GamePanelHUD\BepInEx\plugins\kmyuhkyuk-GamePanelHUD\bundles\gamepanelhealthhud.bundle', 'GamePanelHUD'),
    (r'E:\Game\EFT_Offline\Inescapable Tarkov\mods\[8]坠落的补给直升机-HeliCrash.ArysReloaded\BepInEx\plugins\SamSWAT.HeliCrash.ArysReloaded\sikorsky_uh60_blackhawk.bundle', 'HeliCrash'),
]

for path, tag in bundles:
    print(f'=== {tag}: {pathlib.Path(path).name} ===')
    try:
        env = UnityPy.load(path)
        mono_scripts = set()
        asset_types = {}
        for obj in env.objects:
            try:
                tree = obj.read_typetree()
            except Exception:
                continue
            tname = obj.type.name if obj.type else '?'
            asset_types[tname] = asset_types.get(tname, 0) + 1
            if tname == 'MonoBehaviour':
                script = tree.get('m_Script', {})
                if isinstance(script, dict):
                    fname = script.get('m_FileID', '?')
                    ppath = script.get('m_PathID', '?')
                    mono_scripts.add(f'fileID={fname} pathID={ppath}')
                # 尝试读 m_Script 引用的脚本名（需要 resolve）
            elif tname == 'MonoScript':
                mono_scripts.add(f'{tree.get("m_ClassName", "?")} @ {tree.get("m_AssemblyName", "?")} | ns={tree.get("m_Namespace", "")}')
            elif tname in ('Texture2D', 'Sprite', 'AudioClip', 'Shader', 'Material', 'GameObject', 'Transform', 'RectTransform', 'NavMeshData'):
                pass
        print(f'  类型分布: {dict(sorted(asset_types.items(), key=lambda x: -x[1])[:12])}')
        print(f'  MonoScript 引用:')
        for s in sorted(mono_scripts)[:20]:
            print(f'    {s}')
        if not mono_scripts:
            print('    (无 MonoBehaviour/MonoScript 引用 - 纯数据 bundle)')
    except Exception as e:
        print(f'  读取失败: {e}')
    print()
