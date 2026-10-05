# -*- coding: utf-8 -*-
"""dump_slices.py — 从 quests_joined.json 生成分片可读素材（work/slice-N-text.md）。

v3：无文本任务压缩为紧凑 stub；剥离 extras（主线叙事孤儿键另见 data/quests_joined.json
与二阶段 work/main-story-text.md）；去掉重复 name 行与 missingCh 噪音。

用法:
    python -X utf8 tools/dump_slices.py            # 生成全部 5 片
    python -X utf8 tools/dump_slices.py --slice 3  # 只生成第 3 片
"""
import json
import os
import sys
import textwrap

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)                      # .scratch/lore-distill
DATA = os.path.join(ROOT, 'data', 'quests_joined.json')
WORK = os.path.join(ROOT, 'work')

SLICES = {
    1: ('01-mechanic-arena', [
        ('5a7c2eca46aef81a7ca2145d', 'Mechanic'),
        ('6617beeaa9cfa777ca915b7c', '竞技场裁判'),
    ]),
    2: ('02-skier-lightkeeper-btr', [
        ('58330581ace78e27b8b10cee', 'Skier'),
        ('638f541a29ffd1183d187f57', 'Lightkeeper'),
        ('656f0f98d80a697f855d34b1', 'BTR司机'),
    ]),
    3: ('03-prapor-peacekeeper', [
        ('54cb50c76803fa8b248b4571', 'Prapor'),
        ('5935c25fb3acc3127c3d8cd9', 'Peacekeeper'),
    ]),
    4: ('04-player-ragman-misc', [
        ('67f7af56c117b6140af2a607', 'Player Trader'),
        ('5ac3b934156ae10c4430e83c', 'Ragman'),
        ('688246518448b05efd61d461', 'Kerman'),
        ('688246958448b05efd61d462', 'Voevoda'),
        ('69e0d6cc77b63940375b9173', '幸存者'),
        ('68fe15990f29ba3fdbba9d55', '无线电台'),
    ]),
    5: ('05-jaeger-therapist-fence', [
        ('5c0647fdd443bc2504c2d371', 'Jaeger'),
        ('54cb57776803fa99248b456e', 'Therapist'),
        ('579dc571d53a0658a154fbec', 'Fence'),
    ]),
}

ORDER = ['description', 'questNoteText', 'successMessageText', 'failMessageText',
         'whileAvailableMessageText', 'acceptPlayerMessage', 'declinePlayerMessage', 'completePlayerMessage']
EN_FALLBACK = ['name', 'description', 'successMessageText', 'failMessageText', 'questNoteText']
WIDTH = 500


def wrapped_lines(text):
    out = []
    for para in str(text or '').split('\n'):
        para = para.strip()
        if not para:
            continue
        if len(para) <= WIDTH:
            out.append(para)
        else:
            out.extend(textwrap.wrap(para, width=WIDTH, break_long_words=True, break_on_hyphens=False))
    return out


def emit(out, label, text, indent=0):
    lines = wrapped_lines(text)
    if not lines:
        return
    pad = '  ' * indent
    out.append(f'{pad}- {label}: {lines[0]}')
    for ln in lines[1:]:
        out.append(f'{pad}  {ln}')


def _emit_cond(out, c, nested):
    parts = []
    if c.get('text'):
        parts.append(str(c['text']))
    if c.get('hint'):
        parts.append(f"(提示: {c['hint']})")
    if c.get('note'):
        parts.append(f"(备注: {c['note']})")
    if not parts:
        return
    tag = c.get('group') or '?'
    ctype = c.get('type') or c.get('conditionType') or '?'
    sub = '（子）' if nested else ''
    emit(out, f'条件[{tag}/{ctype}]{sub}', ' '.join(parts), 1)


def _flatten_conds(conds):
    if isinstance(conds, dict):
        flat = []
        for vv in conds.values():
            if isinstance(vv, list):
                flat.extend(vv)
        conds = flat
    return conds if isinstance(conds, list) else []


def fmt_quest(q):
    """Return (rendered_text, compact_bool)."""
    t = q.get('textsCh') or {}
    te = q.get('textsEn') or {}
    kind = '主线' if q.get('isStoryQuest') else '支线'
    name = t.get('name') or te.get('name') or '(无名称)'
    meta = [
        f"id={q.get('id')}",
        f"类型={q.get('type')}",
        f"地点={q.get('location')}",
        '前置=' + (','.join(q.get('prereqQuestIds') or []) or '无'),
        '后继=' + (','.join(q.get('successorQuestIds') or []) or '无'),
        f"notDisplayed={q.get('notDisplayedQuest')}",
    ]
    cond_lines = []
    for c in _flatten_conds(q.get('conditions')):
        if not isinstance(c, dict):
            continue
        _emit_cond(cond_lines, c, nested=False)
        for cc in (c.get('nested') or []):
            if isinstance(cc, dict):
                _emit_cond(cond_lines, cc, nested=True)
    has_ch = any(str(v or '').strip() for v in t.values())
    has_en = any(str(te.get(f) or '').strip() for f in EN_FALLBACK)
    if not has_ch and not has_en and not cond_lines:
        out = [f"### [{kind}|无文本] {name} | {q.get('id', '')}",
               '- ' + ' | '.join(meta)]
        return '\n'.join(out), True
    out = [f"### [{kind}] {name} | {q.get('id', '')}",
           '- ' + ' | '.join(meta)]
    for f in ORDER:
        emit(out, f, t.get(f), 0)
    for f in EN_FALLBACK:
        if not str(t.get(f) or '').strip():
            emit(out, f'[en] {f}', te.get(f), 0)
    if cond_lines:
        out.append('- 条件:')
        out.extend(cond_lines)
    return '\n'.join(out), False


def main():
    want = None
    if '--slice' in sys.argv:
        want = int(sys.argv[sys.argv.index('--slice') + 1])
    with open(DATA, encoding='utf-8') as f:
        j = json.load(f)
    qs = j['quests']
    os.makedirs(WORK, exist_ok=True)
    for n, (slug, traders) in sorted(SLICES.items()):
        if want and n != want:
            continue
        tids = [t[0] for t in traders]
        names = {t[0]: t[1] for t in traders}
        mine = [q for q in qs.values() if q.get('traderId') in tids]
        mine.sort(key=lambda q: (tids.index(q.get('traderId')), bool(q.get('isStoryQuest')),
                                 len(q.get('prereqQuestIds') or []), q.get('id', '')))
        total_ch = sum(q.get('chCharCount') or 0 for q in mine)
        side = sum(1 for q in mine if not q.get('isStoryQuest'))
        full_n = 0
        out = []
        out.append(f'# Slice {n} 素材 — {slug}')
        out.append('> 生成: tools/dump_slices.py | 数据: data/quests_joined.json'
                   '（源自 SPT 5.x quests.json localization + locales global ch/en）')
        out.append(f'> 任务 {len(mine)}（支线 {side} / 主线 {len(mine) - side}）| ch 字符 {total_ch}')
        out.append('> 主线任务（isStoryQuest=True）在本素材中只有目标/条件文本；完整叙事见 work/main-story-text.md（第二阶段产出）。')
        out.append('> 排序: 商人 > 支线优先 > 前置数 > id；无文本任务为紧凑 stub（[主线|无文本]/[支线|无文本]）。')
        out.append('> 本素材不含 extras（主线叙事日志孤儿键）；如需核对见 data/quests_joined.json。')
        cur = None
        for q in mine:
            if q.get('traderId') != cur:
                cur = q.get('traderId')
                sub = [x for x in mine if x.get('traderId') == cur]
                out.append('')
                out.append(f'## 商人: {names.get(cur, cur)} ({cur}) — 任务 {len(sub)}')
            rendered, compact = fmt_quest(q)
            if not compact:
                full_n += 1
            out.append('')
            out.append(rendered)
        path = os.path.join(WORK, f'slice-{n}-text.md')
        with open(path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(out) + '\n')
        print(f'slice-{n}: {os.path.basename(path)} | tasks={len(mine)} | side={side} | ch={total_ch}'
              f' | full={full_n} | stub={len(mine) - full_n}')


if __name__ == '__main__':
    main()
