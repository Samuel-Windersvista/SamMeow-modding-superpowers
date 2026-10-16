# -*- coding: utf-8 -*-
"""
softcore416 数据对齐枚举（只读分析）。
机械执行：BASE 基础库 / MOD 包内 CustomItems 注入 / 我方数据表 三类 id 对齐。
不修改任何被分析文件；仅写报告到 D:\\Temp\\opencode\\。
"""
import io, sys, json, os, re

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

BASE_ITEMS = r"E:\Game\EFT_Offline\SPT_41x\SPT_Runtime\SPT_Data\database\templates\items.json"
MOD_ROOT   = r"E:\Game\EFT_Offline\Norvinsk_Forever\mods"
DATA_ROOT  = r"E:\云文件\GitHub\SamMeow-modding-superpowers\mods\InescapableTarkovsSoftcore416\data"
REPORT     = r"D:\Temp\opencode\softcore416_align_report.txt"

HEX24 = re.compile(r'[0-9a-fA-F]{24}')
BAG_PARENT = "5448e53e4bdc2d60728b4567"   # Backpack
ARM_PARENT = "5b3f15d486f77432d0509248"   # ArmBand

OUT = []
def w(s=""):
    OUT.append(s)

def say(s=""):
    print(s)

# ----------------------------------------------------------------------------
# 1. 载入 BASE
# ----------------------------------------------------------------------------
with open(BASE_ITEMS, encoding='utf-8') as fh:
    base = json.load(fh)
BASE = set(base.keys())
base_name = {k: (v.get('_name') or '') for k, v in base.items()}
base_parent = {k: (v.get('_parent') or '') for k, v in base.items()}
base_kids_bag = {k for k in BASE if base_parent.get(k) == BAG_PARENT}
base_kids_arm = {k for k in BASE if base_parent.get(k) == ARM_PARENT}

# ----------------------------------------------------------------------------
# 2. 载入 MOD (CustomItems)
# ----------------------------------------------------------------------------
mod_provider = {}      # id -> [rel file, ...]
mod_meta = {}          # id -> value dict
file_key_counts = {}   # rel -> top-level key count
parse_failures = []    # (rel, reason)
skipped_large = []     # (rel, size)
large_parsed = []      # (rel, size)
mod_files_scanned = 0

for dp, dn, fn in os.walk(MOD_ROOT):
    for f in fn:
        full = os.path.join(dp, f)
        if not f.lower().endswith('.json'):
            continue
        if 'customitems' not in full.replace('\\', '/').lower():
            continue
        rel = os.path.relpath(full, MOD_ROOT)
        mod_files_scanned += 1
        size = os.path.getsize(full)
        if size > 50 * 1024 * 1024:
            skipped_large.append((rel, size))
            continue
        if size > 10 * 1024 * 1024:
            large_parsed.append((rel, size))
        try:
            with open(full, encoding='utf-8-sig') as fh:
                data = json.load(fh)
        except Exception as e:
            parse_failures.append((rel, repr(e)))
            continue
        if not isinstance(data, dict):
            parse_failures.append((rel, 'top-level not object: ' + type(data).__name__))
            continue
        file_key_counts[rel] = len(data)
        for k, v in data.items():
            mod_provider.setdefault(k, []).append(rel)
            if k not in mod_meta:
                mod_meta[k] = v

MOD = set(mod_provider.keys())

def mod_name(v):
    if isinstance(v, dict):
        loc = v.get('locales')
        if isinstance(loc, dict):
            en = loc.get('en')
            if isinstance(en, dict) and en.get('name'):
                return en['name']
        if v.get('_name'):
            return v['_name']
    return ''

# ----------------------------------------------------------------------------
# 3. 我方表
# ----------------------------------------------------------------------------
def read_text(p):
    with open(p, encoding='utf-8', errors='replace') as fh:
        return fh.read()

def ours_ids_names(rel):
    raw = read_text(os.path.join(DATA_ROOT, rel))
    ids = set(HEX24.findall(raw))
    names = {}
    for m in re.finditer(r'"id"\s*:\s*"([0-9a-fA-F]{24})"\s*,\s*"name"\s*:\s*"([^"]*)"', raw):
        names[m.group(1)] = m.group(2)
    return ids, names, raw

back_ours, back_ours_names, _ = ours_ids_names(r'backpacks\backpacks.json')
arm_ours, arm_ours_names, _ = ours_ids_names(r'antigravArmbands\armbands.json')

TRUEITEMS = ['barter', 'clothing', 'keycards', 'medicals', 'partsnmods', 'provisions']
SOFTCORE = ['scavcase', 'fleamarket', 'crafting-rebalance', 'crafting-recipes']

def collect_ids(node, out):
    if isinstance(node, dict):
        for k, v in node.items():
            if k == '_id':
                out.append(v if isinstance(v, str) else json.dumps(v, ensure_ascii=False))
            collect_ids(v, out)
    elif isinstance(node, list):
        for it in node:
            collect_ids(it, out)

def classify(i):
    if i in BASE:
        return 'base'
    if i in MOD:
        return 'mod'
    return 'neither'

# ----------------------------------------------------------------------------
# 4. 域分析（背包 / 臂章）
# ----------------------------------------------------------------------------
def domain(parent_id, our_ids, our_names, base_kids, label):
    seed = set(our_ids) | set(base_kids)
    domain_mod = set()
    while True:
        added = set()
        for mid in MOD:
            if mid in domain_mod:
                continue
            mv = mod_meta.get(mid)
            if not isinstance(mv, dict):
                continue
            if mv.get('parentId') == parent_id or mv.get('itemTplToClone') in seed:
                added.add(mid)
        if not added:
            break
        domain_mod |= added
        seed |= added
    all_ids = set(base_kids) | domain_mod | set(our_ids)
    rows = []
    for i in sorted(all_ids):
        if i in BASE:
            src, nm = 'base', base_name.get(i, '')
        elif i in mod_provider:
            src, nm = mod_provider[i][0], mod_name(mod_meta.get(i, {}))
        else:
            src, nm = '(our-table-only)', ''
        if not nm and i in our_names:
            nm = our_names[i]
        in_ours = i in our_ids
        resolved = (i in BASE) or (i in MOD)
        if in_ours and resolved:
            cls = '已在我表'
        elif in_ours and not resolved:
            cls = '我表死链(B类)'
        else:
            cls = '未覆盖候选(C类)'
        rows.append((i, nm, src, cls))
    return rows, base_kids, domain_mod

bag_rows, bag_base, bag_mod = domain(BAG_PARENT, back_ours, back_ours_names, base_kids_bag, 'bagpack')
arm_rows, arm_base, arm_mod = domain(ARM_PARENT, arm_ours, arm_ours_names, base_kids_arm, 'armband')

# ----------------------------------------------------------------------------
# 5. 构建报告
# ----------------------------------------------------------------------------
w("=" * 78)
w("softcore416 数据对齐枚举报告")
w("生成脚本: D:\\Temp\\opencode\\align_enum.py")
w("=" * 78)
w("")
w("方法说明:")
w("  BASE = items.json 顶层键集（键==_id，共 %d）" % len(BASE))
w("  MOD  = 包内路径含 CustomItems 的 .json 顶层键集（去重 %d；扫描文件 %d）" % (len(MOD), mod_files_scanned))
w("  我方表: backpacks/armbands/softcore 四表用正则 [0-9a-fA-F]{24} 提取 id;")
w("          trueitems 六表 json.load 后遍历 JSON 树收集全部 `_id` 字段值。")
w("  编码: 统一用 utf-8-sig 读取（兼容 BOM）。")
w("  域匹配: 种子 = (BASE 中 _parent==父类 id 的子项) ∪ 我方表 id;")
w("          再迭代吸收 parentId==父类 id 或 itemTplToClone∈种子 的 MOD 项（克隆链传递闭包，直至不动点）。")
w("  分类: 已在我表 = id∈我方表且可解析(BASE∪MOD);")
w("        我表死链(B类) = id∈我方表但既不在 BASE 也不在 MOD;")
w("        未覆盖候选(C类) = id∈BASE∪MOD 但不在我方表。")
w("")
w("-" * 78)
w("A. 计数")
w("-" * 78)
w("BASE 总量: %d" % len(BASE))
w("MOD 总量(去重): %d" % len(MOD))
w("MOD CustomItems 文件扫描数: %d" % mod_files_scanned)
w("")
w("MOD 按文件分组计数(顶层键数):")
for rel in sorted(file_key_counts, key=lambda r: (-file_key_counts[r], r)):
    w("  %6d  %s" % (file_key_counts[rel], rel))
w("")

def dump_domain(title, rows, parent_id, base_kids, domain_mod, our_ids):
    w("-" * 78)
    w(title)
    w("-" * 78)
    w("父类: %s (%s)" % (parent_id, base_name.get(parent_id, '')))
    w("BASE 子项: %d | MOD 域项: %d | 我方表 id: %d | 并集行数: %d"
      % (len(base_kids), len(domain_mod), len(our_ids), len(rows)))
    from collections import Counter
    c = Counter(r[3] for r in rows)
    for k in ('已在我表', '我表死链(B类)', '未覆盖候选(C类)'):
        w("  %s: %d" % (k, c.get(k, 0)))
    w("")
    w("全表 (id | 名称 | 来源 | 分类):")
    order = {'已在我表': 0, '我表死链(B类)': 1, '未覆盖候选(C类)': 2}
    for i, nm, src, cls in sorted(rows, key=lambda r: (order.get(r[3], 9), r[1], r[0])):
        w("  %s | %s | %s | %s" % (i, nm, src, cls))
    w("")

dump_domain("B. 背包域", bag_rows, BAG_PARENT, bag_base, bag_mod, back_ours)
dump_domain("C. 臂章域", arm_rows, ARM_PARENT, arm_base, arm_mod, arm_ours)

# D. trueitems
w("-" * 78)
w("D. trueitems 六表（遍历 _id）")
w("-" * 78)
trueitems_stat = {}
for name in TRUEITEMS:
    rel = r'trueitems\%s.json' % name
    p = os.path.join(DATA_ROOT, rel)
    ids = []
    try:
        with open(p, encoding='utf-8-sig') as fh:
            d = json.load(fh)
        collect_ids(d, ids)
    except Exception as e:
        w("  [解析失败] %s : %r" % (rel, e))
        trueitems_stat[name] = None
        continue
    uniq = []
    seen = set()
    for i in ids:
        if i not in seen:
            seen.add(i)
            uniq.append(i)
    cnt = {'base': 0, 'mod': 0, 'neither': 0}
    neither = []
    for i in uniq:
        c = classify(i)
        cnt[c] += 1
        if c == 'neither':
            neither.append(i)
    trueitems_stat[name] = cnt
    w("  %s: _id 命中 %d(去重) -> base %d | mod %d | neither %d"
      % (name, len(uniq), cnt['base'], cnt['mod'], cnt['neither']))
    w("    neither 全列:")
    for i in neither:
        w("      %s" % i)
w("")

# E. softcore
w("-" * 78)
w("E. softcore 四表（正则 24-hex）")
w("-" * 78)
softcore_stat = {}
for name in SOFTCORE:
    rel = r'softcore\%s.json' % name
    p = os.path.join(DATA_ROOT, rel)
    try:
        raw = read_text(p)
    except Exception as e:
        w("  [读取失败] %s : %r" % (rel, e))
        softcore_stat[name] = None
        continue
    matches = HEX24.findall(raw)
    uniq = []
    seen = set()
    for i in matches:
        if i not in seen:
            seen.add(i)
            uniq.append(i)
    cnt = {'base': 0, 'mod': 0, 'neither': 0}
    neither = []
    for i in uniq:
        c = classify(i)
        cnt[c] += 1
        if c == 'neither':
            neither.append(i)
    softcore_stat[name] = cnt
    w("  %s: 命中 %d(去重，原始 %d) -> base %d | mod %d | neither %d"
      % (name, len(uniq), len(matches), cnt['base'], cnt['mod'], cnt['neither']))
    w("    neither 列表(每表上限 200):")
    for i in neither[:200]:
        w("      %s" % i)
    if len(neither) > 200:
        w("      ... 截断，剩余 %d 条" % (len(neither) - 200))
w("")

# F. 异常
w("-" * 78)
w("F. 异常")
w("-" * 78)
w("CustomItems 解析失败: %d" % len(parse_failures))
for rel, reason in parse_failures:
    w("  [失败] %s : %s" % (rel, reason))
w("")
w("跳过的大文件(>50MB, 未解析): %d" % len(skipped_large))
for rel, size in skipped_large:
    w("  [跳过] %s (%.1f MB)" % (rel, size / 1048576))
w("")
w("大文件(>10MB, 已解析): %d" % len(large_parsed))
for rel, size in large_parsed:
    w("  %s (%.1f MB)" % (rel, size / 1048576))
w("")
w("=" * 78)
w("报告结束。")
w("=" * 78)

with open(REPORT, 'w', encoding='utf-8', newline='\n') as fh:
    fh.write("\n".join(OUT) + "\n")

# ----------------------------------------------------------------------------
# 6. stdout 摘要
# ----------------------------------------------------------------------------
say("[VT-OS] 对齐枚举完成。")
say("")
say("A. 计数: BASE=%d | MOD(去重)=%d | CustomItems 文件=%d"
    % (len(BASE), len(MOD), mod_files_scanned))
say("")
from collections import Counter
def brief(title, rows):
    c = Counter(r[3] for r in rows)
    say("%s 并集 %d -> 已在我表 %d | 死链B %d | 未覆盖C %d"
        % (title, len(rows), c.get('已在我表', 0), c.get('我表死链(B类)', 0), c.get('未覆盖候选(C类)', 0)))
    for tag in ('我表死链(B类)', '未覆盖候选(C类)'):
        sel = [r for r in rows if r[3] == tag][:5]
        if sel:
            say("  %s 前5: %s" % (tag, ", ".join("%s(%s)" % (x[0], x[1] or '?') for x in sel)))
brief("B. 背包域", bag_rows)
brief("C. 臂章域", arm_rows)
say("")
say("D. trueitems:")
for name in TRUEITEMS:
    c = trueitems_stat.get(name)
    if c is None:
        say("  %s: 解析失败" % name)
    else:
        say("  %s: base %d | mod %d | neither %d" % (name, c['base'], c['mod'], c['neither']))
say("")
say("E. softcore:")
for name in SOFTCORE:
    c = softcore_stat.get(name)
    if c is None:
        say("  %s: 读取失败" % name)
    else:
        say("  %s: base %d | mod %d | neither %d" % (name, c['base'], c['mod'], c['neither']))
say("")
say("F. 解析失败 %d | 跳过大文件 %d | 大文件已解析 %d"
    % (len(parse_failures), len(skipped_large), len(large_parsed)))
say("")
say("报告已写入: %s" % REPORT)
say("报告是否存在: %s | 行数: %d" % (os.path.exists(REPORT), len(OUT)))
