"""Audit CP-AnemiC, then train and honestly validate the PaleCheck pallor model.

Pure numpy (no scikit-learn) so every step is visible and the final model is a
handful of numbers the browser app runs offline.

Usage:  python model/train.py
Inputs: data/raw/cp-anemic/{Anemic,Non-anemic}/*.png + Anemia_Data_Collection_Sheet.xlsx
Writes: model/out/{model.json,metrics.json} (+ the web app's copies), demo samples
        for the app, and parity fixtures for the C# tests.

Protocol v2 (v1 is recorded in metrics.json under protocol_history):
  1. Drop every image whose pixels appear under more than one patient record, exactly (v1) or as a
     re-cropped / re-encoded near-identical copy (added in v2 after DupeScope found them).
  2. Hold out 8 demo images (never used anywhere else).
  3. Split the rest 70/30 into development and lockbox, stratified by label.
  4. On development only: choose the feature set (absolute / relative / all) by nested CV.
  5. Evaluate once on the lockbox. Report it whatever it says.
  6. Retrain on development + lockbox for the app.
"""
import csv
import hashlib
import json
import shutil
from collections import defaultdict
from pathlib import Path

import numpy as np
import openpyxl
from PIL import Image

import features as F
from near_duplicates import HASH_BITS_MAX, MAD_MAX, find_near_pairs

ROOT = Path(__file__).resolve().parent.parent
RAW = ROOT / "data" / "raw" / "cp-anemic"
OUT = ROOT / "model" / "out"
CLEAN = ROOT / "data" / "clean"
WEB = ROOT / "src" / "PaleCheck.Web" / "wwwroot"
PARITY = ROOT / "tests" / "PaleCheck.Core.Tests" / "parity"

SEED = 20261001
LAMBDAS = [0.01, 0.1, 1.0, 10.0, 100.0]
OUTER_FOLDS, INNER_FOLDS, REPEATS = 5, 5, 5
LOCKBOX_FRACTION = 0.30
N_DEMO_PER_CLASS = 4
TARGET_SENSITIVITY = 0.90
TARGET_SPECIFICITY = 0.80
FEATURE_SETS = {"absolute": F.ABSOLUTE, "relative": F.RELATIVE, "all": F.ALL_FEATURES}
ILLUMINANTS_K = [2700, 3500, 4000, 5000]   # compared against daylight (6500 K, the sRGB white)


# ---------------------------------------------------------------- data

def load_raw():
    wb = openpyxl.load_workbook(RAW / "Anemia_Data_Collection_Sheet.xlsx", read_only=True)
    rows = list(wb.worksheets[0].iter_rows(values_only=True))
    head = rows[0]
    meta = {str(r[0]).strip(): dict(zip(head, r)) for r in rows[1:] if r[0]}
    items = []
    for folder, label in (("Anemic", 1), ("Non-anemic", 0)):
        for p in sorted((RAW / folder).glob("*.png")):
            m = meta.get(p.stem)
            if m is None:
                continue
            rgba = np.asarray(Image.open(p).convert("RGBA"))
            items.append({
                "id": p.stem, "path": p, "label": label,
                "sheet_label": 1 if str(m["REMARK"]).strip().lower() == "anemic" else 0,
                "hb": float(m["HB_LEVEL"]), "age_months": float(m["Age(Months)"]),
                "gender": str(m["GENDER"]).strip(), "hospital": str(m["HOSPITAL"]).strip(),
                "rgba": rgba, "hash": hashlib.md5(rgba.tobytes()).hexdigest(),
            })
    items.sort(key=lambda it: it["id"])
    return items


def audit(items, rng):
    """Find photos reused across patient records and measure how much they could inflate scores."""
    groups = defaultdict(list)
    for it in items:
        groups[it["hash"]].append(it)
    dup_groups = [g for g in groups.values() if len(g) > 1]
    in_dups = {it["id"] for g in dup_groups for it in g}
    differing = sum(1 for g in dup_groups
                    if len({(it["hb"], it["age_months"], it["gender"], it["hospital"]) for it in g}) > 1)
    conflicting = sum(1 for g in dup_groups if len({it["label"] for it in g}) > 1)
    biggest = max(dup_groups, key=len)
    hb_spread = max(max(it["hb"] for it in g) - min(it["hb"] for it in g) for g in dup_groups)

    # A "model" that only memorises photos: under the usual random 5-fold CV, any test photo
    # with an identical twin in the training folds is answered by copying the twin's label;
    # everything else is a coin flip. This is the free accuracy duplicates hand out.
    y = np.array([it["label"] for it in items])
    h = np.array([it["hash"] for it in items])
    leaked_frac, mem_acc = [], []
    for _ in range(20):
        folds = stratified_folds(y, 5, rng)
        correct, leaked = 0.0, 0
        for f in folds:
            train_hash = defaultdict(list)
            for i in np.setdiff1d(np.arange(len(y)), f):
                train_hash[h[i]].append(y[i])
            for i in f:
                if h[i] in train_hash:
                    leaked += 1
                    votes = train_hash[h[i]]
                    correct += float(round(np.mean(votes)) == y[i])
                else:
                    correct += 0.5
        leaked_frac.append(leaked / len(y))
        mem_acc.append(correct / len(y))

    # Second pass (found with DupeScope): re-cropped / re-encoded copies that exact hashing misses.
    near_pairs = find_near_pairs(items)
    parent = {it["id"]: it["id"] for it in items}

    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x

    for g in dup_groups:
        for it in g[1:]:
            parent[find(it["id"])] = find(g[0]["id"])
    for a, b, _ in near_pairs:
        parent[find(a)] = find(b)
    clusters = defaultdict(list)
    for it in items:
        clusters[find(it["id"])].append(it)
    multi = [c for c in clusters.values() if len(c) > 1]
    unreliable = {it["id"] for c in multi for it in c}
    near_ids = {a for a, _, _ in near_pairs} | {b for _, b, _ in near_pairs}
    near_clusters = [c for c in multi if any(it["id"] in near_ids for it in c)]
    near_differing = sum(1 for c in near_clusters
                         if len({(it["hb"], it["age_months"], it["gender"], it["hospital"]) for it in c}) > 1)

    return unreliable, multi, {
        "images_in_sheet_and_folders": len(items),
        "label_folder_vs_sheet_mismatches": sum(1 for it in items if it["label"] != it["sheet_label"]),
        "duplicate_groups": len(dup_groups),
        "images_in_duplicate_groups": len(in_dups),
        "share_of_dataset_in_duplicate_groups": round(len(in_dups) / len(items), 4),
        "largest_group_size": len(biggest),
        "groups_whose_copies_list_different_patients": differing,
        "groups_with_conflicting_labels": conflicting,
        "max_hb_spread_within_one_photo_g_dl": round(hb_spread, 1),
        "largest_group_example": [{"id": it["id"], "hb": it["hb"], "age_months": it["age_months"],
                                   "gender": it["gender"], "hospital": it["hospital"]} for it in biggest],
        "random_5fold_test_images_with_identical_twin_in_train": round(float(np.mean(leaked_frac)), 4),
        "memoriser_accuracy_under_random_5fold": round(float(np.mean(mem_acc)), 4),
        "published_benchmark_for_context": "CP-AnemiC paper (Appiahene et al. 2023), Table 3: ResNet50 AUC 0.835, "
                                           "accuracy 84.79%, 5-fold CV with augmentation; no deduplication described.",
        "near_identical_pairs": len(near_pairs),
        "near_identical_rule": f"64-bit dHash within {HASH_BITS_MAX} bits, aspect ratio within 5%, mean absolute "
                               f"pixel difference <= {MAD_MAX} on a 128x64 white-flattened grid",
        "images_added_by_near_identical": len(unreliable - in_dups),
        "clusters_with_near_identical_copies": len(near_clusters),
        "near_clusters_listing_different_patients": near_differing,
        "unreliable_images": len(unreliable),
        "share_unreliable": round(len(unreliable) / len(items), 4),
        "reliable_images_kept": len(items) - len(unreliable),
    }


# ---------------------------------------------------------------- maths

def standardize_fit(X):
    mu = X.mean(axis=0)
    sd = X.std(axis=0)
    sd[sd == 0] = 1.0
    return mu, sd


def logreg_fit(X, y, lam):
    """L2-regularised logistic regression by Newton's method. X already standardised."""
    n, d = X.shape
    Xb = np.hstack([X, np.ones((n, 1))])
    w = np.zeros(d + 1)
    reg = np.full(d + 1, lam)
    reg[-1] = 0.0  # never shrink the bias
    for _ in range(50):
        p = 1.0 / (1.0 + np.exp(-Xb @ w))
        grad = Xb.T @ (p - y) + reg * w
        H = (Xb * (p * (1 - p))[:, None]).T @ Xb + np.diag(reg) + 1e-9 * np.eye(d + 1)
        step = np.linalg.solve(H, grad)
        w -= step
        if np.abs(step).max() < 1e-10:
            break
    return w[:-1], w[-1]


def logreg_predict(X, w, b):
    return 1.0 / (1.0 + np.exp(-(X @ w + b)))


def ridge_fit(X, y, lam):
    n, d = X.shape
    Xb = np.hstack([X, np.ones((n, 1))])
    reg = np.full(d + 1, lam)
    reg[-1] = 0.0
    w = np.linalg.solve(Xb.T @ Xb + np.diag(reg), Xb.T @ y)
    return w[:-1], w[-1]


def auc(y, s):
    """Probability that a random anaemic eye scores above a random healthy one (ties count half)."""
    y, s = np.asarray(y), np.asarray(s)
    pos, neg = s[y == 1], s[y == 0]
    if len(pos) == 0 or len(neg) == 0:
        return float("nan")
    allv = np.concatenate([pos, neg])
    order = np.argsort(allv, kind="mergesort")
    sv = allv[order]
    ranks = np.empty(len(allv))
    i = 0
    while i < len(sv):
        j = i
        while j + 1 < len(sv) and sv[j + 1] == sv[i]:
            j += 1
        ranks[order[i:j + 1]] = (i + j) / 2.0 + 1
        i = j + 1
    return float((ranks[:len(pos)].sum() - len(pos) * (len(pos) + 1) / 2) / (len(pos) * len(neg)))


def stratified_folds(y, k, rng):
    idx = np.arange(len(y))
    folds = [[] for _ in range(k)]
    for cls in (0, 1):
        c = idx[y == cls].copy()
        rng.shuffle(c)
        for i, v in enumerate(c):
            folds[i % k].append(v)
    return [np.array(sorted(f)) for f in folds]


def choose_lambda(X, y, rng):
    folds = stratified_folds(y, INNER_FOLDS, rng)
    best, best_auc = LAMBDAS[0], -1.0
    for lam in LAMBDAS:
        scores = np.zeros(len(y))
        for f in folds:
            tr = np.setdiff1d(np.arange(len(y)), f)
            mu, sd = standardize_fit(X[tr])
            w, b = logreg_fit((X[tr] - mu) / sd, y[tr], lam)
            scores[f] = logreg_predict((X[f] - mu) / sd, w, b)
        a = auc(y, scores)
        if a > best_auc + 1e-9:
            best, best_auc = lam, a
    return best


def fit_full(X, y, rng):
    lam = choose_lambda(X, y, rng)
    mu, sd = standardize_fit(X)
    w, b = logreg_fit((X - mu) / sd, y, lam)
    return {"lam": lam, "mu": mu, "sd": sd, "w": w, "b": b}


def predict(model, X):
    return logreg_predict((X - model["mu"]) / model["sd"], model["w"], model["b"])


def nested_cv(X, y, rng, repeats=REPEATS):
    oof_sum = np.zeros(len(y))
    aucs = []
    for _ in range(repeats):
        oof = np.zeros(len(y))
        for f in stratified_folds(y, OUTER_FOLDS, rng):
            tr = np.setdiff1d(np.arange(len(y)), f)
            oof[f] = predict(fit_full(X[tr], y[tr], rng), X[f])
        aucs.append(auc(y, oof))
        oof_sum += oof
    return oof_sum / repeats, aucs


def confusion(y, p, t):
    pred = (p >= t).astype(int)
    tp = int(((pred == 1) & (y == 1)).sum())
    fn = int(((pred == 0) & (y == 1)).sum())
    tn = int(((pred == 0) & (y == 0)).sum())
    fp = int(((pred == 1) & (y == 0)).sum())
    return {"threshold": round(float(t), 4), "tp": tp, "fn": fn, "tn": tn, "fp": fp,
            "sensitivity": round(tp / max(tp + fn, 1), 4), "specificity": round(tn / max(tn + fp, 1), 4),
            "ppv": round(tp / max(tp + fp, 1), 4), "npv": round(tn / max(tn + fn, 1), 4),
            "accuracy": round((tp + tn) / len(y), 4)}


def roc_points(y, p, n=60):
    pts = set()
    for t in np.concatenate([[0.0, 1.0001], np.quantile(p, np.linspace(0, 1, n))]):
        c = confusion(y, p, t)
        pts.add((round(1 - c["specificity"], 4), round(c["sensitivity"], 4)))
    return sorted(pts)


def bootstrap_auc_ci(y, p, rng, n=2000):
    vals = [auc(y[i], p[i]) for i in (rng.integers(0, len(y), len(y)) for _ in range(n))]
    vals = [v for v in vals if not np.isnan(v)]
    return [round(float(np.percentile(vals, 2.5)), 4), round(float(np.percentile(vals, 97.5)), 4)]


def histogram(p, bins=20):
    counts, _ = np.histogram(p, bins=bins, range=(0.0, 1.0))
    return counts.tolist()


# ---------------------------------------------------------------- lighting simulation

def planckian_linear_rgb(kelvin):
    """Colour of a blackbody light in linear sRGB, max channel = 1 (Kang et al. 2002 locus fit)."""
    T = float(kelvin)
    if T <= 4000:
        x = -0.2661239e9 / T**3 - 0.2343589e6 / T**2 + 0.8776956e3 / T + 0.179910
    else:
        x = -3.0258469e9 / T**3 + 2.1070379e6 / T**2 + 0.2226347e3 / T + 0.240390
    if T <= 2222:
        y = -1.1063814 * x**3 - 1.34811020 * x**2 + 2.18555832 * x - 0.20219683
    elif T <= 4000:
        y = -0.9549476 * x**3 - 1.37418593 * x**2 + 2.09137015 * x - 0.16748867
    else:
        y = 3.0817580 * x**3 - 5.87338670 * x**2 + 3.75112997 * x - 0.37001483
    X, Y, Z = x / y, 1.0, (1 - x - y) / y
    rgb = np.array([3.2404542 * X - 1.5371385 * Y - 0.4985314 * Z,
                    -0.9692660 * X + 1.8760108 * Y + 0.0415560 * Z,
                    0.0556434 * X - 0.2040259 * Y + 1.0572252 * Z])
    rgb = np.clip(rgb, 1e-6, None)
    return rgb / rgb.max()


def relight(rgb255, light_lin):
    """Re-render pixels as if lit by another light (von Kries, camera auto white balance off)."""
    lin = F._srgb_to_linear(rgb255 / 255.0) * light_lin
    return F._linear_to_srgb(lin) * 255.0


def lighting_simulation(items, model, names):
    idx = [F.ALL_FEATURES.index(n) for n in names]
    pix = [F.valid_pixels(it["rgba"]) for it in items]
    y = np.array([it["label"] for it in items])
    base = np.array([F.extract(p) for p in pix])
    a0 = base[:, 0]
    disease_gap = float(a0[y == 0].mean() - a0[y == 1].mean())
    p0 = predict(model, base[:, idx])
    out = {"disease_gap_a_star": round(disease_gap, 3), "illuminants": []}
    for k in ILLUMINANTS_K:
        light = planckian_linear_rgb(k)
        card_white = F._linear_to_srgb(light) * 255.0       # how a white card photographs under it
        lit = [relight(p, light) for p in pix]
        lit_f = np.array([F.extract(p) for p in lit])
        fixed_f = np.array([F.extract(F.white_balance(p, card_white)) for p in lit])
        rel_idx = [F.ALL_FEATURES.index(n) for n in F.RELATIVE]
        abs_idx = [F.ALL_FEATURES.index(n) for n in F.ABSOLUTE]
        sd = base.std(axis=0)
        p_lit = predict(model, lit_f[:, idx])
        p_fix = predict(model, fixed_f[:, idx])
        out["illuminants"].append({
            "kelvin": k,
            "card_white_rgb": [round(float(v)) for v in card_white],
            "a_star_shift_uncorrected": round(float((lit_f[:, 0] - a0).mean()), 3),
            "a_star_shift_with_card": round(float((fixed_f[:, 0] - a0).mean()), 3),
            "shift_vs_disease_gap_uncorrected": round(float(abs((lit_f[:, 0] - a0).mean()) / disease_gap), 2),
            "absolute_features_mean_shift_in_sd": round(float(np.mean(np.abs((lit_f - base).mean(axis=0) / sd)[abs_idx])), 3),
            "relative_features_mean_shift_in_sd": round(float(np.mean(np.abs((lit_f - base).mean(axis=0) / sd)[rel_idx])), 3),
            "model_prob_mean_abs_change_uncorrected": round(float(np.abs(p_lit - p0).mean()), 4),
            "model_prob_mean_abs_change_with_card": round(float(np.abs(p_fix - p0).mean()), 4),
        })
    return out


# ---------------------------------------------------------------- manifests

def write_manifests(raw, clusters, demo, rest, lock_mask):
    """Cleaned-dataset manifests for other researchers (also served by the app under data/)."""
    fields = ["image_id", "label", "hb_g_dl", "age_months", "gender", "hospital"]

    def row(it):
        return [it["id"], "anaemic" if it["label"] else "non-anaemic", it["hb"], int(it["age_months"]),
                it["gender"], it["hospital"]]

    split = {d["id"]: "demo" for d in demo}
    split.update({it["id"]: ("lockbox" if lock else "development") for it, lock in zip(rest, lock_mask)})
    reliable = [it for it in raw if it["id"] in split]
    ordered = sorted(clusters, key=lambda c: (-len(c), min(it["id"] for it in c)))

    for d in (CLEAN, WEB / "data"):
        d.mkdir(parents=True, exist_ok=True)
        stale = d / "cp-anemic-duplicate-groups.csv"
        if stale.exists():
            stale.unlink()
        with open(d / "cp-anemic-reliable.csv", "w", newline="") as f:
            w = csv.writer(f)
            w.writerow(fields + ["palecheck_split"])
            for it in reliable:
                w.writerow(row(it) + [split[it["id"]]])
        with open(d / "cp-anemic-unreliable-groups.csv", "w", newline="") as f:
            w = csv.writer(f)
            w.writerow(["group", "images_in_group", "identical_copies_of_this_image"] + fields + ["pixel_md5"])
            for gi, c in enumerate(ordered, 1):
                copies = defaultdict(int)
                for it in c:
                    copies[it["hash"]] += 1
                for it in sorted(c, key=lambda it: it["id"]):
                    w.writerow([gi, len(c), copies[it["hash"]]] + row(it) + [it["hash"]])


# ---------------------------------------------------------------- main

def main():
    rng = np.random.default_rng(SEED)
    raw = load_raw()
    unreliable, clusters, audit_report = audit(raw, rng)
    print("AUDIT:", {k: v for k, v in audit_report.items() if k != "largest_group_example"})

    reliable = [it for it in raw if it["id"] not in unreliable and it["label"] == it["sheet_label"]]

    demo = []
    for cls in (1, 0):
        pool = [it for it in reliable if it["label"] == cls]
        demo += [pool[i] for i in sorted(rng.choice(len(pool), N_DEMO_PER_CLASS, replace=False))]
    demo_ids = {d["id"] for d in demo}
    rest = [it for it in reliable if it["id"] not in demo_ids]

    X_all = np.array([F.extract(F.valid_pixels(it["rgba"])) for it in rest])
    y_all = np.array([it["label"] for it in rest])
    lock_mask = np.zeros(len(rest), dtype=bool)
    for cls in (0, 1):
        idx = np.where(y_all == cls)[0]
        lock_mask[rng.choice(idx, int(round(len(idx) * LOCKBOX_FRACTION)), replace=False)] = True
    dev, lock = ~lock_mask, lock_mask
    print(f"reliable {len(reliable)} = demo {len(demo)} + dev {dev.sum()} + lockbox {lock.sum()}")

    # Feature-set choice on development data only.
    selection = {}
    for name, cols in FEATURE_SETS.items():
        idx = [F.ALL_FEATURES.index(c) for c in cols]
        _, aucs = nested_cv(X_all[dev][:, idx], y_all[dev], rng, repeats=3)
        selection[name] = round(float(np.mean(aucs)), 4)
    chosen = max(selection, key=selection.get)
    names = FEATURE_SETS[chosen]
    cols = [F.ALL_FEATURES.index(c) for c in names]
    X = X_all[:, cols]
    print("feature-set nested CV on dev:", selection, "-> chosen:", chosen)

    # Development nested CV for thresholds, then the one-shot lockbox test.
    dev_oof, dev_aucs = nested_cv(X[dev], y_all[dev], rng)
    ts = np.unique(dev_oof)
    t_low = max(t for t in ts if confusion(y_all[dev], dev_oof, t)["sensitivity"] >= TARGET_SENSITIVITY)
    t_high = max(t_low, min(t for t in ts if confusion(y_all[dev], dev_oof, t)["specificity"] >= TARGET_SPECIFICITY))
    dev_model = fit_full(X[dev], y_all[dev], rng)
    p_lock = predict(dev_model, X[lock])
    y_lock = y_all[lock]
    lock_auc = auc(y_lock, p_lock)
    lock_ci = bootstrap_auc_ci(y_lock, p_lock, rng)
    print(f"dev nested CV AUC {np.mean(dev_aucs):.3f}; LOCKBOX AUC {lock_auc:.3f} CI {lock_ci}")

    # Whole reliable set (excluding demo): nested CV for the app's thresholds and charts.
    oof, cv_aucs = nested_cv(X, y_all, rng)
    pooled = auc(y_all, oof)
    ts = np.unique(oof)
    a_low = max(t for t in ts if confusion(y_all, oof, t)["sensitivity"] >= TARGET_SENSITIVITY)
    a_high = max(a_low, min(t for t in ts if confusion(y_all, oof, t)["specificity"] >= TARGET_SPECIFICITY))

    single = {n: round(max(auc(y_all, X_all[:, j]), auc(y_all, -X_all[:, j])), 4)
              for j, n in enumerate(F.ALL_FEATURES)}
    size_X = np.array([[it["rgba"].shape[1], it["rgba"].shape[0], len(F.valid_pixels(it["rgba"]))]
                       for it in rest], dtype=float)
    size_oof, _ = nested_cv(size_X, y_all, rng, repeats=2)

    hosp = np.array([it["hospital"] for it in rest])
    loho, loho_scores = [], np.zeros(len(y_all))
    for h in sorted(set(hosp)):
        te = hosp == h
        loho_scores[te] = predict(fit_full(X[~te], y_all[~te], rng), X[te])
        a = auc(y_all[te], loho_scores[te])
        loho.append({"hospital": h, "n": int(te.sum()), "anaemic": int(y_all[te].sum()),
                     "auc": None if np.isnan(a) else round(a, 4)})

    gender = np.array([it["gender"] for it in rest])
    age = np.array([it["age_months"] for it in rest])
    hb = np.array([it["hb"] for it in rest])
    subgroups = [{"group": g, "n": int(m.sum()), "auc": round(auc(y_all[m], oof[m]), 4)}
                 for g, m in [("Girls", gender == "Female"), ("Boys", gender == "Male"),
                              ("Under 24 months", age < 24), ("24 months and older", age >= 24),
                              ("Severe only (Hb < 7) vs healthy", (hb < 7) | (y_all == 0))]]

    hb_pred = np.zeros(len(y_all))
    for f in stratified_folds(y_all, OUTER_FOLDS, rng):
        tr = np.setdiff1d(np.arange(len(y_all)), f)
        mu, sd = standardize_fit(X[tr])
        w, b = ridge_fit((X[tr] - mu) / sd, hb[tr], 10.0)
        hb_pred[f] = ((X[f] - mu) / sd) @ w + b

    final = fit_full(X, y_all, rng)
    lighting = lighting_simulation(rest, final, names)
    print("lighting:", json.dumps(lighting["illuminants"][0]))

    demo_X = np.array([F.extract(F.valid_pixels(d["rgba"]))[cols] for d in demo])
    demo_p = predict(final, demo_X)

    model = {
        "version": "1.0.0",
        "trainedOn": "CP-AnemiC reliable subset (Ghana, children 6-59 months), CC BY 4.0",
        "featureSet": chosen,
        "features": names,
        "mean": [round(float(v), 6) for v in final["mu"]],
        "std": [round(float(v), 6) for v in final["sd"]],
        "weights": [round(float(v), 6) for v in final["w"]],
        "bias": round(float(final["b"]), 6),
        "lambda": final["lam"],
        "thresholds": {"low": round(float(a_low), 4), "high": round(float(a_high), 4)},
        "pixelRules": {"alphaMin": F.ALPHA_MIN, "glareMin": F.GLARE_MIN, "darkMax": F.DARK_MAX},
        "reference": {"healthyHistogram": histogram(oof[y_all == 0]),
                      "anaemicHistogram": histogram(oof[y_all == 1])},
    }
    metrics = {
        "audit": audit_report,
        "protocol_history": [
            # v1 ran on 2026-10-01 with exact-duplicate removal only; its lockbox was opened once.
            {"version": 1, "rule": "exact duplicates removed", "reliable_images": 398, "lockbox_n": 117,
             "lockbox_auc": 0.6818, "lockbox_auc_ci95": [0.582, 0.7756], "nested_cv_auc": 0.6419},
            {"version": 2, "rule": "exact and near-identical copies removed",
             "reliable_images": len(reliable), "lockbox_n": int(lock.sum()),
             "lockbox_auc": round(lock_auc, 4), "lockbox_auc_ci95": lock_ci,
             "nested_cv_auc": round(float(np.mean(cv_aucs)), 4)},
        ],
        "dataset": {
            "name": "CP-AnemiC", "source": "https://data.mendeley.com/datasets/m53vz6b7fx/1",
            "license": "CC BY 4.0", "doi": "10.17632/m53vz6b7fx.1",
            "reliable_images": len(reliable), "demo_holdout": len(demo),
            "development": int(dev.sum()), "lockbox": int(lock.sum()),
            "anaemic": int(y_all.sum()), "healthy": int((1 - y_all).sum()),
            "hospitals": len(set(hosp)), "age_months": [int(age.min()), int(age.max())],
            "anaemia_definition": "Hb < 11 g/dL (WHO, children 6-59 months)",
        },
        "feature_set_selection_dev_nested_cv_auc": selection,
        "chosen_feature_set": chosen,
        "development_nested_cv_auc": round(float(np.mean(dev_aucs)), 4),
        "lockbox": {"n": int(lock.sum()), "auc": round(lock_auc, 4), "auc_ci95": lock_ci,
                    "at_low_threshold": confusion(y_lock, p_lock, t_low),
                    "at_high_threshold": confusion(y_lock, p_lock, t_high),
                    "note": "Evaluated exactly once, with thresholds fixed on development data."},
        "nested_cv_all": {"auc_mean": round(float(np.mean(cv_aucs)), 4), "auc_sd": round(float(np.std(cv_aucs)), 4),
                          "auc_pooled": round(pooled, 4), "auc_pooled_ci95": bootstrap_auc_ci(y_all, oof, rng)},
        "operating_points": {"low": confusion(y_all, oof, a_low), "high": confusion(y_all, oof, a_high)},
        "roc": roc_points(y_all, oof),
        "single_feature_auc": single,
        "confound_check": {"image_size_only_auc": round(auc(y_all, size_oof), 4)},
        "leave_one_hospital_out": {"pooled_auc": round(auc(y_all, loho_scores), 4), "per_hospital": loho},
        "subgroups": subgroups,
        "hb_regression": {"mae": round(float(np.abs(hb_pred - hb).mean()), 3),
                          "baseline_mae": round(float(np.abs(hb.mean() - hb).mean()), 3),
                          "pearson_r": round(float(np.corrcoef(hb_pred, hb)[0, 1]), 4)},
        "feature_weights": [{"feature": n, "weight": round(float(w), 4)} for n, w in zip(names, final["w"])],
        "lighting_simulation": lighting,
        "demo_holdout": [{"id": d["id"], "label": int(d["label"]), "hb": d["hb"], "p": round(float(p), 4)}
                         for d, p in zip(demo, demo_p)],
    }

    OUT.mkdir(parents=True, exist_ok=True)
    (WEB / "model").mkdir(parents=True, exist_ok=True)
    for d in (OUT, WEB / "model"):
        (d / "model.json").write_text(json.dumps(model, indent=2))
        (d / "metrics.json").write_text(json.dumps(metrics, indent=2))

    write_manifests(raw, clusters, demo, rest, lock_mask)

    # The most-reused photo, shown on the Science page next to its conflicting records.
    sci = WEB / "science"
    sci.mkdir(parents=True, exist_ok=True)
    first = audit_report["largest_group_example"][0]["id"]
    shutil.copy(next(it["path"] for it in raw if it["id"] == first), sci / "duplicate-example.png")

    sdir = WEB / "samples"
    if sdir.exists():
        shutil.rmtree(sdir)
    sdir.mkdir(parents=True)
    samples = []
    for i, d in enumerate(demo):
        name = f"sample-{i + 1}.png"
        shutil.copy(d["path"], sdir / name)
        samples.append({"file": f"samples/{name}", "sourceId": d["id"], "hb": d["hb"], "anaemic": bool(d["label"])})
    (sdir / "samples.json").write_text(json.dumps(samples, indent=2))

    # Parity fixtures for the C# tests: raw RGBA bytes, Python's features and probability,
    # plus one white-balanced case.
    if PARITY.exists():
        shutil.rmtree(PARITY)
    PARITY.mkdir(parents=True)
    fixtures = []
    for i, d in enumerate(demo[:4] + rest[:2]):
        rgba = d["rgba"]
        (PARITY / f"img{i}.rgba").write_bytes(rgba.tobytes())
        px = F.valid_pixels(rgba)
        fx = F.extract(px)
        warm = [255, 200, 140]
        fx_wb = F.extract(F.white_balance(px, warm))
        fixtures.append({"file": f"img{i}.rgba", "width": int(rgba.shape[1]), "height": int(rgba.shape[0]),
                         "validPixels": int(len(px)), "features": [float(v) for v in fx],
                         "probability": float(predict(final, fx[cols][None, :])[0]),
                         "whiteRgb": warm, "featuresWhiteBalanced": [float(v) for v in fx_wb]})
    (PARITY / "fixtures.json").write_text(json.dumps(fixtures, indent=2))
    print("wrote", OUT)


if __name__ == "__main__":
    main()
