import pathlib, sys
sys.stdout.reconfigure(encoding='utf-8')

import UnityPy

bundles = [
    (r'E:\Game\EFT_Offline\Inescapable Tarkov\mods\[1]前置框架-KmyTarkovApi\BepInEx\plugins\kmyuhkyuk-KmyTarkovApi\bundles\kmytarkovconfiguration.bundle', 'KmyTarkovApi-config'),
    (r'E:\Game\EFT_Offline\Inescapable Tarkov\mods\[7]平衡的夜视装备视效-BRNVG服务器文件\user\mods\BRNVG_N-15Adapter\bundles\assets\content\items\equipment\customizable\nvg_armasight_n-15\nvg_armasight_n-15.bundle', 'BRNVG-armasight'),
    (r'E:\Game\EFT_Offline\Inescapable Tarkov\mods\[7]平衡的夜视装备视效-BRNVG服务器文件\user\mods\BRNVG_N-15Adapter\bundles\assets\content\items\equipment\nvg_pvs14\pvs14_textures.bundle', 'BRNVG-pvs14-textures'),
]

for path, tag in bundles:
    print(f'=== {tag}: {pathlib.Path(path).name} ===')
    try:
        env = UnityPy.load(path)
        from collections import Counter
        types = Counter()
        monoscripts = set()
        shaders = set()
        for obj in env.objects:
            tname = obj.type.name if obj.type else '?'
            types[tname] += 1
            if tname == 'MonoScript':
                try:
                    tree = obj.read_typetree()
                    monoscripts.add(f'{tree.get("m_ClassName","?")} @ {tree.get("m_AssemblyName","?")}')
                except Exception:
                    pass
            elif tname == 'Shader':
                shaders.add('有内嵌shader')
        print(f'  类型: {dict(types.most_common(10))}')
        if monoscripts:
            print('  MonoScript 外部引用:')
            for s in sorted(monoscripts)[:10]:
                print(f'    {s}')
        else:
            print('  MonoScript: 无')
        print(f'  Shader: {shaders if shaders else "无内嵌（引用外部）"}')
    except Exception as e:
        print(f'  读取失败: {e}')
    print()
