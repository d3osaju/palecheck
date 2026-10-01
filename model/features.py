"""Colour features of the palpebral conjunctiva (inner lower eyelid).

The exact same maths is implemented in C# (src/PaleCheck.Core/FeatureExtractor.cs).
tests/PaleCheck.Core.Tests checks both produce identical numbers, so any change
here must be mirrored there.
"""
import numpy as np

# Absolute colour: how red/pale the tissue looks. Sensitive to room lighting.
ABSOLUTE = [
    "a_mean",    # CIELAB a* (green-red axis). Pale tissue -> lower a*.
    "a_p25",     # 25th percentile of a* (the palest quarter of the tissue)
    "L_mean",    # CIELAB lightness
    "b_mean",    # CIELAB b* (blue-yellow axis)
    "ei_mean",   # erythema index 100*log10(R/G): a classic redness measure
    "r_chrom",   # R / (R+G+B): redness independent of brightness
    "g_chrom",   # G / (R+G+B)
    "sat_mean",  # HSV saturation: how vivid the colour is
    "a_std",     # spread of a*: visible blood vessels raise it
]

# Relative colour: contrasts *within* the same photo, like a clinician comparing the
# bright-red rim of the eyelid with the paler body. Room lighting shifts both parts
# together, so much of it cancels out.
RELATIVE = [
    "a_p90_minus_p10",
    "a_p90_minus_p50",
    "ei_p90_minus_p10",
    "ei_p90_minus_p50",
    "a_over_L",
    "a_cv",
    "frac_ei_above_median_plus5",
    "rg_top20_minus_bottom20",
]

ALL_FEATURES = ABSOLUTE + RELATIVE

# Pixels that are glare (near white) or shadow (near black) carry no colour information.
GLARE_MIN = 235   # all channels above this -> specular highlight
DARK_MAX = 25     # all channels below this -> shadow
ALPHA_MIN = 128   # transparent pixels are outside the outlined eyelid


def _srgb_to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def _linear_to_srgb(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


def rgb_to_lab(rgb255):
    """sRGB (N,3) in 0..255 -> CIELAB (N,3), D65 white."""
    c = _srgb_to_linear(rgb255 / 255.0)
    x = (c[:, 0] * 0.4124564 + c[:, 1] * 0.3575761 + c[:, 2] * 0.1804375) / 0.95047
    y = (c[:, 0] * 0.2126729 + c[:, 1] * 0.7151522 + c[:, 2] * 0.0721750) / 1.00000
    z = (c[:, 0] * 0.0193339 + c[:, 1] * 0.1191920 + c[:, 2] * 0.9503041) / 1.08883

    def f(t):
        return np.where(t > 0.008856, np.cbrt(t), 7.787 * t + 16.0 / 116.0)

    fx, fy, fz = f(x), f(y), f(z)
    return np.stack([116.0 * fy - 16.0, 500.0 * (fx - fy), 200.0 * (fy - fz)], axis=1)


def white_balance(rgb255, white255):
    """Von Kries correction: scale linear channels so the card's white patch becomes neutral."""
    lin = _srgb_to_linear(rgb255 / 255.0)
    w = _srgb_to_linear(np.asarray(white255, dtype=float) / 255.0)
    gain = w.max() / np.maximum(w, 1e-6)
    return _linear_to_srgb(lin * gain) * 255.0


def valid_pixels(rgba):
    """(H,W,4) uint8 -> (N,3) float RGB of usable tissue pixels."""
    px = rgba.reshape(-1, 4).astype(np.float64)
    rgb = px[:, :3]
    keep = px[:, 3] >= ALPHA_MIN
    keep &= ~(rgb.min(axis=1) > GLARE_MIN)
    keep &= ~(rgb.max(axis=1) < DARK_MAX)
    return rgb[keep]


def percentile(sorted_vals, q):
    """Linear-interpolated percentile on pre-sorted values (matches numpy 'linear')."""
    n = len(sorted_vals)
    pos = (n - 1) * q
    lo = int(np.floor(pos))
    hi = min(lo + 1, n - 1)
    return float(sorted_vals[lo] + (sorted_vals[hi] - sorted_vals[lo]) * (pos - lo))


def extract(rgb):
    """(N,3) RGB 0..255 -> all features, in ALL_FEATURES order."""
    lab = rgb_to_lab(rgb)
    r, g, b = rgb[:, 0], rgb[:, 1], rgb[:, 2]
    s = r + g + b + 1e-9
    mx = rgb.max(axis=1)
    mn = rgb.min(axis=1)
    sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-9), 0.0)
    ei = 100.0 * np.log10((r + 1.0) / (g + 1.0))
    L, a = lab[:, 0], lab[:, 1]
    sa, se = np.sort(a), np.sort(ei)
    a_p10, a_p25, a_p50, a_p90 = (percentile(sa, q) for q in (0.10, 0.25, 0.50, 0.90))
    e_p10, e_p50, e_p90 = (percentile(se, q) for q in (0.10, 0.50, 0.90))
    rg = np.sort(r / (g + 1.0))
    k = max(len(rg) // 5, 1)
    return np.array([
        a.mean(), a_p25, L.mean(), lab[:, 2].mean(), ei.mean(),
        (r / s).mean(), (g / s).mean(), sat.mean(), a.std(),
        a_p90 - a_p10,
        a_p90 - a_p50,
        e_p90 - e_p10,
        e_p90 - e_p50,
        (a / np.maximum(L, 1.0)).mean(),
        a.std() / max(abs(a.mean()), 1e-6),
        float((ei > e_p50 + 5.0).mean()),
        rg[-k:].mean() - rg[:k].mean(),
    ])
