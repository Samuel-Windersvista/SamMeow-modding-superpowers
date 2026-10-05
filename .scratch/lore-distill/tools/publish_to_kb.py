# -*- coding: utf-8 -*-
"""publish_to_kb.py — 将终稿复制为 spt-kb curated/lore 文档（附 frontmatter）。

用法: python -X utf8 tools/publish_to_kb.py
输入: <lore-distill>/塔科夫剧情统一蒸馏.md
输出: <repo>/knowledge/spt-kb/curated/lore/13-quest-text-distillation.md
"""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)                      # .scratch/lore-distill
REPO = os.path.dirname(os.path.dirname(ROOT))     # repo root
SRC = os.path.join(ROOT, '塔科夫剧情统一蒸馏.md')
DST = os.path.join(REPO, 'knowledge', 'spt-kb', 'curated', 'lore', '13-quest-text-distillation.md')

FRONT = """---
version: [通用]
domain: both
topic: lore
source: curated
keywords: [任务, 剧情, 主线, 支线, 蒸馏, 世界观, 玩家线, 引用]
summary: SPT 5.x 本地数据任务文本全量蒸馏：786 任务（主线 192 / 支线 594）逐条目引用溯源、主线十章统一叙事（笔记/对话/音频/结局）、世界观背景与玩家线补充；另附引用体系与覆盖统计
---
"""


def main():
    text = open(SRC, encoding='utf-8').read()
    if text.startswith('---'):
        print('SRC already has frontmatter; abort')
        return
    os.makedirs(os.path.dirname(DST), exist_ok=True)
    with open(DST, 'w', encoding='utf-8') as f:
        f.write(FRONT + '\n' + text)
    print('WROTE', DST)
    print('chars', len(FRONT) + 1 + len(text))


if __name__ == '__main__':
    main()
