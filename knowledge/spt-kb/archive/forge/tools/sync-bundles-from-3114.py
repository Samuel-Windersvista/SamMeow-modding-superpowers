import pathlib, sys, shutil

sys.stdout.reconfigure(encoding='utf-8')

SRC = pathlib.Path(r'E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2\mods')
DST = pathlib.Path(r'E:\Game\EFT_Offline\Inescapable Tarkov\mods')

def copy_file(src_file, dst_file):
    dst_file.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(str(src_file), str(dst_file))
    print(f'[OK] {dst_file.relative_to(DST)} ({src_file.stat().st_size//1024} KB)')

# 1. StashSearch
s = SRC / '[10]仓库搜索框-StashSearch' / 'BepInEx' / 'plugins' / 'StashSearch' / 'stashsearch.bundle'
d = DST / '[10]仓库搜索框-StashSearch' / 'BepInEx' / 'plugins' / 'StashSearch' / 'stashsearch.bundle'
if s.exists():
    copy_file(s, d)
else:
    print('[MISS] StashSearch bundle 源不存在')

# 2. accessibilityindicators（bundle 在 assets 子目录）
sa = SRC / '[11]声音可视化-accessibilityindicators' / 'BepInEx' / 'plugins' / 'acidphantasm-accessibilityindicators' / 'assets'
da = DST / '[11]声音可视化-accessibilityindicators' / 'BepInEx' / 'plugins' / 'assets'
if sa.exists():
    for f in sa.rglob('*'):
        if f.is_file():
            copy_file(f, da / f.name)
else:
    print('[MISS] accessibilityindicators assets 源不存在')

# 3. GamePanelHUD（6 个 bundle）
sg = SRC / '[11]更多界面信息-GamePanelHUD' / 'BepInEx' / 'plugins' / 'kmyuhkyuk-GamePanelHUD' / 'bundles'
dg = DST / '[11]更多界面信息-GamePanelHUD' / 'BepInEx' / 'plugins' / 'kmyuhkyuk-GamePanelHUD' / 'bundles'
if sg.exists():
    for f in sg.glob('*.bundle'):
        copy_file(f, dg / f.name)
else:
    print('[MISS] GamePanelHUD bundles 源不存在')

# 4. hideoutcat：只复制 AssetBundleLoader.dll + bundles/ + CatNodeGraph.json（不覆盖 4.1 版 hideoutcat.bepinex.dll）
st = SRC / '[2]新物品-藏身处猫咪-hideoutcat' / 'BepInEx' / 'plugins' / 'tarkin'
dt = DST / '[2]新物品-藏身处猫咪-hideoutcat' / 'BepInEx' / 'plugins' / 'tarkin'
if st.exists():
    abl = st / 'AssetBundleLoader.dll'
    if abl.exists():
        copy_file(abl, dt / 'AssetBundleLoader.dll')
    sb = st / 'bundles'
    if sb.exists():
        for f in sb.rglob('*'):
            if f.is_file():
                copy_file(f, dt / 'bundles' / f.name)
else:
    print('[MISS] hideoutcat tarkin 源不存在')

print()
print('=== 完成 ===')
