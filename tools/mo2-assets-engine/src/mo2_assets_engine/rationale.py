"""Per-bucket explanation.

Bucket -> short human-readable rationale that explains the underlying rule.

Consumers (CLI, GUI, future MO2 MCP) use this to render "why this verdict"
without re-deriving the engine rules in presentation code.
"""

from __future__ import annotations

from dataclasses import dataclass

from .types import ConflictBucket


@dataclass(frozen=True)
class BucketRationale:
    short: str


_RATIONALES: dict[ConflictBucket, BucketRationale] = {
    ConflictBucket.NO_CONFLICT: BucketRationale(
        short="Only one enabled mod contributes this path; no conflict.",
    ),
    ConflictBucket.LOOSE_OVERWRITES_LOOSE: BucketRationale(
        short=(
            "Both mods ship this path as a loose file. The mod with the higher "
            "modlist priority wins (= the one closer to the top of modlist.txt)."
        ),
    ),
    ConflictBucket.LOOSE_OVERWRITTEN_BY_LOOSE: BucketRationale(
        short=(
            "This mod ships this path as a loose file, but another loose file "
            "from a higher-priority mod wins. Adjust modlist priority to flip."
        ),
    ),
    ConflictBucket.LOOSE_OVERWRITES_ARCHIVE: BucketRationale(
        short=(
            "Loose files ALWAYS win over archived assets, regardless of plugin "
            "or archive load order. The loose copy wins."
        ),
    ),
    ConflictBucket.ARCHIVE_OVERWRITTEN_BY_LOOSE: BucketRationale(
        short=(
            "This entry is inside a BSA/BA2 archive, but another mod ships the "
            "same path as a loose file. Loose ALWAYS wins; the archived entry "
            "loses regardless of plugin load order."
        ),
    ),
    ConflictBucket.ARCHIVE_OVERWRITES_ARCHIVE: BucketRationale(
        short=(
            "Both contributions come from archives. The archive loaded LATER "
            "wins; archive load order is derived from plugin order in plugins.txt "
            "via naming convention (<base>.bsa or <base> - Main.ba2 etc.)."
        ),
    ),
    ConflictBucket.ARCHIVE_OVERWRITTEN_BY_ARCHIVE: BucketRationale(
        short=(
            "This entry is inside an archive that loses to another archive "
            "loaded later. Move the owning plugin later in plugins.txt to flip."
        ),
    ),
}


def rationale_for_bucket(bucket: ConflictBucket) -> BucketRationale:
    return _RATIONALES[bucket]
