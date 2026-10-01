// DupeScope: finds identical and near-identical images in a dataset folder, entirely in the browser.
// Exact = same pixels (SHA-256 of decoded RGBA + size), so re-saved or renamed copies are caught.
// Near  = 64-bit difference hash within 2 bits AND mean colour within 3 levels.

const IMAGE = /\.(png|jpe?g|bmp|gif|webp)$/i;
let lastUrls = [];

function stem(name) {
    return name.replace(/\.[^.]+$/, "");
}

function canvas(w, h) {
    if (typeof OffscreenCanvas !== "undefined") return new OffscreenCanvas(w, h);
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    return c;
}

async function fingerprint(file) {
    const bmp = await createImageBitmap(file);
    const w = bmp.width, h = bmp.height;
    const c = canvas(w, h);
    const ctx = c.getContext("2d", { willReadFrequently: true });
    ctx.drawImage(bmp, 0, 0);
    const px = ctx.getImageData(0, 0, w, h).data;
    const sized = new Uint8Array(px.length + 8);
    new DataView(sized.buffer).setUint32(0, w);
    new DataView(sized.buffer).setUint32(4, h);
    sized.set(px, 8);
    const digest = new Uint8Array(await crypto.subtle.digest("SHA-256", sized));
    const exact = Array.from(digest, b => b.toString(16).padStart(2, "0")).join("");

    // Near-duplicate features use the image flattened onto white, so a transparent PNG and the
    // same picture saved as JPEG compare equal.
    const flat = (cw, ch) => {
        const cv = canvas(cw, ch);
        const cx = cv.getContext("2d", { willReadFrequently: true });
        cx.fillStyle = "#fff";
        cx.fillRect(0, 0, cw, ch);
        cx.drawImage(bmp, 0, 0, cw, ch);
        return cx.getImageData(0, 0, cw, ch).data;
    };
    const t = flat(32, 32);
    let r = 0, g = 0, b = 0;
    for (let i = 0; i < t.length; i += 4) { r += t[i]; g += t[i + 1]; b += t[i + 2]; }
    const mean = [r / 1024, g / 1024, b / 1024];

    const s = flat(9, 8);
    let hi = 0, lo = 0;
    for (let y = 0; y < 8; y++) {
        for (let x = 0; x < 8; x++) {
            const a = s[(y * 9 + x) * 4] + s[(y * 9 + x) * 4 + 1] + s[(y * 9 + x) * 4 + 2];
            const c2 = s[(y * 9 + x + 1) * 4] + s[(y * 9 + x + 1) * 4 + 1] + s[(y * 9 + x + 1) * 4 + 2];
            const bit = a > c2 ? 1 : 0;
            const k = y * 8 + x;
            if (k < 32) lo |= bit << k; else hi |= bit << (k - 32);
        }
    }
    bmp.close();
    return { exact, hi, lo, mean, w, h };
}

function popcount(v) {
    v = v - ((v >>> 1) & 0x55555555);
    v = (v & 0x33333333) + ((v >>> 2) & 0x33333333);
    return (((v + (v >>> 4)) & 0x0f0f0f0f) * 0x01010101) >>> 24;
}

function parseCsv(text) {
    const rows = [];
    let row = [], field = "", quoted = false;
    for (let i = 0; i < text.length; i++) {
        const ch = text[i];
        if (quoted) {
            if (ch === '"' && text[i + 1] === '"') { field += '"'; i++; }
            else if (ch === '"') quoted = false;
            else field += ch;
        } else if (ch === '"') quoted = true;
        else if (ch === ",") { row.push(field); field = ""; }
        else if (ch === "\n" || ch === "\r") {
            if (ch === "\r" && text[i + 1] === "\n") i++;
            row.push(field); field = "";
            if (row.some(v => v !== "")) rows.push(row);
            row = [];
        } else field += ch;
    }
    if (field !== "" || row.length) { row.push(field); rows.push(row); }
    return rows;
}

async function readMetadata(csvInput, stems) {
    const file = csvInput && csvInput.files && csvInput.files[0];
    if (!file) return null;
    const rows = parseCsv(await file.text());
    if (rows.length < 2) return null;
    const header = rows[0].map(h => h.trim());
    // The id column is whichever column matches the most image file names.
    let best = -1, bestHits = 0;
    for (let c = 0; c < header.length; c++) {
        const hits = rows.slice(1).filter(r => stems.has(stem(String(r[c] ?? "").trim()))).length;
        if (hits > bestHits) { best = c; bestHits = hits; }
    }
    if (best < 0) return { header, idColumn: null, byId: new Map() };
    const byId = new Map();
    for (const r of rows.slice(1)) byId.set(stem(String(r[best] ?? "").trim()), r.map(v => String(v ?? "").trim()));
    return { header, idColumn: header[best], idIndex: best, byId };
}

/** Scans the chosen files. Progress goes to dotnet.OnProgress(done, total). */
export async function scan(imageInput, csvInput, dotnet) {
    for (const u of lastUrls) URL.revokeObjectURL(u);
    lastUrls = [];

    const files = [...(imageInput.files || [])].filter(f => IMAGE.test(f.name));
    const items = [];
    const failed = [];
    for (let i = 0; i < files.length; i++) {
        try {
            items.push({ file: files[i], name: files[i].webkitRelativePath || files[i].name, ...(await fingerprint(files[i])) });
        } catch {
            failed.push(files[i].name);
        }
        if (i % 10 === 9 || i === files.length - 1) await dotnet.invokeMethodAsync("OnProgress", i + 1, files.length);
    }

    // Exact groups.
    const byHash = new Map();
    items.forEach((it, i) => {
        if (!byHash.has(it.exact)) byHash.set(it.exact, []);
        byHash.get(it.exact).push(i);
    });
    const exactGroups = [...byHash.values()].filter(g => g.length > 1);

    // Near groups between images that are not exact copies (union-find over representatives).
    const reps = [...byHash.values()].map(g => g[0]);
    const parent = new Map(reps.map(r => [r, r]));
    const find = x => { while (parent.get(x) !== x) { parent.set(x, parent.get(parent.get(x))); x = parent.get(x); } return x; };
    for (let a = 0; a < reps.length; a++) {
        const A = items[reps[a]];
        for (let b = a + 1; b < reps.length; b++) {
            const B = items[reps[b]];
            if (popcount(A.hi ^ B.hi) + popcount(A.lo ^ B.lo) > 2) continue;
            if (Math.abs(A.mean[0] - B.mean[0]) > 3 || Math.abs(A.mean[1] - B.mean[1]) > 3 || Math.abs(A.mean[2] - B.mean[2]) > 3) continue;
            parent.set(find(reps[a]), find(reps[b]));
        }
    }
    const nearMap = new Map();
    for (const r of reps) {
        const root = find(r);
        if (!nearMap.has(root)) nearMap.set(root, []);
        nearMap.get(root).push(...byHash.get(items[r].exact));
    }
    const nearGroups = [...nearMap.values()].filter(g => g.length > 1 && new Set(g.map(i => items[i].exact)).size > 1);

    const meta = await readMetadata(csvInput, new Set(items.map(it => stem(it.file.name))));
    const member = i => {
        const it = items[i];
        const url = URL.createObjectURL(it.file);
        lastUrls.push(url);
        const row = meta && meta.byId.get(stem(it.file.name));
        return { name: it.name, thumb: url, width: it.w, height: it.h, meta: row || null };
    };
    const describe = (g, kind) => {
        const members = g.map(member);
        const rows = members.map(m => m.meta).filter(Boolean);
        // Copies that carry different metadata (ignoring the id column) are the dangerous kind.
        const conflicting = rows.length > 1 &&
            new Set(rows.map(r => r.filter((_, c) => c !== meta.idIndex).join("\u0001"))).size > 1;
        return { kind, members, conflicting };
    };

    const groups = [
        ...exactGroups.map(g => describe(g, "exact")),
        ...nearGroups.map(g => describe(g, "near")),
    ].sort((x, y) => (y.conflicting - x.conflicting) || (y.members.length - x.members.length));

    return {
        total: items.length,
        failed,
        inExactGroups: exactGroups.reduce((s, g) => s + g.length, 0),
        exactGroups: exactGroups.length,
        inNearGroups: nearGroups.reduce((s, g) => s + g.length, 0),
        nearGroups: nearGroups.length,
        header: meta ? meta.header : null,
        idColumn: meta ? meta.idColumn : null,
        groups,
    };
}
