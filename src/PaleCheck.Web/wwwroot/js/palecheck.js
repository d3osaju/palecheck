// Canvas work for PaleCheck: load a photo, tap the white card, paint the eyelid,
// and hand the outlined pixels to .NET. All processing stays on the device.

const MAX_SIDE = 1600;          // photos are scaled down to this; colour averages are unaffected
const MAX_EXPORT_PIXELS = 400000;
const editors = new Map();

function editor(id) {
    const e = editors.get(id);
    if (!e) throw new Error(`Editor ${id} not initialised`);
    return e;
}

export function init(id, dotnet) {
    const root = document.getElementById(id);
    const photo = root.querySelector("canvas.photo");
    const overlay = root.querySelector("canvas.overlay");
    const mask = document.createElement("canvas");
    const e = { root, photo, overlay, mask, dotnet, mode: "move", brush: 0.02, zoom: 1, tolerance: 16, painting: false, card: null, hasImage: false };
    editors.set(id, e);

    const toImage = ev => {
        const r = overlay.getBoundingClientRect();
        return { x: (ev.clientX - r.left) * overlay.width / r.width, y: (ev.clientY - r.top) * overlay.height / r.height };
    };
    overlay.addEventListener("pointerdown", ev => {
        if (!e.hasImage) return;
        const p = toImage(ev);
        if (e.mode === "card") {
            pickCard(e, p);
        } else if (e.mode === "wand") {
            const n = wand(e, p);
            e.dotnet.invokeMethodAsync("OnWand", n < 0 ? "too-large" : n === 0 ? "nothing" : "ok");
            if (n >= 0) e.dotnet.invokeMethodAsync("OnMaskChanged", countMask(e));
        } else if (e.mode === "paint" || e.mode === "erase") {
            e.painting = true;
            e.last = p;
            overlay.setPointerCapture(ev.pointerId);
            stroke(e, p, p);
        }
    });
    overlay.addEventListener("pointermove", ev => {
        if (!e.painting) return;
        const p = toImage(ev);
        stroke(e, e.last, p);
        e.last = p;
    });
    const end = () => {
        if (!e.painting) return;
        e.painting = false;
        e.dotnet.invokeMethodAsync("OnMaskChanged", countMask(e));
    };
    overlay.addEventListener("pointerup", end);
    overlay.addEventListener("pointercancel", end);
}

export function dispose(id) {
    editors.delete(id);
}

function setSize(e, w, h) {
    for (const c of [e.photo, e.overlay, e.mask]) {
        c.width = w;
        c.height = h;
    }
    e.card = null;
    e.hasImage = true;
    applyZoom(e);
}

function drawImage(e, img) {
    const scale = Math.min(1, MAX_SIDE / Math.max(img.naturalWidth || img.width, img.naturalHeight || img.height));
    const w = Math.round((img.naturalWidth || img.width) * scale);
    const h = Math.round((img.naturalHeight || img.height) * scale);
    setSize(e, w, h);
    const ctx = e.photo.getContext("2d", { willReadFrequently: true });
    ctx.clearRect(0, 0, w, h);
    ctx.drawImage(img, 0, 0, w, h);
    redraw(e);
    return { width: w, height: h };
}

function loadImage(src) {
    return new Promise((resolve, reject) => {
        const img = new Image();
        img.onload = () => resolve(img);
        img.onerror = () => reject(new Error("Could not read that image."));
        img.src = src;
    });
}

/** Loads the file chosen in an <input type=file>. Returns {width,height} or null. */
export async function loadFromInput(id, input) {
    const file = input.files && input.files[0];
    if (!file) return null;
    const url = URL.createObjectURL(file);
    try {
        const size = drawImage(editor(id), await loadImage(url));
        input.value = "";
        return size;
    } finally {
        URL.revokeObjectURL(url);
    }
}

/**
 * Loads a research-dataset sample. Those images are already cut out (transparent outside the
 * eyelid), so the mask is taken from their alpha channel.
 */
export async function loadSample(id, url) {
    const e = editor(id);
    const img = await loadImage(url);
    // Samples are small: show them on a padded white background, scaled up for visibility.
    const pad = 24, scale = Math.max(1, Math.floor(600 / img.width));
    const w = img.width * scale + 2 * pad, h = img.height * scale + 2 * pad;
    setSize(e, w, h);
    const ctx = e.photo.getContext("2d", { willReadFrequently: true });
    ctx.fillStyle = "#fff";
    ctx.fillRect(0, 0, w, h);
    ctx.imageSmoothingEnabled = false;
    ctx.drawImage(img, pad, pad, img.width * scale, img.height * scale);

    // Mask from alpha, at the same scale.
    const tmp = document.createElement("canvas");
    tmp.width = img.width;
    tmp.height = img.height;
    const tctx = tmp.getContext("2d", { willReadFrequently: true });
    tctx.drawImage(img, 0, 0);
    const src = tctx.getImageData(0, 0, img.width, img.height).data;
    const mctx = e.mask.getContext("2d", { willReadFrequently: true });
    const m = mctx.createImageData(w, h);
    for (let y = 0; y < img.height; y++) {
        for (let x = 0; x < img.width; x++) {
            const a = src[(y * img.width + x) * 4 + 3];
            if (a < 128) continue;
            for (let dy = 0; dy < scale; dy++) {
                for (let dx = 0; dx < scale; dx++) {
                    const o = ((pad + y * scale + dy) * w + pad + x * scale + dx) * 4;
                    m.data[o] = 236; m.data[o + 1] = 72; m.data[o + 2] = 153; m.data[o + 3] = 255;
                }
            }
        }
    }
    mctx.putImageData(m, 0, 0);
    // Keep the sample's own pixels exactly (no smoothing) so results match the research pipeline.
    e.samplePixels = { img, scale, pad };
    redraw(e);
    return countMask(e);
}

export function setMode(id, mode) {
    const e = editor(id);
    e.mode = mode;
    e.overlay.style.touchAction = mode === "move" ? "auto" : "none";
    e.overlay.style.cursor = mode === "move" ? "grab" : "crosshair";
}

export function setBrush(id, fraction) {
    editor(id).brush = fraction;
}

export function setTolerance(id, tolerance) {
    editor(id).tolerance = tolerance;
}

// ---------------------------------------------------------------- colour helpers

function srgbToLinear(c) {
    c /= 255;
    return c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
}

function labF(t) {
    return t > 0.008856 ? Math.cbrt(t) : 7.787 * t + 16 / 116;
}

/** sRGB 0..255 -> CIELAB (D65), same formula as ColorMath.cs / features.py. */
function toLab(r, g, b) {
    const R = srgbToLinear(r), G = srgbToLinear(g), B = srgbToLinear(b);
    const fx = labF((R * 0.4124564 + G * 0.3575761 + B * 0.1804375) / 0.95047);
    const fy = labF(R * 0.2126729 + G * 0.7151522 + B * 0.0721750);
    const fz = labF((R * 0.0193339 + G * 0.1191920 + B * 0.9503041) / 1.08883);
    return [116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz)];
}

// ---------------------------------------------------------------- magic wand

/**
 * Selects the connected region around the tap whose colour is close to the tapped colour.
 * Lightness counts for less than hue, because the curved eyelid is shaded unevenly but keeps
 * its colour. Returns the number of pixels added, or -1 if the region ran away (too big).
 */
function wand(e, p) {
    const w = e.photo.width, h = e.photo.height;
    const sx = Math.round(p.x), sy = Math.round(p.y);
    if (sx < 0 || sy < 0 || sx >= w || sy >= h) return 0;
    const img = e.photo.getContext("2d", { willReadFrequently: true }).getImageData(0, 0, w, h).data;

    let sL = 0, sA = 0, sB = 0, n = 0;
    for (let dy = -2; dy <= 2; dy++) {
        for (let dx = -2; dx <= 2; dx++) {
            const x = sx + dx, y = sy + dy;
            if (x < 0 || y < 0 || x >= w || y >= h) continue;
            const o = (y * w + x) * 4;
            const [L, A, B] = toLab(img[o], img[o + 1], img[o + 2]);
            sL += L; sA += A; sB += B; n++;
        }
    }
    sL /= n; sA /= n; sB /= n;

    const tol2 = e.tolerance * e.tolerance;
    const limit = Math.floor(w * h * 0.3);
    const visited = new Uint8Array(w * h);
    const region = [];
    const stack = [sy * w + sx];
    visited[sy * w + sx] = 1;
    while (stack.length) {
        const i = stack.pop();
        const o = i * 4;
        const [L, A, B] = toLab(img[o], img[o + 1], img[o + 2]);
        const d2 = 0.25 * (L - sL) ** 2 + (A - sA) ** 2 + (B - sB) ** 2;
        if (d2 > tol2) continue;
        region.push(i);
        if (region.length > limit) return -1;
        const x = i % w, y = (i - x) / w;
        if (x > 0 && !visited[i - 1]) { visited[i - 1] = 1; stack.push(i - 1); }
        if (x < w - 1 && !visited[i + 1]) { visited[i + 1] = 1; stack.push(i + 1); }
        if (y > 0 && !visited[i - w]) { visited[i - w] = 1; stack.push(i - w); }
        if (y < h - 1 && !visited[i + w]) { visited[i + w] = 1; stack.push(i + w); }
    }
    if (region.length < 20) return 0;

    const mctx = e.mask.getContext("2d", { willReadFrequently: true });
    const m = mctx.getImageData(0, 0, w, h);
    for (const i of region) {
        const o = i * 4;
        m.data[o] = 236; m.data[o + 1] = 72; m.data[o + 2] = 153; m.data[o + 3] = 255;
    }
    mctx.putImageData(m, 0, 0);
    e.samplePixels = null;
    redraw(e);
    return region.length;
}

// ---------------------------------------------------------------- card detection

/**
 * Finds the PaleCheck card: a bright patch, roughly rectangular, framed by a dark border.
 * Works on a small copy of the photo. Returns {r,g,b,sd} sampled from the middle of the
 * patch at full resolution, or null when no card-like patch is found.
 */
export function autoDetectCard(id) {
    const e = editor(id);
    const W = e.photo.width, H = e.photo.height;
    const s = Math.min(1, 320 / Math.max(W, H));
    const w = Math.max(1, Math.round(W * s)), h = Math.max(1, Math.round(H * s));
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    const ctx = c.getContext("2d", { willReadFrequently: true });
    ctx.drawImage(e.photo, 0, 0, w, h);
    const d = ctx.getImageData(0, 0, w, h).data;

    const N = w * h;
    const Y = new Float32Array(N);
    for (let i = 0; i < N; i++) Y[i] = 0.2126 * d[4 * i] + 0.7152 * d[4 * i + 1] + 0.0722 * d[4 * i + 2];
    const sorted = Float32Array.from(Y).sort();
    const p99 = sorted[Math.floor(0.99 * (N - 1))];
    if (p99 < 60) return null;
    const brightMin = 0.72 * p99, darkMax = 0.35 * p99;

    const label = new Int32Array(N).fill(-1);
    let best = null;
    for (let start = 0; start < N; start++) {
        if (label[start] !== -1 || Y[start] < brightMin) continue;
        let area = 0, minX = w, minY = h, maxX = 0, maxY = 0, touches = false;
        const stack = [start];
        label[start] = start;
        while (stack.length) {
            const i = stack.pop();
            const x = i % w, y = (i - x) / w;
            area++;
            if (x < minX) minX = x; if (x > maxX) maxX = x;
            if (y < minY) minY = y; if (y > maxY) maxY = y;
            if (x === 0 || y === 0 || x === w - 1 || y === h - 1) touches = true;
            for (const j of [x > 0 ? i - 1 : -1, x < w - 1 ? i + 1 : -1, y > 0 ? i - w : -1, y < h - 1 ? i + w : -1]) {
                if (j >= 0 && label[j] === -1 && Y[j] >= brightMin) { label[j] = start; stack.push(j); }
            }
        }
        const bw = maxX - minX + 1, bh = maxY - minY + 1;
        if (touches || area < 0.004 * N || bw < 8 || bh < 8) continue;
        const fill = area / (bw * bh);
        if (fill < 0.6) continue;
        // The printed frame: a band just outside the patch should be mostly dark.
        const m = Math.max(2, Math.round(0.08 * Math.min(bw, bh)));
        let ring = 0, ringDark = 0;
        for (let y = Math.max(0, minY - m); y <= Math.min(h - 1, maxY + m); y++) {
            for (let x = Math.max(0, minX - m); x <= Math.min(w - 1, maxX + m); x++) {
                if (x >= minX && x <= maxX && y >= minY && y <= maxY) continue;
                ring++;
                if (Y[y * w + x] <= darkMax) ringDark++;
            }
        }
        const darkShare = ring ? ringDark / ring : 0;
        if (darkShare < 0.45) continue;
        const score = area * darkShare * fill;
        if (!best || score > best.score) best = { score, minX, minY, maxX, maxY };
    }
    if (!best) return null;

    // Sample the inner half of the patch at full resolution.
    const x0 = Math.round((best.minX + 0.25 * (best.maxX - best.minX)) / s);
    const x1 = Math.round((best.maxX - 0.25 * (best.maxX - best.minX)) / s);
    const y0 = Math.round((best.minY + 0.25 * (best.maxY - best.minY)) / s);
    const y1 = Math.round((best.maxY - 0.25 * (best.maxY - best.minY)) / s);
    const full = e.photo.getContext("2d", { willReadFrequently: true }).getImageData(x0, y0, Math.max(1, x1 - x0), Math.max(1, y1 - y0)).data;
    const step = Math.max(1, Math.floor(Math.sqrt(full.length / 4 / 40000)));
    const R = [], G = [], B = [];
    for (let i = 0; i < full.length; i += 4 * step) { R.push(full[i]); G.push(full[i + 1]); B.push(full[i + 2]); }
    const mean = R.reduce((a, v) => a + v, 0) / R.length;
    const sd = Math.sqrt(R.reduce((a, v) => a + (v - mean) ** 2, 0) / R.length);
    e.card = { x: (x0 + x1) / 2, y: (y0 + y1) / 2, radius: Math.max(6, Math.min(x1 - x0, y1 - y0) / 2) };
    redraw(e);
    return { r: median(R), g: median(G), b: median(B), sd };
}

export function setZoom(id, zoom) {
    const e = editor(id);
    e.zoom = zoom;
    applyZoom(e);
}

function applyZoom(e) {
    const pct = `${Math.round(e.zoom * 100)}%`;
    e.photo.style.width = pct;
    e.overlay.style.width = pct;
}

export function clearMask(id) {
    const e = editor(id);
    e.mask.getContext("2d").clearRect(0, 0, e.mask.width, e.mask.height);
    e.samplePixels = null;
    redraw(e);
    return 0;
}

function stroke(e, a, b) {
    const ctx = e.mask.getContext("2d");
    ctx.globalCompositeOperation = e.mode === "erase" ? "destination-out" : "source-over";
    ctx.strokeStyle = "rgb(236,72,153)";
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    ctx.lineWidth = Math.max(4, e.brush * Math.max(e.mask.width, e.mask.height));
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(b.x + 0.01, b.y);
    ctx.stroke();
    ctx.globalCompositeOperation = "source-over";
    e.samplePixels = null;
    redraw(e);
}

function countMask(e) {
    const d = e.mask.getContext("2d", { willReadFrequently: true }).getImageData(0, 0, e.mask.width, e.mask.height).data;
    let n = 0;
    for (let i = 3; i < d.length; i += 4) if (d[i] >= 128) n++;
    return n;
}

function redraw(e) {
    const ctx = e.overlay.getContext("2d");
    ctx.clearRect(0, 0, e.overlay.width, e.overlay.height);
    ctx.globalAlpha = 0.45;
    ctx.drawImage(e.mask, 0, 0);
    ctx.globalAlpha = 1;
    if (e.card) {
        const r = e.card.radius;
        ctx.lineWidth = Math.max(2, r / 4);
        ctx.strokeStyle = "#111";
        ctx.strokeRect(e.card.x - r, e.card.y - r, 2 * r, 2 * r);
        ctx.strokeStyle = "#fff";
        ctx.setLineDash([r / 2, r / 2]);
        ctx.strokeRect(e.card.x - r, e.card.y - r, 2 * r, 2 * r);
        ctx.setLineDash([]);
    }
}

function median(values) {
    values.sort((a, b) => a - b);
    return values[Math.floor(values.length / 2)];
}

function pickCard(e, p) {
    const r = Math.max(6, Math.round(0.012 * Math.max(e.photo.width, e.photo.height)));
    const x0 = Math.max(0, Math.round(p.x) - r), y0 = Math.max(0, Math.round(p.y) - r);
    const w = Math.min(e.photo.width - x0, 2 * r + 1), h = Math.min(e.photo.height - y0, 2 * r + 1);
    const d = e.photo.getContext("2d", { willReadFrequently: true }).getImageData(x0, y0, w, h).data;
    const R = [], G = [], B = [];
    for (let i = 0; i < d.length; i += 4) { R.push(d[i]); G.push(d[i + 1]); B.push(d[i + 2]); }
    const mean = R.reduce((s, v) => s + v, 0) / R.length;
    const sd = Math.sqrt(R.reduce((s, v) => s + (v - mean) ** 2, 0) / R.length);
    e.card = { x: p.x, y: p.y, radius: r };
    redraw(e);
    e.dotnet.invokeMethodAsync("OnCardPicked", median(R), median(G), median(B), sd);
}

/**
 * RGBA bytes of the outlined region's bounding box; alpha is 255 inside the outline, 0 outside.
 * Large regions are subsampled on a grid (colour statistics are unaffected).
 */
export function getOutlinedPixels(id) {
    const e = editor(id);
    if (e.samplePixels) return sampleOriginalPixels(e.samplePixels);

    const w = e.photo.width, h = e.photo.height;
    const img = e.photo.getContext("2d", { willReadFrequently: true }).getImageData(0, 0, w, h).data;
    const m = e.mask.getContext("2d", { willReadFrequently: true }).getImageData(0, 0, w, h).data;
    let minX = w, minY = h, maxX = -1, maxY = -1;
    for (let y = 0; y < h; y++) {
        for (let x = 0; x < w; x++) {
            if (m[(y * w + x) * 4 + 3] >= 128) {
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }
    }
    if (maxX < 0) return new Uint8Array(0);
    const bw = maxX - minX + 1, bh = maxY - minY + 1;
    const step = Math.max(1, Math.ceil(Math.sqrt((bw * bh) / MAX_EXPORT_PIXELS)));
    const ow = Math.ceil(bw / step), oh = Math.ceil(bh / step);
    const out = new Uint8Array(ow * oh * 4);
    let o = 0;
    for (let y = minY; y <= maxY; y += step) {
        for (let x = minX; x <= maxX; x += step) {
            const i = (y * w + x) * 4;
            out[o] = img[i];
            out[o + 1] = img[i + 1];
            out[o + 2] = img[i + 2];
            out[o + 3] = m[i + 3] >= 128 ? 255 : 0;
            o += 4;
        }
    }
    return out.subarray(0, o);
}

function sampleOriginalPixels({ img }) {
    const c = document.createElement("canvas");
    c.width = img.width;
    c.height = img.height;
    const ctx = c.getContext("2d", { willReadFrequently: true });
    ctx.drawImage(img, 0, 0);
    return new Uint8Array(ctx.getImageData(0, 0, img.width, img.height).data.buffer);
}

// ---------------------------------------------------------------- small helpers

export function storageGet(key) {
    try { return localStorage.getItem(key); } catch { return null; }
}

export function storageSet(key, value) {
    try { localStorage.setItem(key, value); return true; } catch { return false; }
}

export function storageRemove(key) {
    try { localStorage.removeItem(key); } catch { /* storage unavailable */ }
}

export function downloadText(filename, text, mime) {
    const blob = new Blob([text], { type: mime || "text/plain" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 1000);
}

export function print() {
    window.print();
}

export function setLang(lang) {
    document.documentElement.lang = lang;
}

export function clickElement(el) {
    el.click();
}
