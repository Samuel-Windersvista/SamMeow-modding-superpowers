import pathlib, sys, struct, hashlib, json

sys.stdout.reconfigure(encoding='utf-8')

mo2 = pathlib.Path(r'E:\Game\EFT_Offline\Inescapable Tarkov\mods')

def bundle_header(path):
    with open(path, 'rb') as f:
        head = f.read(128)
    if head[:7] != b'UnityFS':
        return {'format': 'NOT-UnityFS', 'unity': '?', 'fs': '?', 'comp': '?'}
    fs = struct.unpack('<I', head[7:11])[0]
    import re
    s = head.decode('utf-8', errors='ignore')
    m = re.search(r'(\d+\.\d+\.\d+[0-9a-f]*)\x00', s)
    unity = m.group(1) if m else '?'
    flags = head[11]
    comp = {0: 'none', 1: 'lzma', 2: 'lz4', 3: 'lz4hc'}.get(flags & 0x3F, f'flags={flags}')
    return {'format': 'UnityFS', 'unity': unity, 'fs': fs, 'comp': comp}

# 逐 mod 扫描所有 bundle（含 SPT_Runtime 路径）
report = {}
for mod in sorted(mo2.iterdir()):
    if not mod.is_dir():
        continue
    bundles = []
    for p in mod.rglob('*'):
        if p.is_file() and p.suffix.lower() in ('.bundle', '.unity3d'):
            rel = str(p.relative_to(mod))
            size_kb = p.stat().st_size // 1024
            h = bundle_header(p)
            bundles.append({'path': rel, 'kb': size_kb, **h})
    if bundles:
        report[mod.name] = bundles

# 输出汇总
print(f'=== 全部 overlay bundle 逐 mod 检查表（{sum(len(v) for v in report.values())} 个）===')
for mod, bundles in sorted(report.items()):
    unities = sorted(set(b['unity'] for b in bundles))
    total_kb = sum(b['kb'] for b in bundles)
    print(f'\n{mod}: {len(bundles)} 个, {total_kb//1024} MB, Unity版本={unities}')
    for b in bundles:
        flag = '' if b['format'] == 'UnityFS' else ' <-- 非UnityFS!'
        print(f'    [{b["kb"]} KB] {b["unity"]} {b["comp"]} {b["path"]}{flag}')

# 保存 JSON 供后续深读
out = pathlib.Path(r'D:\Temp\opencode\bundle-inventory.json')
out.write_text(json.dumps(report, ensure_ascii=False, indent=1), encoding='utf-8')
print(f'\n清单已保存: {out}')
