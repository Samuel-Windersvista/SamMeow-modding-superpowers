import pathlib, sys, struct

sys.stdout.reconfigure(encoding='utf-8')

# 1. 从 UnityPlayer.dll 读版本信息
import subprocess
ps = r'''
$f = 'E:\Game\EFT_Offline\SPT_410\UnityPlayer.dll'
(Get-Item -LiteralPath $f).VersionInfo | Select-Object FileVersion, ProductVersion | Format-List
'''
r = subprocess.run(['powershell', '-NoProfile', '-Command', ps], capture_output=True, text=True, encoding='utf-8', errors='replace')
print('=== UnityPlayer.dll 版本 ===')
print(r.stdout)

# 2. 读 bundle 头版本（Unity bundle 头: 20 字节签名 + 版本 uint32 在 offset 4 附近）
# UnityFS bundle 头格式: "UnityFS\0" + version(uint32) + 其他
print('=== bundle 头检查 ===')
mo2 = pathlib.Path(r'E:\Game\EFT_Offline\Inescapable Tarkov\mods')
count = 0
for mod in sorted(mo2.iterdir()):
    if not mod.is_dir():
        continue
    for p in mod.rglob('*'):
        if p.is_file() and p.suffix.lower() in ('.bundle', '.unity3d'):
            with open(p, 'rb') as f:
                head = f.read(64)
            if head[:7] == b'UnityFS':
                ver = struct.unpack('<I', head[7:11])[0]
                # 找 unity version 字符串
                unity_ver = '?'
                try:
                    s = head.decode('utf-8', errors='ignore')
                    import re
                    m = re.search(r'(\d+\.\d+\.\d+[0-9a-f]*)', s)
                    if m:
                        unity_ver = m.group(1)
                except Exception:
                    pass
                print(f'  UnityFS v{ver} {unity_ver} [{p.stat().st_size//1024} KB] {p.relative_to(mo2)}')
                count += 1
            else:
                print(f'  [非UnityFS] {p.relative_to(mo2)}')
print(f'共 {count} 个 UnityFS bundle')
