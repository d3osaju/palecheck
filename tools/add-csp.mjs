// Adds a Content-Security-Policy <meta> to the published index.html. Inline scripts (the import map
// that `dotnet publish` fills in, and the service-worker registration) are allowed by their SHA-256
// hashes, so no 'unsafe-inline' is needed for scripts.
import { createHash } from "node:crypto";
import { readFileSync, writeFileSync } from "node:fs";

const file = process.argv[2] ?? "dist/index.html";
let html = readFileSync(file, "utf8");
if (html.includes('http-equiv="Content-Security-Policy"')) {
    console.log("CSP already present");
    process.exit(0);
}

// Browsers hash the script text after normalising CRLF/CR to LF, so do the same.
const hashes = [...html.matchAll(/<script(?![^>]*\bsrc=)[^>]*>([\s\S]*?)<\/script>/g)]
    .map(m => `'sha256-${createHash("sha256").update(m[1].replace(/\r\n?/g, "\n"), "utf8").digest("base64")}'`);

const csp = [
    "default-src 'self'",
    `script-src 'self' 'wasm-unsafe-eval' ${hashes.join(" ")}`,
    "style-src 'self' 'unsafe-inline'",
    "img-src 'self' data: blob:",
    "connect-src 'self'",
    "font-src 'self'",
    "worker-src 'self'",
    "manifest-src 'self'",
    "object-src 'none'",
    "base-uri 'self'",
    "form-action 'none'",
].join("; ");

html = html.replace(/<meta charset="utf-8" \/>/, m => `${m}\n    <meta http-equiv="Content-Security-Policy" content="${csp}" />`);
writeFileSync(file, html);
console.log(`CSP added with ${hashes.length} inline script hash(es)`);
