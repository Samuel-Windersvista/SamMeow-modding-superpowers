# -*- coding: utf-8 -*-
"""verify_report.py — 终稿引用抽样校验。

用法: python -X utf8 tools/verify_report.py [final_doc_path]
检查终稿中各类引用 id 是否真实存在于各自来源数据集
（quests_joined.json / main_story_extract.json / work/main-story-text.md），
并随机打印若干条引用上下文供人工复核。
"""
import json
import os
import random
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)


def main():
    doc = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, '塔科夫剧情统一蒸馏.md')
    text = open(doc, encoding='utf-8').read()
    qj = json.load(open(os.path.join(ROOT, 'data', 'quests_joined.json'), encoding='utf-8'))['quests']
    mst = open(os.path.join(ROOT, 'work', 'main-story-text.md'), encoding='utf-8').read()

    print('DOC', doc)
    print('chars', len(text))

    patterns = {
        'quests_joined': (r'quests_joined\.json#([0-9a-f]{24})', lambda i: i in qj),
        'quests_note': (r'(?<!_joined)quests\.json#([0-9a-f]{24})', lambda i: i in mst),
        'dialogue': (r'dialogue\.json#([0-9a-f]{24})', lambda i: i in mst),
        'tapes': (r'tapes\.json#([0-9a-f]{24})', lambda i: i in mst),
        'subtitleTracks': (r'subtitleTracks\.json#([0-9a-f]{24})', lambda i: i in mst),
        'mainQuestNotes': (r'mainQuestNotes\.json#([0-9a-f]{24})', lambda i: i in mst),
    }
    total = 0
    for name, (pat, ok) in patterns.items():
        ids = re.findall(pat, text)
        total += len(ids)
        uniq = sorted(set(ids))
        missing = [i for i in uniq if not ok(i)]
        line = f'{name}: refs={len(ids)} unique={len(uniq)} missing={len(missing)}'
        if missing:
            line += ' missing_sample=' + str(missing[:5])
        print(line)
    ends = re.findall(r'endings\.json#([\w\-]+)', text)
    miss_e = [e for e in sorted(set(ends)) if e not in mst]
    print(f'endings: refs={len(ends)} unique={len(set(ends))} missing={len(miss_e)}'
          + (' missing_sample=' + str(miss_e[:5]) if miss_e else ''))
    print('TOTAL_REFS', total)

    random.seed(2077)
    for name, pat in [('quests_joined', r'quests_joined\.json#([0-9a-f]{24})'),
                      ('dialogue', r'dialogue\.json#([0-9a-f]{24})')]:
        hits = [m.start() for m in re.finditer(pat, text)]
        if not hits:
            continue
        for pos in random.sample(hits, min(3, len(hits))):
            s = max(0, pos - 60)
            snippet = text[s:pos + 80].replace('\n', ' ')
            print(f'  ctx[{name}]', snippet)


if __name__ == '__main__':
    main()
