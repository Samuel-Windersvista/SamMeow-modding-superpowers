import pathlib, sys, json

sys.stdout.reconfigure(encoding='utf-8')

mo2 = pathlib.Path(r'E:\Game\EFT_Offline\Inescapable Tarkov\mods')
pack = pathlib.Path(r'E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2\mods')

# 已部署 overlay 名 -> 原包同名目录 bundle 数
print('=== 已部署 overlay vs 原包同名目录 bundle 对照 ===')
print('（检查：原包有 bundle 的 mod，我们 overlay 是否也带上了）')
for mod in sorted(mo2.iterdir()):
    if not mod.is_dir():
        continue
    name = mod.name
    # overlay 自己的 bundle
    own = [p for p in mod.rglob('*') if p.is_file() and p.suffix.lower() in ('.bundle', '.unity3d')]
    # 原包同名目录
    pack_mod = pack / name
    pack_bundles = []
    if pack_mod.exists():
        pack_bundles = [p for p in pack_mod.rglob('*') if p.is_file() and p.suffix.lower() in ('.bundle', '.unity3d')]
    if pack_bundles and not own:
        # 原包有 bundle 但 overlay 没有 —— 遗漏！
        total_kb = sum(p.stat().st_size for p in pack_bundles) // 1024
        print(f'  [遗漏!] {name}: 原包 {len(pack_bundles)} 个 bundle ({total_kb//1024} MB), overlay 无 bundle')
        # 显示几个样例路径
        for p in list(pack_bundles)[:3]:
            print(f'       e.g. {p.relative_to(pack_mod)}')
    elif pack_bundles and own:
        # 都有，但数量可能不同
        marker = 'OK' if len(pack_bundles) == len(own) else f'数量不同(原包{len(pack_bundles)}/overlay{len(own)})'
        print(f'  [{marker}] {name}: 原包 {len(pack_bundles)} / overlay {len(own)}')
