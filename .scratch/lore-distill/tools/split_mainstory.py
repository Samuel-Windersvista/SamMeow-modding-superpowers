# -*- coding: utf-8 -*-
"""split_mainstory.py — 将 work/main-story-text.md 的 §2 对话节切分为 N 个平衡分块。

用法: python -X utf8 tools/split_mainstory.py [--parts 4]
输出: work/mainstory-dialogue-part-{i}.md（按元素边界连续切分，保持顺序）
"""
import re
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
SRC = os.path.join(ROOT, 'work', 'main-story-text.md')
WORK = os.path.join(ROOT, 'work')


def main():
    parts_n = 4
    if '--parts' in sys.argv:
        parts_n = int(sys.argv[sys.argv.index('--parts') + 1])
    t = open(SRC, encoding='utf-8').read()
    m = re.search(r'\n## 2\.', t)
    if not m:
        print('SECTION2 NOT FOUND')
        return
    d = t[m.start():].split('\n## 3.', 1)[0]
    subs = re.split(r'\n(?=### )', d)
    elems = subs[1:]
    if len(elems) < parts_n:
        print('NOT ENOUGH ELEMENTS', len(elems))
        return
    total = sum(len(e) for e in elems)
    cumsums = []
    acc = 0
    for e in elems:
        acc += len(e)
        cumsums.append(acc)
    bounds = [0]
    last = 0
    for k in range(1, parts_n):
        tc = total * k / parts_n
        idx = last
        while idx < len(elems) - (parts_n - k) and cumsums[idx] < tc:
            idx += 1
        bounds.append(idx + 1)
        last = idx + 1
    bounds.append(len(elems))
    for i in range(parts_n):
        chunk = elems[bounds[i]:bounds[i + 1]]
        n_chars = sum(len(x) for x in chunk)
        head = [
            f'# 主线对话素材 — 第 {i + 1}/{parts_n} 部分',
            f'> 切分自 work/main-story-text.md §2（dialogue.json，ch 文本优先）；元素 {len(chunk)} 个；约 {n_chars} 字符。',
            '',
        ]
        path = os.path.join(WORK, f'mainstory-dialogue-part-{i + 1}.md')
        with open(path, 'w', encoding='utf-8') as f:
            f.write('\n'.join(head) + '\n'.join(chunk) + '\n')
        first = chunk[0].splitlines()[0][:64]
        last_h = chunk[-1].splitlines()[0][:64]
        print(f'part-{i + 1}: elems={len(chunk)} chars={n_chars}')
        print(f'   first: {first}')
        print(f'   last : {last_h}')


if __name__ == '__main__':
    main()
