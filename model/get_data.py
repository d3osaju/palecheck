"""Download and unpack CP-AnemiC (CC BY 4.0) into data/raw/cp-anemic.

Source: Appiahene et al., Mendeley Data, DOI 10.17632/m53vz6b7fx.1
Needs bsdtar for the RAR archive: built into Windows 10+ and macOS as `tar`;
on Linux install libarchive-tools.
"""
import hashlib
import shutil
import subprocess
import urllib.request
from pathlib import Path

URL = ("https://data.mendeley.com/public-files/datasets/m53vz6b7fx/files/"
       "904a85cb-aa4b-4060-9064-3405f279ce65/file_downloaded")
SHA256 = "78d7c2eca49d250f3208b7cf384238d0793f5e772d33153e6ee7d73294cd319c"
RAW = Path(__file__).resolve().parent.parent / "data" / "raw"


def main():
    RAW.mkdir(parents=True, exist_ok=True)
    archive = RAW / "cp-anemic.rar"
    if not archive.exists():
        print("downloading CP-AnemiC (8.3 MB)...")
        urllib.request.urlretrieve(URL, archive)
    digest = hashlib.sha256(archive.read_bytes()).hexdigest()
    if digest != SHA256:
        raise SystemExit(f"checksum mismatch: {digest}")
    out = RAW / "cp-anemic"
    out.mkdir(exist_ok=True)
    # bsdtar (libarchive) reads RAR; it is plain `tar` on Windows and macOS.
    tar = shutil.which("bsdtar") or "tar"
    subprocess.run([tar, "-xf", str(archive), "-C", str(out)], check=True)
    n = len(list(out.rglob("*.png")))
    print(f"extracted {n} images to {out}")


if __name__ == "__main__":
    main()
