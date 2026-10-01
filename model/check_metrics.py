"""CI guard: the freshly re-derived metrics must match the committed ones.

Usage: python model/check_metrics.py committed.json regenerated.json
Counts must match exactly; AUCs and effect sizes within 0.005 (BLAS rounding differs across machines).
"""
import json
import sys


def get(m, path):
    for key in path.split("."):
        m = m[int(key)] if isinstance(m, list) else m[key]
    return m


EXACT = [
    "audit.duplicate_groups", "audit.images_in_duplicate_groups", "audit.near_identical_pairs",
    "audit.recrop_pairs", "audit.mirrored_copy_pairs", "audit.distinct_photographs",
    "audit.unreliable_images", "audit.reliable_images_kept", "dataset.lockbox", "dataset.development",
]
CLOSE = [
    "audit.memoriser_accuracy_under_random_5fold", "lockbox.auc", "nested_cv_all.auc_mean",
    "leakage_demo.0.memoriser", "leakage_demo.3.memoriser", "leakage_demo.3.colour_model",
    "leave_one_hospital_out.pooled_auc", "lighting_simulation.disease_gap_a_star",
    "lighting_simulation.illuminants.0.shift_vs_disease_gap_uncorrected",
]


def main():
    committed, fresh = (json.load(open(p)) for p in sys.argv[1:3])
    failed = False
    for path in EXACT + CLOSE:
        a, b = get(committed, path), get(fresh, path)
        ok = a == b if path in EXACT else abs(a - b) <= 0.005
        failed |= not ok
        print(f"{'ok ' if ok else 'BAD'} {path}: committed {a} / re-derived {b}")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
