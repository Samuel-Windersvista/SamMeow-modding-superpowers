from mo2_assets_engine.rationale import (
    BucketRationale,
    rationale_for_bucket,
)
from mo2_assets_engine.types import ConflictBucket


def test_each_bucket_has_a_rationale() -> None:
    for bucket in ConflictBucket:
        rationale = rationale_for_bucket(bucket)
        assert isinstance(rationale, BucketRationale)
        assert rationale.short


def test_no_conflict_has_nonempty_short_rationale() -> None:
    rationale = rationale_for_bucket(ConflictBucket.NO_CONFLICT)
    assert rationale.short  # not empty — explains "no overlap"
