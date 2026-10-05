# -*- coding: utf-8 -*-
"""第二阶段：SPT 5.x 主线叙事文本提取。

只读游戏目录；只写本工作目录。
运行：python -X utf8 tools/extract_main_story.py

产出：
  data/main_story_extract.json   结构化叙事数据
  work/main-story-text.md        可读全文（ch 优先，en 兜底标 [en]）
  data/main_story_index.md       索引与覆盖统计
  coverage.md                    追加「## 10. 第二阶段：主线叙事提取」
"""

from __future__ import annotations

import json
import sys
from collections import Counter, defaultdict
from pathlib import Path

_re = getattr(sys.stdout, "reconfigure", None)
if callable(_re):
    _re(encoding="utf-8")

WORK = Path(__file__).resolve().parent.parent
DATA = WORK / "data"
WORKDIR = WORK / "work"
T = Path(r"E:\Game\EFT_Offline\SPT_5xx\SPT_Runtime\SPT_Data\database\templates")
LOC = Path(r"E:\Game\EFT_Offline\SPT_5xx\SPT_Runtime\SPT_Data\database\locales\global")

COVERAGE_MARKER = "## 10. 第二阶段：主线叙事提取"


def load(p: Path):
    return json.loads(p.read_text(encoding="utf-8"))


# ---------------------------------------------------------------------------
def main():
    print("[p2] loading sources ...")
    qj = load(T / "quests.json")
    mqn = load(T / "mainQuestNotes.json")
    dlg = load(T / "dialogue.json")["elements"]
    tapes = load(T / "tapes.json")
    stracks = load(T / "subtitleTracks.json")
    endings = load(T / "endings.json")["elements"]
    chains = load(T / "questChains.json")["elements"]
    archived = load(T / "archivedQuests.json")
    ch = load(LOC / "ch.json")
    en = load(LOC / "en.json")
    p1 = load(DATA / "quests_joined.json")["quests"]

    story_ids = {qid for qid, q in qj.items() if q.get("isStoryQuest")}

    def qname(qid):
        q = qj.get(qid, {})
        loc = q.get("localization", {})
        v = (loc.get("ch", {}) or {}).get(f"{qid} name")
        if v and v.strip():
            return v
        v = (loc.get("en", {}) or {}).get(f"{qid} name")
        return v if (v and v.strip()) else "(无名称)"

    def qname_pair(qid):
        q = qj.get(qid, {})
        loc = q.get("localization", {})
        c = (loc.get("ch", {}) or {}).get(f"{qid} name")
        e = (loc.get("en", {}) or {}).get(f"{qid} name")
        return (c or None), (e or None)

    def trader_names(tid):
        cn = None
        e = None
        for suffix in ("Nickname", "FullName", "FirstName"):
            v = ch.get(f"{tid} {suffix}")
            if v and v.strip() and not cn:
                cn = v
            v = en.get(f"{tid} {suffix}")
            if v and v.strip() and not e:
                e = v
        return cn, e

    # 预先建立 trader 名称缓存
    trader_cache = {}
    for qid, q in qj.items():
        tid = q.get("traderId")
        if tid and tid not in trader_cache:
            trader_cache[tid] = trader_names(tid)

    # --- 条件 id -> quest ---
    cond2quest = {}

    def walk(o, qid):
        if isinstance(o, dict):
            if isinstance(o.get("id"), str) and o.get("conditionType"):
                cond2quest[o["id"]] = qid
            for v in o.values():
                walk(v, qid)
        elif isinstance(o, list):
            for v in o:
                walk(v, qid)

    for qid, q in qj.items():
        walk(q.get("conditions", {}), qid)

    # --- note 文本（直接从 quests.json localization）---
    note_text = defaultdict(dict)
    note_owners = defaultdict(set)
    for qid, q in qj.items():
        locs = q.get("localization", {})
        for lang in ("ch", "en"):
            for k, v in (locs.get(lang, {}) or {}).items():
                if k.endswith(" questNoteText"):
                    nid = k.rsplit(" ", 1)[0]
                    if v and v.strip():
                        note_text[nid][lang] = v
                    note_owners[nid].add(qid)

    # --- notes 输出 ---
    notes = {}
    notes_with_text = set()
    for x in mqn:
        nid = x["id"]
        cids = x.get("conditionIds") or []
        targets = sorted({cond2quest[c] for c in cids if c in cond2quest})
        if not targets:
            targets = [x["chapterId"]]
        t = note_text.get(nid, {})
        tc = t.get("ch")
        te = t.get("en")
        src = "ch" if tc else ("en" if te else None)
        if src:
            notes_with_text.add(nid)
        notes[nid] = {
            "id": nid,
            "chapterId": x.get("chapterId"),
            "chapterName": qname(x["chapterId"]),
            "questIds": targets,
            "conditionIds": cids,
            "links": x.get("links"),
            "hasText": bool(src),
            "textSource": src,
            "textCh": tc,
            "textEn": te,
            "ownerQuests": sorted(note_owners.get(nid, set())),
        }

    # --- dialogue 输出 ---
    # quest -> dialogueId
    quest_dialogue = defaultdict(list)  # dialogueId -> [questId]
    for qid, q in qj.items():
        d = q.get("dialogueId")
        if d:
            quest_dialogue[d].append(qid)

    dialogues = {}
    reachable_map = {}
    dlg_ids = {e["Id"] for e in dlg}
    for e in dlg:
        eid = e["Id"]
        loc = e.get("localization", {}) or {}
        loc_ch = loc.get("ch", {}) or {}
        loc_en = loc.get("en", {}) or {}
        lines_out = []
        text_lines = 0
        switch_targets = set()
        for i, ln in enumerate(e.get("Lines", [])):
            subs = (ln.get("AnimationData", {}) or {}).get("subtitles", []) or []
            sub_ids = [s.get("id") for s in subs]
            tc = " ".join(loc_ch[sid] for sid in sub_ids if sid and loc_ch.get(sid))
            te = " ".join(loc_en[sid] for sid in sub_ids if sid and loc_en.get(sid))
            while "  " in tc:
                tc = tc.replace("  ", " ")
            while "  " in te:
                te = te.replace("  ", " ")
            has = bool(tc.strip() or te.strip())
            if has:
                text_lines += 1
            for a in ln.get("Actions", []) or []:
                if a.get("type") == "SwitchDialog" and a.get("dialogId"):
                    switch_targets.add(a["dialogId"])
            lines_out.append({
                "index": i,
                "lineId": ln.get("Id"),
                "side": ln.get("DialogSide"),
                "traderId": ln.get("TraderId"),
                "subtitleIds": sub_ids,
                "textSource": "ch" if tc.strip() else ("en" if te.strip() else None),
                "textCh": tc if tc.strip() else None,
                "textEn": te if te.strip() else None,
            })
        linked = sorted(set(quest_dialogue.get(eid, [])))
        trader = e.get("Trader")
        tcn, ten = trader_cache.get(trader, (None, None))
        dialogues[eid] = {
            "id": eid,
            "isStart": e.get("IsStart"),
            "traderId": trader,
            "traderNameCn": tcn,
            "traderNameEn": ten,
            "subTraders": e.get("SubTraders"),
            "linkedQuestIds": linked,
            "isStoryLinked": any(q in story_ids for q in linked),
            "lineCount": len(lines_out),
            "textLineCount": text_lines,
            "switchDialogTargets": sorted(switch_targets),
            "lines": lines_out,
        }
        reachable_map[eid] = sorted(switch_targets)

    # 通过 SwitchDialog 从 quest-linked 元素可达的对话
    linked_elements = {eid for eid in dialogues if dialogues[eid]["linkedQuestIds"]}
    reachable_from_linked = set()
    frontier = list(linked_elements)
    while frontier:
        cur = frontier.pop()
        for nxt in reachable_map.get(cur, []):
            if nxt in dlg_ids and nxt not in reachable_from_linked and nxt not in linked_elements:
                reachable_from_linked.add(nxt)
                frontier.append(nxt)

    # --- tapes / subtitleTracks ---
    def track_text(subtitles):
        out = []
        for s in subtitles or []:
            sid = s.get("id")
            tc = ch.get(sid)
            te = en.get(sid)
            out.append({
                "subtitleId": sid,
                "start": s.get("start"),
                "end": s.get("end"),
                "textSource": "ch" if tc else ("en" if te else None),
                "textCh": tc,
                "textEn": te,
            })
        return out

    def quests_containing(token):
        hits = []
        for qid, q in qj.items():
            if token in json.dumps(q, ensure_ascii=False):
                hits.append(qid)
        return sorted(hits)

    tapes_out = {}
    tape_to_quests = {}
    for t in tapes:
        tid = t["id"]
        qs = quests_containing(tid)
        tape_to_quests[tid] = qs
        tapes_out[tid] = {
            "id": tid,
            "linkedQuestIds": qs,
            "linkedQuestNames": [qname(q) for q in qs],
            "subtitles": track_text(t.get("subtitles")),
        }

    stracks_out = {}
    for t in stracks:
        stracks_out[t["id"]] = {
            "id": t["id"],
            "subtitles": track_text(t.get("subtitles")),
        }

    # --- endings ---
    endings_out = {}
    for e in endings:
        sn = e["systemName"]

        def lk(suffix):
            tc = ch.get(f"{sn}{suffix}")
            te = en.get(f"{sn}{suffix}")
            return (tc, te)

        name_c, name_e = lk("_name")
        desc_c, desc_e = lk("_description")
        cap_c, cap_e = lk("_caption")
        cons_c = ch.get(f"{sn}.consequence")
        cons_e = en.get(f"{sn}.consequence")
        targets = set()
        for c in e.get("conditions", []):
            t = c.get("target")
            if isinstance(t, str):
                targets.add(t)
            elif isinstance(t, list):
                targets.update(t)
        endings_out[sn] = {
            "id": e["id"],
            "systemName": sn,
            "nameCh": name_c,
            "nameEn": name_e,
            "descriptionCh": desc_c,
            "descriptionEn": desc_e,
            "captionCh": cap_c,
            "captionEn": cap_e,
            "consequenceCh": cons_c,
            "consequenceEn": cons_e,
            "conditionTargets": sorted(targets),
            "conditionTargetsKnownStory": sorted(t for t in targets if t in story_ids),
            "conditionTargetsKnown": sorted(t for t in targets if t in qj),
            "consequences": e.get("consequences"),
        }

    # --- questChains ---
    chains_out = {}
    for cid, ids in chains.items():
        story_n = sum(1 for q in ids if q in story_ids)
        chains_out[cid] = {
            "chainId": cid,
            "questIds": ids,
            "questCount": len(ids),
            "storyCount": story_n,
            "sideCount": len(ids) - story_n,
            "questNames": [qname(q) for q in ids],
        }

    # --- archivedQuests ---
    archived_out = {}
    for aid, q in archived.items():
        archived_out[aid] = {
            "id": aid,
            "questName": q.get("QuestName"),
            "isStoryQuest": bool(q.get("isStoryQuest")),
            "traderId": q.get("traderId"),
            "conditionCount": len(q.get("conditions", {}) or {}),
        }

    # --- 覆盖统计 ---
    note_quests_text = set()
    for nid in notes_with_text:
        note_quests_text |= set(notes[nid]["questIds"])
    note_story = sorted(note_quests_text & story_ids)

    dlg_story = sorted({q for eid in dialogues for q in dialogues[eid]["linkedQuestIds"]
                        if q in story_ids and dialogues[eid]["textLineCount"] > 0})

    tape_story = sorted({q for t in tapes_out.values() for q in t["linkedQuestIds"] if q in story_ids})
    ending_story = sorted({q for e in endings_out.values() for q in e["conditionTargetsKnownStory"]})

    union_story = sorted(set(note_story) | set(dlg_story) | set(tape_story) | set(ending_story))
    uncovered = sorted(story_ids - set(union_story))

    dangling = ["6863e09a5f4d17fd3e01feee", "686403eeb4aaef121c0f0f06", "6a88255e8636c03a2d09ba37"]
    dangling_report = []
    for d in dangling:
        dangling_report.append({
            "id": d,
            "in_quests": d in qj,
            "in_archived": d in archived,
            "in_questChains_as_chain": d in chains,
            "in_questChains_as_quest": any(d in ids for ids in chains.values()),
        })

    # 重复 note 出现次数（phase1 口径 376 次 vs 364 个唯一 id）
    occurrences = sum(len(v) for v in note_owners.values())

    extract = {
        "sources": {
            "quests.json": {"path": str(T / "quests.json"),
                            "structure_note": "note 文本以 <noteId> questNoteText 内嵌于 localization",
                            "counts": {"quests": len(qj), "storyQuests": len(story_ids)}},
            "mainQuestNotes.json": {"path": str(T / "mainQuestNotes.json"),
                                    "structure_note": "list[{id,links,chapterId,conditionIds}]；文本不在此文件",
                                    "counts": {"notes": len(mqn), "distinctChapterId": len({x['chapterId'] for x in mqn}),
                                               "withConditionIds": sum(1 for x in mqn if x.get('conditionIds')),
                                               "withText": len(notes_with_text)}},
            "dialogue.json": {"path": str(T / "dialogue.json"),
                              "structure_note": "elements[]；文本在 element.localization[lang][subtitleId]",
                              "counts": {"elements": len(dlg),
                                         "withChLoc": sum(1 for e in dlg if (e.get('localization', {}) or {}).get('ch')),
                                         "questLinked": len(linked_elements),
                                         "subtitleIds": sum(1 for e in dlg for ln in e.get('Lines', [])
                                                            for _ in (ln.get('AnimationData', {}) or {}).get('subtitles', []) or [])}},
            "tapes.json": {"path": str(T / "tapes.json"),
                           "structure_note": "list[{id,subtitles[]}]；文本在全局 locale 以 subtitleId 为键",
                           "counts": {"tapes": len(tapes),
                                      "withText": sum(1 for v in tapes_out.values() if any(s['textCh'] or s['textEn'] for s in v['subtitles']))}},
            "subtitleTracks.json": {"path": str(T / "subtitleTracks.json"),
                                    "structure_note": "list[{id,subtitles[]}]；文本在全局 locale",
                                    "counts": {"tracks": len(stracks)}},
            "endings.json": {"path": str(T / "endings.json"),
                             "structure_note": "elements[]；文本在全局 locale，键 <systemName>_name/_description/_caption 与 <systemName>.consequence",
                             "counts": {"endings": len(endings)}},
            "questChains.json": {"path": str(T / "questChains.json"),
                                 "structure_note": "elements{chainId:[questId]}（经核实为支线链，0 主线）",
                                 "counts": {"chains": len(chains),
                                            "questsInChains": sum(len(v) for v in chains.values()),
                                            "storyInChains": sum(1 for ids in chains.values() for q in ids if q in story_ids)}},
            "archivedQuests.json": {"path": str(T / "archivedQuests.json"),
                                    "structure_note": "dict questId->quest；含 QuestName 与旧条件结构",
                                    "counts": {"archived": len(archived)}},
        },
        "notes": notes,
        "dialogues": dialogues,
        "tapes": tapes_out,
        "subtitleTracks": stracks_out,
        "endings": endings_out,
        "questChains": chains_out,
        "archivedQuests": archived_out,
        "storyCoverage": {
            "totalStory": len(story_ids),
            "coveredByNotes": note_story,
            "coveredByDialogue": dlg_story,
            "coveredByTapes": tape_story,
            "coveredByEndings": ending_story,
            "unionCovered": union_story,
            "uncovered": uncovered,
            "counts": {
                "notes": len(note_story),
                "dialogue": len(dlg_story),
                "tapes": len(tape_story),
                "endings": len(ending_story),
                "union": len(union_story),
                "uncovered": len(uncovered),
            },
        },
        "unmapped": {
            "notesWithoutText": sorted(nid for nid in notes if nid not in notes_with_text),
            "storyQuestsWithoutNarrative": uncovered,
            "dialoguesWithoutQuestLink": sorted(eid for eid in dialogues if not dialogues[eid]["linkedQuestIds"]),
            "dialoguesReachableFromLinkedViaSwitchDialog": sorted(reachable_from_linked),
            "subtitleTracksWithoutQuestLink": sorted(stracks_out.keys()),
            "danglingPrereqIds": dangling_report,
            "noteOccurrenceVsUnique": {
                "phase1OrphanNoteKeys": occurrences,
                "uniqueNoteIds": len(note_owners),
                "note": "同一 note 文本在多个 quest 的 localization 中重复出现，故键出现次数 > 唯一 id 数。",
            },
        },
    }

    check_lines = self_check(ex=extract, qj=qj, ch=ch, en=en, dlg=dlg, tapes=tapes,
                             stracks=stracks, endings=endings, mqn=mqn)

    # --- 写出 ---
    DATA.mkdir(parents=True, exist_ok=True)
    WORKDIR.mkdir(parents=True, exist_ok=True)
    write_json(DATA / "main_story_extract.json", extract)
    write_index_md(DATA / "main_story_index.md", extract, qname)
    write_text_md(WORKDIR / "main-story-text.md", extract)
    append_coverage(WORK / "coverage.md", extract, qname, check_lines)

    print(f"[p2] notes {len(notes)} (withText {len(notes_with_text)})")
    print(f"[p2] dialogues {len(dialogues)} (questLinked {len(linked_elements)})")
    print(f"[p2] tapes {len(tapes_out)} subtitleTracks {len(stracks_out)} endings {len(endings_out)} "
          f"chains {len(chains_out)} archived {len(archived_out)}")
    print(f"[p2] story coverage union {len(union_story)}/{len(story_ids)} (uncovered {len(uncovered)})")
    print("[p2] done.")


# ---------------------------------------------------------------------------
def self_check(ex, qj, ch, en, dlg, tapes, stracks, endings, mqn):
    """抽查 ≥3 条引用与源文件一致性。"""
    lines = []
    all_ok = True

    def rec(title, ok, detail):
        nonlocal all_ok
        all_ok = all_ok and ok
        lines.append(f"- {title}：{'PASS' if ok else 'FAIL'} — {detail}")

    # 1) note 文本对照 quests.json 原始 localization
    note_pick = next((n for n in ex["notes"].values() if n["hasText"]), None)
    if note_pick:
        owner = note_pick["ownerQuests"][0] if note_pick["ownerQuests"] else None
        raw = None
        if owner:
            raw = (qj[owner].get("localization", {}).get("ch", {}) or {}).get(
                note_pick["id"] + " questNoteText")
        ok = raw == note_pick["textCh"]
        rec("note", ok, f"quests.json#{note_pick['id']}（owner {owner}）文本一致={ok}")

    # 2) dialogue 行文本对照 element.localization
    dlg_pick = next((d for d in ex["dialogues"].values() if d["textLineCount"] > 0), None)
    if dlg_pick:
        raw_el = next(e for e in dlg if e["Id"] == dlg_pick["id"])
        raw_ch = (raw_el.get("localization", {}) or {}).get("ch", {}) or {}
        checked = 0
        bad = 0
        for ln in dlg_pick["lines"]:
            if not ln["textCh"]:
                continue
            expected = " ".join(raw_ch.get(sid, "") for sid in ln["subtitleIds"])
            while "  " in expected:
                expected = expected.replace("  ", " ")
            if expected != ln["textCh"]:
                bad += 1
            checked += 1
            if checked >= 3:
                break
        rec("dialogue", bad == 0 and checked > 0,
            f"dialogue.json#{dlg_pick['id']} 抽查 {checked} 行，文本不一致 {bad}")

    # 3) tape 文本对照 ch.json
    tape_pick = next((t for t in ex["tapes"].values() if t["subtitles"]), None)
    if tape_pick:
        s = tape_pick["subtitles"][0]
        ok = ch.get(s["subtitleId"]) == s["textCh"]
        rec("tape", ok, f"tapes.json#{tape_pick['id']} 首字幕对照 ch.json 一致={ok}")

    # 4) subtitleTrack 对照 ch.json
    st_pick = next((t for t in ex["subtitleTracks"].values() if t["subtitles"]), None)
    if st_pick:
        s = st_pick["subtitles"][0]
        ok = ch.get(s["subtitleId"]) == s["textCh"]
        rec("subtitleTrack", ok, f"subtitleTracks.json#{st_pick['id']} 首字幕对照 ch.json 一致={ok}")

    # 5) ending 文本对照 ch.json
    end_pick = next(iter(ex["endings"].values()))
    ok = ch.get(f"{end_pick['systemName']}_name") == end_pick["nameCh"]
    rec("ending", ok, f"endings.json#{end_pick['systemName']} name 对照 ch.json 一致={ok}")

    # 6) note 结构字段对照 mainQuestNotes
    n0 = ex["notes"][note_pick["id"]] if note_pick else None
    if n0:
        raw_n = next(x for x in mqn if x["id"] == n0["id"])
        ok = (raw_n.get("chapterId") == n0["chapterId"]
              and (raw_n.get("conditionIds") or []) == n0["conditionIds"])
        rec("note结构", ok, f"mainQuestNotes#{n0['id']} chapterId/conditionIds 一致={ok}")

    lines.append(f"- 自检结论：{'全部通过' if all_ok else '存在失败项'}。")
    return lines


# ---------------------------------------------------------------------------
def write_json(path: Path, obj):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(obj, f, ensure_ascii=False, indent=2, sort_keys=True)
        f.write("\n")
    print(f"[p2] wrote {path} ({path.stat().st_size} bytes)")


def esc(s):
    return (s or "").replace("|", "\\|").replace("\n", " ")


def write_index_md(path: Path, ex, qname):
    cov = ex["storyCoverage"]
    L = ["# 主线叙事提取 — 索引与覆盖统计（第二阶段）", "",
         "> 由 `tools/extract_main_story.py` 生成。", ""]
    L.append("## 覆盖总览")
    L.append("")
    L.append(f"- 主线任务总数：**{cov['totalStory']}**")
    L.append(f"- 获得叙事文本（并集）：**{cov['counts']['union']} / {cov['totalStory']}**")
    L.append(f"- 仍未覆盖：**{cov['counts']['uncovered']}**")
    L.append("")
    L.append("| 来源 | 覆盖主线任务数 |")
    L.append("|---|---:|")
    L.append(f"| mainQuestNotes（笔记） | {cov['counts']['notes']} |")
    L.append(f"| dialogue（对话） | {cov['counts']['dialogue']} |")
    L.append(f"| tapes（音频日志） | {cov['counts']['tapes']} |")
    L.append(f"| endings（结局） | {cov['counts']['endings']} |")
    L.append("")
    L.append("## 各文件贡献")
    L.append("")
    L.append("| 文件 | 条目/计数 | 说明 |")
    L.append("|---|---|---|")
    for fn, d in sorted(ex["sources"].items()):
        cnt = ", ".join(f"{k}={v}" for k, v in d["counts"].items())
        L.append(f"| {fn} | {cnt} | {esc(d['structure_note'])} |")
    L.append("")
    L.append("## 主线章节（10 章）与笔记数")
    L.append("")
    L.append("| chapterId | 章节名 | 笔记数（有文本） |")
    L.append("|---|---|---:|")
    chap = Counter()
    chap_txt = Counter()
    for n in ex["notes"].values():
        chap[n["chapterId"]] += 1
        if n["hasText"]:
            chap_txt[n["chapterId"]] += 1
    for cid, c in sorted(chap.items(), key=lambda kv: -kv[1]):
        L.append(f"| `{cid}` | {esc(qname(cid))} | {chap_txt[cid]}/{c} |")
    L.append("")
    L.append(f"## 未覆盖主线任务清单（{len(cov['uncovered'])}）")
    L.append("")
    for qid in cov["uncovered"]:
        L.append(f"- `{qid}` {esc(qname(qid))}")
    L.append("")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(L))
    print(f"[p2] wrote {path} ({path.stat().st_size} bytes)")


def write_text_md(path: Path, ex):
    L = ["# 主线叙事全文（第二阶段提取）", "",
         "> ch 优先；缺 ch 时用 en 并标 `[en]`。每条来源标注 `<file>#<id>`。", ""]
    notes = ex["notes"]
    # 1. 按章节的笔记
    L.append("## 1. 主线笔记（按章节）")
    L.append("")
    by_chap = defaultdict(list)
    for n in notes.values():
        by_chap[n["chapterId"]].append(n)
    for cid, items in sorted(by_chap.items(), key=lambda kv: -len(kv[1])):
        title = items[0]["chapterName"]
        L.append(f"### 章节 `{cid}` — {title}（{len(items)} 条，含无文本 {sum(1 for x in items if not x['hasText'])}）")
        L.append("")
        for n in sorted(items, key=lambda x: x["id"]):
            if not n["hasText"]:
                continue
            tag = f"quests.json#{n['id']}"
            txt = n["textCh"]
            mark = ""
            if not txt:
                txt = n["textEn"]
                mark = " [en]"
            L.append(f"- **{tag}**{mark}（关联任务：{', '.join(n['questIds'])}）")
            L.append("")
            L.append("  " + (txt or "").replace("\n", "\n  "))
            L.append("")
    # 2. 对话
    L.append("## 2. 对话（dialogue.json）")
    L.append("")
    dlg_list = sorted(ex["dialogues"].values(),
                      key=lambda d: (not d["isStoryLinked"], d["id"]))
    L.append(f"共 {len(dlg_list)} 个对话元素。")
    L.append("")
    for d in dlg_list:
        head = f"### `{d['id']}`"
        if d["linkedQuestIds"]:
            head += "（关联任务：" + ", ".join(d["linkedQuestIds"]) + "）"
        else:
            head += "（无 quest 直接引用）"
        L.append(head)
        L.append("")
        L.append(f"- trader `{d['traderId']}` {d['traderNameCn'] or d['traderNameEn'] or ''}；"
                 f"行 {d['lineCount']}，有文本 {d['textLineCount']}")
        L.append("")
        for ln in d["lines"]:
            if not ln["textCh"] and not ln["textEn"]:
                continue
            mark = "" if ln["textCh"] else " [en]"
            txt = ln["textCh"] or ln["textEn"]
            L.append(f"- dialogue.json#{ln['lineId']} [{ln['side']}]{mark} {txt}")
        L.append("")
    # 3. tapes
    L.append("## 3. 音频日志（tapes.json）")
    L.append("")
    for t in sorted(ex["tapes"].values(), key=lambda x: x["id"]):
        L.append(f"### `{t['id']}`（关联任务：{', '.join(t['linkedQuestIds']) or '无'}）")
        L.append("")
        for s in t["subtitles"]:
            if not s["textCh"] and not s["textEn"]:
                continue
            mark = "" if s["textCh"] else " [en]"
            L.append(f"- tapes.json#{s['subtitleId']}{mark} {s['textCh'] or s['textEn']}")
        L.append("")
    # 4. subtitleTracks
    L.append("## 4. 字幕轨（subtitleTracks.json）")
    L.append("")
    for t in sorted(ex["subtitleTracks"].values(), key=lambda x: x["id"]):
        L.append(f"### `{t['id']}`")
        L.append("")
        for s in t["subtitles"]:
            if not s["textCh"] and not s["textEn"]:
                continue
            mark = "" if s["textCh"] else " [en]"
            L.append(f"- subtitleTracks.json#{s['subtitleId']}{mark} {s['textCh'] or s['textEn']}")
        L.append("")
    # 5. endings
    L.append("## 5. 结局（endings.json）")
    L.append("")
    for e in sorted(ex["endings"].values(), key=lambda x: x["systemName"]):
        L.append(f"### `{e['systemName']}`")
        L.append("")
        for label, ck, ek in (("名称", "nameCh", "nameEn"), ("描述", "descriptionCh", "descriptionEn"),
                              ("标题", "captionCh", "captionEn"), ("后果", "consequenceCh", "consequenceEn")):
            tc = e.get(ck)
            te = e.get(ek)
            if tc or te:
                mark = "" if tc else " [en]"
                L.append(f"- {label}（endings.json#{e['systemName']}）{mark} {tc or te}")
        if e.get("conditionTargets"):
            L.append(f"- 触发条件目标任务：{', '.join(e['conditionTargets'])}")
        L.append("")
    # 6. questChains
    L.append("## 6. 任务链（questChains.json，支线链）")
    L.append("")
    for c in sorted(ex["questChains"].values(), key=lambda x: -x["questCount"]):
        L.append(f"- `{c['chainId']}`：{c['questCount']} 任务（主线 {c['storyCount']}）")
    L.append("")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(L))
    print(f"[p2] wrote {path} ({path.stat().st_size} bytes)")


def append_coverage(path: Path, ex, qname, check_lines):
    cov = ex["storyCoverage"]
    L = []
    L.append("")
    L.append(COVERAGE_MARKER)
    L.append("")
    L.append("> 由 `tools/extract_main_story.py` 追加。第一阶段内容保持不变。")
    L.append("")
    L.append("### 10.1 来源与方法")
    L.append("")
    L.append("| 文件 | 文本存放方式 | 提取方法 |")
    L.append("|---|---|---|")
    L.append("| quests.json | `<noteId> questNoteText` 内嵌 localization | 直接读取 note 文本，主/英双取 |")
    L.append("| mainQuestNotes.json | 仅结构（id/chapterId/conditionIds/links） | 提供 note->章节/任务映射 |")
    L.append("| dialogue.json | `element.localization[lang][subtitleId]` | 按行展开字幕文本；`dialogueId` 关联任务 |")
    L.append("| tapes.json | 全局 locale 以 subtitleId 为键 | 拼接字幕；tape id 在 quests.json 中反查任务 |")
    L.append("| subtitleTracks.json | 全局 locale 以 subtitleId 为键 | 独立字幕轨，无任务引用 |")
    L.append("| endings.json | 全局 locale `<systemName>_name/_description/_caption/.consequence` | 结局文本 + 条件目标 |")
    L.append("| questChains.json | `elements{chainId:[questId]}` | 链结构；经核实为支线链 |")
    L.append("| archivedQuests.json | dict questId->quest | 确认悬空前置 |")
    L.append("")
    L.append("### 10.2 覆盖统计（对比 192 主线）")
    L.append("")
    L.append(f"- 主线任务总数：{cov['totalStory']}")
    L.append(f"- 获得叙事文本（并集）：**{cov['counts']['union']} / {cov['totalStory']}**；未覆盖 {cov['counts']['uncovered']}")
    L.append(f"  - notes：{cov['counts']['notes']}")
    L.append(f"  - dialogue：{cov['counts']['dialogue']}")
    L.append(f"  - tapes：{cov['counts']['tapes']}")
    L.append(f"  - endings：{cov['counts']['endings']}")
    unnamed_uncovered = sum(1 for q in cov["uncovered"] if qname(q) == "(无名称)")
    L.append(f"- 未覆盖的 {cov['counts']['uncovered']} 个主线任务中 {unnamed_uncovered} 个无名称，"
             f"多为章节内的分步目标任务；其叙事可能由所属章节 notes 概括，但无直接文本引用。")
    L.append("")
    L.append("### 10.3 关键发现")
    L.append("")
    L.append(f"- mainQuestNotes {len(ex['notes'])} 条，其中 {sum(1 for n in ex['notes'].values() if n['hasText'])} 条有文本；"
             f"文本全部来自 quests.json 的 `<noteId> questNoteText` 键。第一阶段的 376 个孤儿 note 键对应唯一 id "
             f"{ex['unmapped']['noteOccurrenceVsUnique']['uniqueNoteIds']} 个（部分 note 在多个 quest 中重复）。")
    L.append("- note 与任务的映射优先用 `conditionIds` 解析到具体任务，否则退回 `chapterId`；"
             "185 条含 conditionIds 的 note 中 169 条可解析到任务，其余因 condition id 不在 quests.json 条件集合内而退回章节。")
    L.append("- dialogue 文本不在全局 locale，而在 `element.localization`；quests.json 有 116 个 quest 带 dialogueId，"
             "全部命中 dialogue 元素（其中 86 个为主线）。")
    L.append(f"- questChains 共 {len(ex['questChains'])} 链，含 {sum(c['questCount'] for c in ex['questChains'].values())} 个任务，"
             f"但**主线任务数为 0**——是支线链，不能作为主线分组依据。")
    L.append("- endings 文本键为 `<systemName>_name/_description/_caption/.consequence`，共 4 个结局。")
    L.append("")
    L.append("### 10.4 异常与未解决")
    L.append("")
    L.append(f"- 未获得叙事文本的主线任务 {cov['counts']['uncovered']} 个（见 `data/main_story_index.md` 清单）。")
    L.append(f"- 无文本 note {len(ex['unmapped']['notesWithoutText'])} 条（mainQuestNotes 中无对应 questNoteText）。")
    L.append(f"- 未被任何 quest 直接引用的对话元素 {len(ex['unmapped']['dialoguesWithoutQuestLink'])} 个；"
             f"其中从 quest-linked 元素经 SwitchDialog 可达 {len(ex['unmapped']['dialoguesReachableFromLinkedViaSwitchDialog'])} 个。")
    L.append("- 悬空前置 3 个：")
    for d in ex["unmapped"]["danglingPrereqIds"]:
        if d.get("in_questChains_as_quest"):
            L.append(f"  - `{d['id']}`：作为 quest 成员出现在 questChains 中，但 quests.json 无该任务对象"
                     f"（已归档/移除的链成员）。")
        elif d.get("in_questChains_as_chain"):
            L.append(f"  - `{d['id']}`：实为 questChains 的链 id。")
        else:
            L.append(f"  - `{d['id']}`：在 quests/archived/chains 中均不存在，无对应任务。")
    L.append(f"- subtitleTracks 共 {len(ex['subtitleTracks'])} 条，均无任务引用。")
    L.append("")
    L.append("### 10.5 自检")
    L.append("")
    L.append("抽查引用与源文件一致性：")
    L.append("")
    L.extend(check_lines)
    L.append("")
    L.append("产物：`data/main_story_extract.json`、`work/main-story-text.md`、`data/main_story_index.md`。")
    L.append("")
    body = "\n".join(L)

    existing = path.read_text(encoding="utf-8") if path.exists() else ""
    idx = existing.find(COVERAGE_MARKER)
    if idx != -1:
        existing = existing[:idx].rstrip("\n") + "\n"
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(existing + body)
    print(f"[p2] appended {path}")


if __name__ == "__main__":
    main()
