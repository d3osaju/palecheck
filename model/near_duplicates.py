"""Second-pass audit: near-identical images (re-cropped, re-encoded or resized copies) in CP-AnemiC.

Candidate pairs come from a coarse 64-bit difference hash; each candidate is then compared pixel by pixel
after flattening onto white and resizing both to a common 128x64 grid. A pair is "near-identical" when
the aspect ratios agree within 5% and the mean absolute difference is at most MAD_MAX levels (0-255).

Usage: python model/near_duplicates.py      (prints pairs; train.py imports find_near_pairs)
"""
from pathlib import Path

import numpy as np
from PIL import Image

MAD_MAX = 6.0
HASH_BITS_MAX = 6


def _flat(rgba):
    im = Image.fromarray(rgba)
    bg = Image.new("RGBA", im.size, (255, 255, 255, 255))
    bg.alpha_composite(im)
    return bg.convert("RGB")


def _dhash(flat):
    s = np.asarray(flat.resize((9, 8), Image.BILINEAR), dtype=int).sum(axis=2)
    return (s[:, :8] > s[:, 1:]).flatten()


def _grid(flat):
    return np.asarray(flat.resize((128, 64), Image.BILINEAR), dtype=float)


def find_near_pairs(items):
    """items: dicts with 'id' and 'rgba'. Returns [(id_a, id_b, mad)] for near-identical, non-identical pairs."""
    feats = []
    for it in items:
        flat = _flat(it["rgba"])
        h, w = it["rgba"].shape[:2]
        feats.append((it["id"], _dhash(flat), _grid(flat), w / h, it["rgba"].tobytes()))
    pairs = []
    for i in range(len(feats)):
        for j in range(i + 1, len(feats)):
            a, b = feats[i], feats[j]
            if a[4] == b[4]:
                continue  # exact copies are handled by the exact audit
            if (a[1] != b[1]).sum() > HASH_BITS_MAX or abs(a[3] / b[3] - 1) > 0.05:
                continue
            mad = float(np.abs(a[2] - b[2]).mean())
            if mad <= MAD_MAX:
                pairs.append((a[0], b[0], round(mad, 2)))
    return pairs


RECROP_RMS_MAX = 4.0      # RMS colour difference (0-255) on the overlap at the best alignment
RECROP_MIN_OVERLAP = 0.5  # the overlap must cover half of the smaller cut-out
RECROP_NEIGHBOURS = 8     # candidates: each image's nearest neighbours in colour-feature space


def _masked(rgba, step=2):
    a = rgba[::step, ::step].astype(np.float64)
    m = (a[..., 3] >= 128).astype(np.float64)
    return a[..., :3] * m[..., None], m


def _corr(a, b, shape):
    """Cross-correlation of a with b for every shift, via FFT (zero-padded to `shape`)."""
    return np.fft.irfft2(np.fft.rfft2(a, shape) * np.conj(np.fft.rfft2(b, shape)), shape)


def recrop_rms(rgba_a, rgba_b):
    """Smallest RMS colour difference between two cut-outs over all translations where their masks overlap
    enough. Two different crops of the same photograph give ~0; different photographs give much more."""
    A, ma = _masked(rgba_a)
    B, mb = _masked(rgba_b)
    shape = (A.shape[0] + B.shape[0], A.shape[1] + B.shape[1])
    overlap = _corr(ma, mb, shape)
    ssd = _corr((A ** 2).sum(-1), mb, shape) + _corr(ma, (B ** 2).sum(-1), shape)
    for ch in range(3):
        ssd -= 2 * _corr(A[..., ch], B[..., ch], shape)
    need = RECROP_MIN_OVERLAP * min(ma.sum(), mb.sum())
    valid = overlap >= need
    if not valid.any():
        return float("inf")
    msd = np.where(valid, ssd / np.maximum(overlap, 1) / 3.0, np.inf)
    return float(np.sqrt(max(msd.min(), 0.0)))


def find_recrop_pairs(items, features):
    """items: dicts with 'id' and 'rgba'; features: (n, d) colour features used to pick candidates.
    Returns [(id_a, id_b, rms, mirrored)] for pairs that are different crops of the same photograph,
    including left-right mirrored copies.

    Validation (see README): comparing the same candidate pairs with one image mirrored gives a null
    distribution; at RMS <= 4 it only matches true mirror-image copies (RMS 0), next-lowest 4.19."""
    # Search among distinct photographs only: otherwise an image's neighbour slots fill up with its own
    # pixel-identical copies (distance 0) and its re-crops are never compared.
    reps, seen = [], set()
    for i, it in enumerate(items):
        key = it["rgba"].tobytes()
        if key not in seen:
            seen.add(key)
            reps.append(i)
    F = features[reps]
    Z = (F - F.mean(0)) / F.std(0)
    D = np.sqrt(((Z[:, None, :] - Z[None, :, :]) ** 2).sum(-1))
    np.fill_diagonal(D, np.inf)
    # Stable sort: NumPy's default sort breaks ties differently on different CPUs (SIMD sorting).
    cands = {tuple(sorted((reps[r], reps[int(s)])))
             for r in range(len(reps)) for s in np.argsort(D[r], kind="stable")[:RECROP_NEIGHBOURS]}
    pairs = []
    for i, j in sorted(cands):
        a, b = items[i]["rgba"], items[j]["rgba"]
        rms = recrop_rms(a, b)
        rms_mirror = recrop_rms(a, b[:, ::-1])
        best = min(rms, rms_mirror)
        if best <= RECROP_RMS_MAX:
            pairs.append((items[i]["id"], items[j]["id"], round(best, 2), rms_mirror < rms))
    return pairs


if __name__ == "__main__":
    root = Path(__file__).resolve().parent.parent / "data" / "raw" / "cp-anemic"
    items = [{"id": p.stem, "rgba": np.asarray(Image.open(p).convert("RGBA"))} for p in sorted(root.glob("*/*.png"))]
    pairs = find_near_pairs(items)
    print(f"{len(pairs)} near-identical pairs")
    for p in pairs:
        print(p)
