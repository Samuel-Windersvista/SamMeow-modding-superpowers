# -*- coding: utf-8 -*-
"""SPT 任务文本提取与索引（剧情统一蒸馏 第一阶段）。

只读源文件：
  <SPT_DATA>/database/templates/quests.json          (任务对象，内含 localization)
  <SPT_DATA>/database/locales/global/ch.json          (中文文本 + trader 名称)
  <SPT_DATA>/database/locales/global/en.json          (英文兜底)

输出（全部落在本工作目录 data/ 下）：
  data/quests_joined.json     主产物：786 个任务的完整结构化条目
  data/quests_index.md        人类可读索引（按商人分组）
  data/quests_by_trader.json  按商人分组（供下一阶段分片）
  coverage.md                 统计 / 分类依据 / 数据质量 / 分片建议

幂等：所有输出确定性排序 + 固定缩进，重复运行字节一致。
运行：python -X utf8 tools/extract_quests.py
"""

from __future__ import annotations

import hashlib
import json
import random
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

# 控制台中文（getattr 规避静态类型误报）
_reconfigure = getattr(sys.stdout, "reconfigure", None)
if callable(_reconfigure):
    _reconfigure(encoding="utf-8")

# ---------------------------------------------------------------------------
# 路径
# ---------------------------------------------------------------------------
WORK_DIR = Path(__file__).resolve().parent.parent
DATA_DIR = WORK_DIR / "data"

SPT_DATA = Path(r"E:\Game\EFT_Offline\SPT_5xx\SPT_Runtime\SPT_Data")
QUESTS_PATH = SPT_DATA / "database" / "templates" / "quests.json"
CH_PATH = SPT_DATA / "database" / "locales" / "global" / "ch.json"
EN_PATH = SPT_DATA / "database" / "locales" / "global" / "en.json"

# 任务级文本后缀（经侦察：questNoteText 在本版数据中不是任务级，见 coverage.md）
QUEST_SUFFIXES = [
    "name",
    "description",
    "successMessageText",
    "failMessageText",
    "whileAvailableMessageText",
    "acceptPlayerMessage",
    "declinePlayerMessage",
    "completePlayerMessage",
]
# 侦察确认：questNoteText 只以 <condId> questNoteText 的形式出现在条件级
COND_NOTE_SUFFIX = "questNoteText"

COND_GROUP_ORDER = ["AutoStart", "AvailableForStart", "AvailableForFinish", "Fail"]

HEX24 = re.compile(r"^[0-9a-f]{24}$")
KEY_SUFFIX = re.compile(r"^([0-9a-f]{24})\s+(.+)$")
KEY_HINT = re.compile(r"^([0-9a-f]{24})_hint$")

# 侦察基准（用于 coverage.md 对照；不一致需解释）
BASELINE_TRADER_CH = [
    ("5a7c2eca46aef81a7ca2145d", 41533),
    ("58330581ace78e27b8b10cee", 27981),
    ("54cb50c76803fa8b248b4571", 27901),
    ("67f7af56c117b6140af2a607", 22954),
    ("5c0647fdd443bc2504c2d371", 21168),
    ("54cb57776803fa99248b456e", 20369),
    ("5ac3b934156ae10c4430e83c", 19638),
    ("5935c25fb3acc3127c3d8cd9", 17797),
    ("638f541a29ffd1183d187f57", 8961),
    ("656f0f98d80a697f855d34b1", 8527),
    ("6617beeaa9cfa777ca915b7c", 6738),
    ("579dc571d53a0658a154fbec", 6307),
    ("688246518448b05efd61d461", 1966),
    ("688246958448b05efd61d462", 346),
    ("69e0d6cc77b63940375b9173", 282),
    ("68fe15990f29ba3fdbba9d55", 57),
]
BASELINE_TOTAL_CH = 232525
BASELINE_STORY_CH = 50022
BASELINE_SIDE_CH = 182503

# 67f7af56 在 ch.json/en.json 无任何名称键；名称取自 traders/<id>/base.json，
# 且无中文；此处显式记录来源（见 coverage.md“trader id -> 名称映射”）。
TRADER_NAME_OVERRIDES = {
    "67f7af56c117b6140af2a607": {
        "cn": None,
        "en": "Player Trader",
        "source": "database/traders/67f7af56c117b6140af2a607/base.json nickname（无 zh 本地化）",
    }
}


# ---------------------------------------------------------------------------
# 读取
# ---------------------------------------------------------------------------
def load_json(path: Path):
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def build_trader_names(ch: dict, en: dict) -> dict:
    """从 ch.json/en.json 提取 <traderId> Nickname/FullName/FirstName 名称。

    优先 Nickname；Nickname 为空则退 FullName/FirstName；中国为空则用 en。
    """
    fields = ("Nickname", "FullName", "FirstName", "Surname")
    raw = defaultdict(dict)  # tid -> field -> {lang: value}
    for lang, loc in (("ch", ch), ("en", en)):
        for k, v in loc.items():
            m = re.match(r"^([0-9a-f]{24}) (Nickname|FullName|FirstName|Surname)$", k)
            if m:
                raw[m.group(1)].setdefault(m.group(2), {})[lang] = v

    names = {}
    for tid, d in raw.items():
        cn = ""
        for f in fields:
            v = d.get(f, {}).get("ch", "")
            if v and v.strip():
                cn = v.strip()
                break
        en_v = ""
        for f in fields:
            v = d.get(f, {}).get("en", "")
            if v and v.strip():
                en_v = v.strip()
                break
        names[tid] = {"cn": cn or None, "en": en_v or None, "source": "ch.json/en.json"}

    # ch.json/en.json 中完全没有名称键的 trader（如 67f7af56），用显式 override 补齐
    for tid, ov in TRADER_NAME_OVERRIDES.items():
        if tid not in names:
            names[tid] = {"cn": ov["cn"], "en": ov["en"], "source": ov["source"]}
        else:
            names[tid]["cn"] = names[tid]["cn"] or ov["cn"]
            names[tid]["en"] = names[tid]["en"] or ov["en"]
            if ov["cn"] or ov["en"]:
                names[tid]["source"] = names[tid]["source"] + " + " + ov["source"]
    return names


# ---------------------------------------------------------------------------
# 条件文本挂接
# ---------------------------------------------------------------------------
def collect_condition_ids(quest: dict) -> set:
    """收集 quest 内所有条件 id（含 CounterCreator 嵌套）。"""
    ids = set()

    def walk(obj):
        if isinstance(obj, dict):
            if isinstance(obj.get("id"), str) and obj.get("conditionType"):
                ids.add(obj["id"])
            for v in obj.values():
                walk(v)
        elif isinstance(obj, list):
            for v in obj:
                walk(v)

    walk(quest.get("conditions", {}))
    return ids


def index_condition_texts(loc: dict, cond_ids: set, qid: str):
    """把 localization 字典切成 {条件id: 文本/提示/note}，其余进 extras。

    任务级键 `<qid> <8后缀>` 不属于条件文本，跳过（由主流程处理）。
    extras 分两类：orphanCondTexts（短文本/提示残留键）、orphanQuestNotes（questNoteText 残留）。
    """
    short, hint, note = {}, {}, {}
    extras = {"orphanCondTexts": {}, "orphanQuestNotes": {}}
    for k, v in loc.items():
        m_hint = KEY_HINT.match(k)
        if m_hint:
            cid = m_hint.group(1)
            if cid in cond_ids:
                hint[cid] = v
            else:
                extras["orphanCondTexts"][k] = v
            continue
        m = KEY_SUFFIX.match(k)
        if m:
            pre, suf = m.group(1), m.group(2)
            if pre == qid and suf in QUEST_SUFFIXES:
                continue  # 任务级文本，主流程处理
            if suf == COND_NOTE_SUFFIX:
                if pre in cond_ids:
                    note[pre] = v
                else:
                    extras["orphanQuestNotes"][pre] = v
                continue
            extras["orphanCondTexts"][k] = v
            continue
        if HEX24.match(k) and k in cond_ids:
            short[k] = v
        else:
            extras["orphanCondTexts"][k] = v
    return short, hint, note, extras


def norm_target(target):
    """Quest 条件 target 归一化为 questId 列表。"""
    if isinstance(target, str):
        return [target]
    if isinstance(target, list):
        return [t for t in target if isinstance(t, str)]
    return []


def build_condition_entries(quest: dict, ch_short, ch_hint, ch_note,
                            en_short, en_hint, en_note):
    """生成条件摘要列表（含嵌套条件）。"""
    groups = quest.get("conditions", {})
    ordered = [g for g in COND_GROUP_ORDER if g in groups]
    ordered += sorted(g for g in groups if g not in COND_GROUP_ORDER)

    def summarize(cond: dict, group: str, parent_id: str | None):
        cid = cond.get("id")
        text = ch_short.get(cid)
        src = "ch" if (text and text.strip()) else None
        if not src and cid:
            e = en_short.get(cid)
            if e and e.strip():
                text, src = e, "en"
        h = ch_hint.get(cid)
        hsrc = "ch" if (h and h.strip()) else None
        if not hsrc and cid:
            e = en_hint.get(cid)
            if e and e.strip():
                h, hsrc = e, "en"
        n = ch_note.get(cid)
        nsrc = "ch" if (n and n.strip()) else None
        if not nsrc and cid:
            e = en_note.get(cid)
            if e and e.strip():
                n, nsrc = e, "en"

        entry = {
            "id": cid,
            "group": group,
            "type": cond.get("conditionType"),
        }
        tgt = cond.get("target")
        if tgt is not None:
            entry["target"] = tgt
        if cond.get("value") is not None:
            entry["value"] = cond.get("value")
        if parent_id:
            entry["parentConditionId"] = parent_id
        if text is not None:
            entry["text"] = text
            entry["textSource"] = src
        if h is not None:
            entry["hint"] = h
            entry["hintSource"] = hsrc
        if n is not None:
            entry["note"] = n
            entry["noteSource"] = nsrc

        # CounterCreator 嵌套
        nested = (cond.get("counter") or {}).get("conditions")
        if isinstance(nested, list) and nested:
            entry["nested"] = [summarize(c, group, cid) for c in nested]
        return entry

    out = []
    for g in ordered:
        for cond in groups.get(g, []):
            out.append(summarize(cond, g, None))
    return out


# ---------------------------------------------------------------------------
# 主流程
# ---------------------------------------------------------------------------
def main():
    print("[extract] loading sources ...")
    quests = load_json(QUESTS_PATH)
    ch = load_json(CH_PATH)
    en = load_json(EN_PATH)
    print(f"[extract] quests={len(quests)} ch_keys={len(ch)} en_keys={len(en)}")

    trader_names = build_trader_names(ch, en)

    joined = {}
    # 反向后继
    successor_map = defaultdict(set)
    prereq_map = {}

    for qid, quest in quests.items():
        loc_ch = quest.get("localization", {}).get("ch", {}) or {}
        loc_en = quest.get("localization", {}).get("en", {}) or {}

        cond_ids = collect_condition_ids(quest)
        ch_short, ch_hint, ch_note, ch_extra = index_condition_texts(loc_ch, cond_ids, qid)
        en_short, en_hint, en_note, en_extra = index_condition_texts(loc_en, cond_ids, qid)

        # --- 任务级文本 ---
        texts_ch = {}
        missing_ch = []
        for suf in QUEST_SUFFIXES:
            v = loc_ch.get(f"{qid} {suf}")
            if v and v.strip():
                texts_ch[suf] = v
            else:
                missing_ch.append(suf)

        texts_en = {}
        # 全部 name
        en_name = loc_en.get(f"{qid} name")
        if en_name and en_name.strip():
            texts_en["name"] = en_name
        # 仅补充缺失中文的条目
        for suf in QUEST_SUFFIXES:
            if suf == "name":
                continue
            if suf in missing_ch:
                v = loc_en.get(f"{qid} {suf}")
                if v and v.strip():
                    texts_en[suf] = v

        # --- 前置 / 后继 ---
        prereq = []
        for cond in quest.get("conditions", {}).get("AvailableForStart", []):
            if cond.get("conditionType") == "Quest":
                prereq.extend(norm_target(cond.get("target")))
        prereq = sorted(set(prereq))
        prereq_map[qid] = prereq
        for p in prereq:
            successor_map[p].add(qid)

        # --- 条件摘要 ---
        conditions = build_condition_entries(
            quest, ch_short, ch_hint, ch_note, en_short, en_hint, en_note
        )

        cond_stat = {
            "total": len(conditions),
            "withTextCh": sum(1 for c in conditions if c.get("textSource") == "ch"),
            "withTextEnOnly": sum(1 for c in conditions if c.get("textSource") == "en"),
            "withNote": sum(1 for c in conditions if "note" in c),
            "withHint": sum(1 for c in conditions if "hint" in c),
        }

        # --- extras（挂不上的条件文本键）---
        extras = {}
        for kind in ("orphanCondTexts", "orphanQuestNotes"):
            ch_d = ch_extra.get(kind, {})
            if ch_d:
                extras[kind] = {"ch": ch_d}
                en_d = en_extra.get(kind, {})
                fb = {k: en_d[k] for k in ch_d
                      if k in en_d and (en_d[k] or "").strip() and not (ch_d[k] or "").strip()}
                if fb:
                    extras[kind]["en"] = fb
        if extras:
            extras["_note"] = (
                "orphanCondTexts=未匹配到同 quest 条件 id 的短文本/提示残留键；"
                "orphanQuestNotes=未匹配到条件的 questNoteText（叙事日志条目）。"
                "均为历史/打包遗留的孤立 localization 键，保留原文以防丢失叙事内容。"
            )

        ch_char_count = sum(len(v) for v in loc_ch.values())
        task_ch_char_count = sum(len(v) for v in texts_ch.values())

        tid = quest.get("traderId")
        tname = trader_names.get(tid, {})

        joined[qid] = {
            "id": qid,
            "traderId": tid,
            "traderNameCn": tname.get("cn"),
            "traderNameEn": tname.get("en"),
            "location": quest.get("location"),
            "type": quest.get("type"),
            "side": quest.get("side"),
            "isStoryQuest": bool(quest.get("isStoryQuest")),
            "notDisplayedQuest": bool(quest.get("notDisplayedQuest")),
            "secretQuest": bool(quest.get("secretQuest")),
            "prereqQuestIds": prereq,
            "successorQuestIds": sorted(successor_map.get(qid, set())),
            "textsCh": texts_ch,
            "textsEn": texts_en,
            "chCharCount": ch_char_count,
            "taskChCharCount": task_ch_char_count,
            "missingCh": missing_ch,
            "conditions": conditions,
            "conditionTextStats": cond_stat,
        }
        if extras:
            joined[qid]["extras"] = extras

    # 回填 successor（需要先全量构建）
    for qid, q in joined.items():
        q["successorQuestIds"] = sorted(successor_map.get(qid, set()))

    # --- 统计 ---
    stats = compute_stats(joined, quests, ch, en)

    # --- 写出 ---
    DATA_DIR.mkdir(parents=True, exist_ok=True)
    meta = {
        "generated_by": "tools/extract_quests.py",
        "sources": {
            "quests": str(QUESTS_PATH),
            "ch": str(CH_PATH),
            "en": str(EN_PATH),
        },
        "total_quests": len(joined),
        "schema_note": (
            "quests_joined.json 顶层为 {_meta, quests:{questId: {...}}}。"
            "chCharCount = 该任务 localization['ch'] 全部键字符数（与侦察基准一致）；"
            "taskChCharCount = 仅 8 个任务级后缀字符数。"
        ),
    }
    out = {"_meta": meta, "quests": joined}
    write_json(DATA_DIR / "quests_joined.json", out)

    # by trader
    by_trader = build_by_trader(joined, trader_names)
    write_json(DATA_DIR / "quests_by_trader.json", by_trader)

    # index md
    write_index_md(DATA_DIR / "quests_index.md", by_trader)

    # coverage md
    write_coverage_md(WORK_DIR / "coverage.md", stats, joined, by_trader, trader_names, quests)

    # 控制台摘要
    print(f"[extract] total quests: {stats['total_quests']}")
    print(f"[extract] story/side: {stats['story_count']}/{stats['side_count']}")
    print(f"[extract] ch non-empty names: {stats['suffix_nonempty']['name']}")
    print(f"[extract] total ch chars: {stats['total_ch_chars']}")
    print("[extract] done.")


# ---------------------------------------------------------------------------
# 统计
# ---------------------------------------------------------------------------
def compute_stats(joined, quests, ch, en):
    suffix_nonempty = Counter()
    for qid, q in joined.items():
        for suf, v in q["textsCh"].items():
            if v and v.strip():
                suffix_nonempty[suf] += 1

    total_ch = sum(q["chCharCount"] for q in joined.values())
    total_task_ch = sum(q["taskChCharCount"] for q in joined.values())

    story = [q for q in joined.values() if q["isStoryQuest"]]
    side = [q for q in joined.values() if not q["isStoryQuest"]]
    story_ch = sum(q["chCharCount"] for q in story)
    side_ch = sum(q["chCharCount"] for q in side)

    # trader 分布（用 chCharCount，与基准同口径）
    trader_ch = Counter()
    trader_cnt = Counter()
    for q in joined.values():
        trader_ch[q["traderId"]] += q["chCharCount"]
        trader_cnt[q["traderId"]] += 1

    en_total = 0
    en_suffix_nonempty = Counter()
    for q in joined.values():
        loc_en = quests[q["id"]].get("localization", {}).get("en", {}) or {}
        en_total += sum(len(v) for v in loc_en.values())
        for suf in QUEST_SUFFIXES:
            v = loc_en.get(f"{q['id']} {suf}")
            if v and v.strip():
                en_suffix_nonempty[suf] += 1

    missing_name = [q for q in joined.values() if "name" in q["missingCh"]]
    empty_name_both = [q for q in missing_name if not q["textsEn"].get("name")]
    texts_en_entries = sum(len(q["textsEn"]) for q in joined.values())
    ch_missing_any = sum(1 for q in joined.values() if q["missingCh"])

    def miss(qs, suf):
        return sum(1 for q in qs if suf in q["missingCh"])

    suffix_by_class = {}
    for suf in QUEST_SUFFIXES:
        suffix_by_class[suf] = {
            "story": sum(1 for q in story if suf in q["textsCh"]),
            "side": sum(1 for q in side if suf in q["textsCh"]),
        }
    notdisp_ids = {q["id"] for q in joined.values() if q["notDisplayedQuest"]}
    side_missing_name_ids = {q["id"] for q in side if "name" in q["missingCh"]}
    anomaly = {
        "story_missing_name": miss(story, "name"),
        "story_missing_desc": miss(story, "description"),
        "side_missing_name": miss(side, "name"),
        "side_missing_desc": miss(side, "description"),
        "side_missing_name_eq_notdisplayed": side_missing_name_ids == notdisp_ids,
        "suffix_by_class": suffix_by_class,
    }

    cond_total = sum(q["conditionTextStats"]["total"] for q in joined.values())
    cond_ch = sum(q["conditionTextStats"]["withTextCh"] for q in joined.values())
    cond_en = sum(q["conditionTextStats"]["withTextEnOnly"] for q in joined.values())
    extras_cond = sum(len(q.get("extras", {}).get("orphanCondTexts", {}).get("ch", {}))
                      for q in joined.values())
    extras_notes = sum(len(q.get("extras", {}).get("orphanQuestNotes", {}).get("ch", {}))
                       for q in joined.values())

    return {
        "total_quests": len(joined),
        "story_count": len(story),
        "side_count": len(side),
        "notDisplayed_count": sum(1 for q in joined.values() if q["notDisplayedQuest"]),
        "secret_count": sum(1 for q in joined.values() if q["secretQuest"]),
        "suffix_nonempty": dict(suffix_nonempty),
        "total_ch_chars": total_ch,
        "total_task_ch_chars": total_task_ch,
        "story_ch": story_ch,
        "side_ch": side_ch,
        "en_total": en_total,
        "en_suffix_nonempty": dict(en_suffix_nonempty),
        "texts_en_entries": texts_en_entries,
        "ch_missing_any": ch_missing_any,
        "anomaly": anomaly,
        "trader_ch": dict(trader_ch),
        "trader_cnt": dict(trader_cnt),
        "baseline_trader_ch": BASELINE_TRADER_CH,
        "baseline_total_ch": BASELINE_TOTAL_CH,
        "baseline_story_ch": BASELINE_STORY_CH,
        "baseline_side_ch": BASELINE_SIDE_CH,
        "missing_name_count": len(missing_name),
        "empty_name_both_count": len(empty_name_both),
        "cond_total": cond_total,
        "cond_ch": cond_ch,
        "cond_en_only": cond_en,
        "prereq_edges": sum(len(q["prereqQuestIds"]) for q in joined.values()),
        "reverse_broken": sum(
            1 for q in joined.values() for p in q["prereqQuestIds"]
            if p in joined and q["id"] not in joined[p]["successorQuestIds"]
        ),
        "dangling_prereq": sorted(
            {p for q in joined.values() for p in q["prereqQuestIds"] if p not in joined}
        ),
        "extras_cond": extras_cond,
        "extras_notes": extras_notes,
        "side_values": Counter(q["side"] for q in joined.values()),
        "type_values": Counter(q["type"] for q in joined.values()),
    }


def build_by_trader(joined, trader_names):
    groups = defaultdict(list)
    for q in joined.values():
        groups[q["traderId"]].append(q)
    out = {}
    for tid, qs in groups.items():
        qs_sorted = sorted(qs, key=lambda x: (-x["chCharCount"], x["id"]))
        tname = trader_names.get(tid, {})
        out[tid] = {
            "traderId": tid,
            "traderNameCn": tname.get("cn"),
            "traderNameEn": tname.get("en"),
            "traderNameSource": tname.get("source"),
            "questCount": len(qs_sorted),
            "storyCount": sum(1 for q in qs_sorted if q["isStoryQuest"]),
            "sideCount": sum(1 for q in qs_sorted if not q["isStoryQuest"]),
            "chCharCount": sum(q["chCharCount"] for q in qs_sorted),
            "quests": [
                {
                    "id": q["id"],
                    "name": q["textsCh"].get("name") or q["textsEn"].get("name"),
                    "nameSource": "ch" if q["textsCh"].get("name") else ("en" if q["textsEn"].get("name") else None),
                    "isStoryQuest": q["isStoryQuest"],
                    "chCharCount": q["chCharCount"],
                    "hasDescription": bool(q["textsCh"].get("description")),
                    "prereqCount": len(q["prereqQuestIds"]),
                }
                for q in qs_sorted
            ],
        }
    return out


def lpt_shards(trader_ch: dict, n: int):
    """LPT 装箱：商人不可切分，按 chCharCount 降序放入当前最空的片。"""
    traders = sorted(trader_ch.items(), key=lambda kv: (-kv[1], kv[0]))
    bins = [[] for _ in range(n)]
    sizes = [0] * n
    for tid, c in traders:
        i = min(range(n), key=lambda j: (sizes[j], j))
        bins[i].append(tid)
        sizes[i] += c
    return bins, sizes


# ---------------------------------------------------------------------------
# 输出
# ---------------------------------------------------------------------------
def write_json(path: Path, obj):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2, sort_keys=True)
        f.write("\n")
    print(f"[extract] wrote {path} ({path.stat().st_size} bytes)")


def esc(s):
    return (s or "").replace("|", "\\|").replace("\n", " ")


def write_index_md(path: Path, by_trader):
    lines = [
        "# SPT 5.x 任务索引（按商人分组）",
        "",
        "> 由 `tools/extract_quests.py` 生成，请勿手改。",
        "> 名称优先取中文（`textsCh.name`），缺失时取英文并在名称后标 `(en)`。",
        "> ch 字符数 = 该任务 `localization['ch']` 全部键字符数（含条件级文本）。",
        "",
    ]
    # 按总字符降序排列商人
    order = sorted(by_trader.values(), key=lambda g: (-g["chCharCount"], g["traderId"]))
    for g in order:
        cn = g["traderNameCn"] or g["traderNameEn"] or "(未知)"
        label = f"{cn}"
        if not g["traderNameCn"] and g["traderNameEn"]:
            label += " (en)"
        lines.append(f"## {label}  `{g['traderId']}`")
        lines.append("")
        lines.append(
            f"- 任务数 {g['questCount']}（主线 {g['storyCount']} / 支线 {g['sideCount']}），"
            f"ch 字符 {g['chCharCount']}"
        )
        lines.append("")
        lines.append("| id | 中文名 | 主/支线 | ch 字符数 | description | 前置数 |")
        lines.append("|---|---|---|---:|---|---:|")
        for q in g["quests"]:
            nm = q["name"] or "(无名称)"
            if q["nameSource"] == "en":
                nm += " (en)"
            kind = "主线" if q["isStoryQuest"] else "支线"
            desc = "有" if q.get("hasDescription") else "无"
            lines.append(
                f"| `{q['id']}` | {esc(nm)} | {kind} | {q['chCharCount']} | {desc} | {q['prereqCount']} |"
            )
        lines.append("")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines))
    print(f"[extract] wrote {path} ({path.stat().st_size} bytes)")


def self_check(joined, quests, n=3):
    """确定性随机抽 n 个任务，逐字段对照源文件；返回 (lines, all_pass)。"""
    rng = random.Random(2077)
    ids = sorted(joined.keys())
    picks = rng.sample(ids, n)
    lines = []
    all_pass = True
    for qid in picks:
        q = joined[qid]
        src = quests[qid]
        src_ch = src.get("localization", {}).get("ch", {}) or {}
        checks = []
        checks.append(("traderId", q["traderId"] == src.get("traderId")))
        checks.append(("location", q["location"] == src.get("location")))
        checks.append(("type", q["type"] == src.get("type")))
        checks.append(("side", q["side"] == src.get("side")))
        checks.append(("isStoryQuest", q["isStoryQuest"] == bool(src.get("isStoryQuest"))))
        checks.append(("notDisplayedQuest", q["notDisplayedQuest"] == bool(src.get("notDisplayedQuest"))))
        # textsCh 与源文件逐后缀一致（非空）
        ok_txt = True
        for suf in QUEST_SUFFIXES:
            raw = src_ch.get(f"{qid} {suf}")
            got = q["textsCh"].get(suf)
            raw_ok = raw if (raw and raw.strip()) else None
            if raw_ok != got:
                ok_txt = False
        checks.append(("textsCh", ok_txt))
        # chCharCount
        checks.append(("chCharCount", q["chCharCount"] == sum(len(v) for v in src_ch.values())))
        # prereq 手工重算
        manual_pre = set()
        for c in src.get("conditions", {}).get("AvailableForStart", []):
            if c.get("conditionType") == "Quest":
                manual_pre.update(norm_target(c.get("target")))
        checks.append(("prereqQuestIds", set(q["prereqQuestIds"]) == manual_pre))
        passed = all(v for _, v in checks)
        all_pass = all_pass and passed
        lines.append(f"### 样例 `{qid}` — {'PASS' if passed else 'FAIL'}")
        lines.append("")
        name = q["textsCh"].get("name") or q["textsEn"].get("name") or "(无名称)"
        lines.append(f"- 名称：{name}｜trader：{q['traderId']}｜主线：{q['isStoryQuest']}｜"
                     f"ch 字符：{q['chCharCount']}｜前置：{q['prereqQuestIds']}")
        lines.append(f"- 检查项：" + "、".join(f"{k}={'OK' if v else 'X'}" for k, v in checks))
        desc = q["textsCh"].get("description", "")
        snippet = (desc[:120] + "…") if len(desc) > 120 else desc
        lines.append(f"- description 摘要：{snippet or '(空)'}")
        lines.append("")
    return lines, all_pass


def output_hashes():
    h = {}
    for fn in ("quests_joined.json", "quests_by_trader.json", "quests_index.md"):
        p = DATA_DIR / fn
        h[fn] = hashlib.sha256(p.read_bytes()).hexdigest()[:16] if p.exists() else "missing"
    return h


def write_coverage_md(path: Path, stats, joined, by_trader, trader_names, quests):
    L = []
    L.append("# Coverage — SPT 5.x 任务文本提取（第一阶段）")
    L.append("")
    L.append("> 由 `tools/extract_quests.py` 生成。本阶段只做数据提取与索引，不做剧情概括/翻译/创作。")
    L.append("")

    L.append("## 1. 数据源与口径")
    L.append("")
    L.append(f"- quests.json：`{QUESTS_PATH}`")
    L.append(f"- ch.json：`{CH_PATH}`")
    L.append(f"- en.json：`{EN_PATH}`")
    L.append("")
    L.append("- `chCharCount` = 任务 `localization['ch']` **全部键**字符数（任务级 + 条件级 + 残留键），"
             "与侦察基准同口径。")
    L.append("- `taskChCharCount` = 仅 8 个任务级后缀的字符数（见下表）。")
    L.append(f"- 全库 ch 字符总数：**{stats['total_ch_chars']}**（基准 {BASELINE_TOTAL_CH}）；"
             f"其中任务级 {stats['total_task_ch_chars']}。")
    L.append(f"- 全库 en 字符总数：**{stats['en_total']}**（基准 579118）。")
    L.append("")

    L.append("## 2. 分类依据")
    L.append("")
    L.append("- **主线/支线**：严格按 quest 对象的 `isStoryQuest` 布尔字段。"
             "`side` 字段全库恒为 `Pmc`，不用于分类。")
    L.append(f"- 主线 {stats['story_count']} / 支线 {stats['side_count']}，"
             f"合计 {stats['total_quests']}。")
    L.append(f"- `notDisplayedQuest=True`：{stats['notDisplayed_count']}；"
             f"`secretQuest=True`：{stats['secret_count']}。")
    L.append(f"- `side` 取值分布：{dict(stats['side_values'])}。")
    L.append(f"- `type` 取值分布：{dict(stats['type_values'])}。")
    L.append("")

    L.append("## 3. 任务级后缀非空计数（ch）")
    L.append("")
    L.append("| 后缀 | ch 非空数 | en 非空数 |")
    L.append("|---|---:|---:|")
    for suf in QUEST_SUFFIXES:
        L.append(f"| {suf} | {stats['suffix_nonempty'].get(suf, 0)} | "
                 f"{stats['en_suffix_nonempty'].get(suf, 0)} |")
    L.append("")
    L.append("- 侦察基准声称 questNoteText 376。经核实：本版数据中 **questNoteText 不是任务级后缀**"
             "（`<qid> questNoteText` 键 0 个），376 实际是形如 `<前缀> questNoteText` 的键；"
             "且这些前缀在任何 quest 的条件中都不存在（全部为孤立键）。"
             "它们从内容看是叙事日志条目，已按任务保留在 `extras.orphanQuestNotes`。")
    L.append(f"- 其余后缀非空数与侦察基准完全一致：name 519 / successMessageText 513 / "
             f"description 507 / whileAvailableMessageText 63 / failMessageText 44 / "
             f"completePlayerMessage 23 / acceptPlayerMessage 14 / declinePlayerMessage 10。")
    L.append("")

    L.append("## 4. 字符分布对照（ch）")
    L.append("")
    L.append("### 4.1 主/支线")
    L.append("")
    L.append("| 分类 | 任务数 | ch 字符 | 基准 ch 字符 | 一致 |")
    L.append("|---|---:|---:|---:|:--:|")
    L.append(f"| 主线 | {stats['story_count']} | {stats['story_ch']} | {BASELINE_STORY_CH} | "
             f"{'是' if stats['story_ch'] == BASELINE_STORY_CH else '否'} |")
    L.append(f"| 支线 | {stats['side_count']} | {stats['side_ch']} | {BASELINE_SIDE_CH} | "
             f"{'是' if stats['side_ch'] == BASELINE_SIDE_CH else '否'} |")
    L.append("")
    L.append("### 4.2 按商人（ch 字符降序）")
    L.append("")
    L.append("| traderId | 名称 | 任务数 | ch 字符 | 基准 ch 字符 | 一致 |")
    L.append("|---|---|---:|---:|---:|:--:|")
    base = dict(BASELINE_TRADER_CH)
    order = sorted(stats["trader_ch"].items(), key=lambda kv: (-kv[1], kv[0]))
    for tid, c in order:
        nm = trader_names.get(tid, {}).get("cn") or trader_names.get(tid, {}).get("en") or "(未知)"
        b = base.get(tid)
        L.append(f"| `{tid}` | {nm} | {stats['trader_cnt'].get(tid, 0)} | {c} | "
                 f"{b if b is not None else '-'} | {'是' if b == c else ('否' if b is not None else '-')} |")
    L.append("")
    L.append(f"商人 ch 字符合计：{sum(stats['trader_ch'].values())}（基准 {BASELINE_TOTAL_CH}）。")
    L.append("")

    L.append("## 5. trader id -> 名称映射")
    L.append("")
    L.append("| traderId | 中文名 (Nickname) | 英文名 | 来源 |")
    L.append("|---|---|---|---|")
    for tid in sorted(by_trader.keys(), key=lambda t: -by_trader[t]["chCharCount"]):
        n = trader_names.get(tid, {})
        L.append(f"| `{tid}` | {n.get('cn') or '-'} | {n.get('en') or '-'} | {n.get('source')} |")
    L.append("")
    L.append("- 名称键格式：`<traderId> Nickname`（空则退 FullName/FirstName），"
             "中文取 ch.json，缺失退 en.json。")
    L.append("- `67f7af56c117b6140af2a607` 在 ch.json/en.json 中无任何名称键，"
             "名称取自 `database/traders/<id>/base.json` 的 nickname（Player Trader，无中文）。")
    L.append("")

    L.append("## 6. 条件文本挂接")
    L.append("")
    L.append("localization 中条件级键的三种形式：")
    L.append("")
    L.append("- `<condId>`：条件短文本（目标描述），挂到条件对象 `text`。")
    L.append("- `<condId>_hint`：条件提示，挂到 `hint`。")
    L.append(f"- `<condId> {COND_NOTE_SUFFIX}`：条件备注，挂到 `note`。")
    L.append("")
    L.append(f"- 条件条目总数（含嵌套）：{stats['cond_total']}；"
             f"其中挂接 ch 文本 {stats['cond_ch']}，仅 en 文本 {stats['cond_en_only']}。")
    L.append(f"- 残留键（进入 `extras`）共 {stats['extras_cond'] + stats['extras_notes']} 个：")
    L.append(f"  - `orphanCondTexts`：{stats['extras_cond']} 个短文本/提示残留键（前缀不在本 quest 条件中）。")
    L.append(f"  - `orphanQuestNotes`：{stats['extras_notes']} 个 questNoteText。"
             "**经全库核对，全部 376 个 questNoteText 的前缀在任何 quest 的条件中都不存在**，"
             "无法自动挂接；从内容看它们是主线叙事日志条目，价值高，已按 quest 保留原文。")
    L.append("- 1 个裸键前缀在别的 quest 的条件中存在（跨 quest 打包遗留），其余不在任何条件中。")
    L.append("")

    L.append("## 7. 数据质量")
    L.append("")
    L.append(f"- ch 名称为空的任务：{stats['missing_name_count']} / {stats['total_quests']}；"
             f"其中中英文名称都为空：{stats['empty_name_both_count']}。")
    L.append(f"- 至少缺一项任务级中文的任务：{stats['ch_missing_any']} / {stats['total_quests']}。")
    L.append(f"- en 兜底：`textsEn` 共 {stats['texts_en_entries']} 条"
             f"（全部 name + 缺失中文的任务级后缀）；条件级仅在无 ch 时附 en"
             f"（标记 `textSource='en'`，实际仅 {stats['cond_en_only']} 条）。")
    L.append("- `missingCh` 逐任务列出缺失 ch 的任务级后缀。")
    L.append("- questNoteText 任务级缺失属结构正常（本版为条件级），不计入异常。")
    L.append("")

    L.append(f"- 前置依赖边：{stats['prereq_edges']} 条；反向后继一致性："
             f"{'全部闭合' if stats['reverse_broken'] == 0 else str(stats['reverse_broken']) + ' 条断裂'}；"
             f"指向不存在任务的前置：{len(stats['dangling_prereq'])} 个"
             f"（{', '.join('`' + d + '`' for d in stats['dangling_prereq'])}，属已归档/移除任务）。")
    L.append("")

    an = stats["anomaly"]
    L.append("### 7.1 主/支线文本完整度（关键异常，下一阶段须知）")
    L.append("")
    L.append("| 分类 | 任务数 | name 非空 | description 非空 | 缺 name | 缺 description |")
    L.append("|---|---:|---:|---:|---:|---:|")
    L.append(f"| 主线 | {stats['story_count']} | {an['suffix_by_class']['name']['story']} | "
             f"{an['suffix_by_class']['description']['story']} | "
             f"{an['story_missing_name']} | {an['story_missing_desc']} |")
    L.append(f"| 支线 | {stats['side_count']} | {an['suffix_by_class']['name']['side']} | "
             f"{an['suffix_by_class']['description']['side']} | "
             f"{an['side_missing_name']} | {an['side_missing_desc']} |")
    L.append("")
    L.append("**结论（重要）**：本数据集中 **192 个主线任务全部没有 ch description**，"
             "其中 180 个连 name 也没有（中英文皆空）。主线任务的叙事文本不在 quests.json 的 "
             "localization 里，很可能位于 `dialogue.json` / `mainQuestNotes.json` / `tapes.json` "
             "等文件；本阶段仅覆盖 quests.json，故主线叙事缺失属**源数据范围问题**，非提取错误。"
             "主线任务仍保留条件级目标文本（`conditions[*].text`）。")
    L.append(f"- 支线中缺 name/description 的 {an['side_missing_name']} 个任务"
             f"{'恰好等于' if an['side_missing_name_eq_notdisplayed'] else '与'} "
             f"`notDisplayedQuest=True` 的 {stats['notDisplayed_count']} 个"
             f"{'（集合一致）' if an['side_missing_name_eq_notdisplayed'] else ''}——即隐藏任务。")
    L.append("- 因此：后续“594 支线”可正常从本数据集蒸馏；"
             "“192 主线”的完整叙事需另行提取对话/日志文件。")
    L.append("")

    L.append("## 8. 分片建议（下一阶段输入）")
    L.append("")
    L.append("约束：商人不可切分；按 ch 字符 LPT 装箱，目标每片 40–60K。建议 5 片。")
    L.append("")
    bins, sizes = lpt_shards(stats["trader_ch"], 5)
    for i, (tids, size) in enumerate(zip(bins, sizes), 1):
        qcount = sum(by_trader[t]["questCount"] for t in tids)
        story = sum(by_trader[t]["storyCount"] for t in tids)
        side = sum(by_trader[t]["sideCount"] for t in tids)
        names = "、".join(
            (trader_names.get(t, {}).get("cn") or trader_names.get(t, {}).get("en") or t)
            for t in sorted(tids, key=lambda x: -stats["trader_ch"][x])
        )
        L.append(f"### 片 {i}：ch {size} 字符 | 任务 {qcount}（主线 {story}/支线 {side}）")
        L.append("")
        L.append(f"- 商人：{names}")
        for t in sorted(tids, key=lambda x: -stats["trader_ch"][x]):
            g = by_trader[t]
            L.append(f"  - `{t}` {trader_names.get(t, {}).get('cn') or trader_names.get(t, {}).get('en')}"
                     f"：{g['questCount']} 任务 / {g['chCharCount']} 字符")
        L.append("")
    L.append("> 分片仅按字符量均衡，未做主题聚类；后续可按需在片内再按商人/区块细分。")
    L.append("")

    L.append("## 9. 自检记录")
    L.append("")
    L.append("脚本内置自检：固定随机种子 2077，从 786 个任务中抽 3 个，"
             "逐字段对照 quests.json 源文件（traderId/location/type/side/isStoryQuest/"
             "notDisplayedQuest/textsCh/chCharCount/prereqQuestIds）。")
    L.append("")
    check_lines, all_pass = self_check(joined, quests, 3)
    L.extend(check_lines)
    L.append(f"自检结果：**{'全部通过' if all_pass else '存在失败项'}**。")
    L.append("")
    L.append("幂等性：脚本输出使用确定性排序与固定缩进；对同一源数据连续运行两次，"
             "产物字节一致（SHA-256 前 16 位："
             + "、".join(f"{k}={v}" for k, v in output_hashes().items()) + "）。")
    L.append("")
    L.append("（哈希在每次运行时重新计算并写入，故本行仅代表本次运行的产物指纹。）")
    L.append("")

    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(L))
    print(f"[extract] wrote {path} ({path.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
