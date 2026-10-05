# -*- coding: utf-8 -*-
"""assemble_report.py — 将各草稿与分片汇编为最终文档《塔科夫剧情统一蒸馏》。

用法: python -X utf8 tools/assemble_report.py [--force]
输入: draft/00-intro-world.md, draft/10-mainline.md, draft/30-player-line.md,
      sections/01-mechanic-arena.md ... sections/05-jaeger-therapist-fence.md
输出: 塔科夫剧情统一蒸馏.md（本目录根）
"""
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

PARTS = [
    ('draft', '00-intro-world.md'),
    ('draft', '10-mainline.md'),
    ('draft', '30-player-line.md'),
    ('sections', '01-mechanic-arena.md'),
    ('sections', '02-skier-lightkeeper-btr.md'),
    ('sections', '03-prapor-peacekeeper.md'),
    ('sections', '04-player-ragman-misc.md'),
    ('sections', '05-jaeger-therapist-fence.md'),
]


def demote(text, levels=1):
    out = []
    for line in text.split('\n'):
        m = re.match(r'^(#{1,5})(\s)', line)
        if m:
            n = len(m.group(1))
            if n + levels <= 6:
                out.append('#' * (n + levels) + line[m.end(1):])
                continue
        out.append(line)
    return '\n'.join(out)


def main():
    missing = []
    chunks = []
    for d, f in PARTS:
        p = os.path.join(ROOT, d, f)
        if not os.path.exists(p):
            missing.append(p)
            continue
        chunks.append((f, open(p, encoding='utf-8').read().strip()))
    if missing:
        print('MISSING INPUTS:')
        for m in missing:
            print('  ', m)
        if '--force' not in sys.argv:
            return
    toc = ['## 目录', '']
    for f, c in chunks:
        for line in c.split('\n'):
            m = re.match(r'^(#{1,2})\s+(.*)$', line)
            if m:
                lvl = len(m.group(1)) + 1
                if lvl == 2:
                    toc.append(f'- {m.group(2).strip()}')
                elif lvl == 3 and not m.group(2).strip().startswith(('附录',)):
                    toc.append(f'  - {m.group(2).strip()}')
    toc.append('')
    header = [
        '# 塔科夫剧情统一蒸馏',
        '',
        '> 基于 SPT 5.x（SPT_Data/database）本地游戏数据的任务文本蒸馏：支线任务链（594 条）与主线叙事（笔记/对话/音频/结局）的统一整合，并补充世界观背景与玩家线。',
        '> 生成流程与中间产物见 `.scratch/lore-distill/`（coverage.md、data/、work/、sections/、draft/、tools/）。',
        '',
    ]
    body = []
    for f, c in chunks:
        body.append(demote(c))
        body.append('')
    appendix = [
        '## 附录 A：引用与来源说明',
        '',
        '- `quests_joined.json#<id>`：任务条目（源自 `quests.json` 的 `localization.ch`，即游戏内中文任务文本）。',
        '- `quests.json#<noteId>`：主线笔记（`<noteId> questNoteText` 内嵌于 quests.json localization）。',
        '- `dialogue.json#<subtitleId>`：主线对话字幕；`mainQuestNotes.json#<id>`：笔记结构；`tapes.json#<id>`、`subtitleTracks.json#<id>`、`endings.json#<name>`：音频日志 / 字幕 / 结局。',
        '- `knowledge/spt-kb/curated/lore/*`：现有 lore 分区文档（社区整理）；`research/external-lore-notes.md`：官方来源简报（含可信度标注）。',
        '',
        '## 附录 B：覆盖统计',
        '',
        '- 任务总数 786（主线叙事 192 / 支线 594）；中文任务文本总量 232,525 字符；注意 `ch.json` 仅含任务短名，正文实为 quests.json 内嵌 `localization.ch`。',
        '- 主线叙事覆盖 138/192（notes 83、dialogue 86、tapes 10、endings 2；并集 138；54 条无直接文本，清单见 `data/main_story_index.md`）。',
        '- 支线分片：片1 148（支线116/主线32）、片2 133（113/20）、片3 140（117/23）、片4 224（118/106）、片5 141（130/11）。',
        '- 隐藏/未展示任务 87（notDisplayedQuest=True）；en 兜底与异常明细见各分片「缺口与标注」与 `coverage.md`。',
        '',
    ]
    text = '\n'.join(header + toc + body + appendix)
    out_path = os.path.join(ROOT, '塔科夫剧情统一蒸馏.md')
    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(text)
    print('WROTE', out_path)
    print('chars', len(text), '| parts', len(chunks), '| missing', len(missing))


if __name__ == '__main__':
    main()
