"""Draws the PaleCheck app icons and the card's QR code into wwwroot.

Usage: python model/make_assets.py   (needs pillow and qrcode)
"""
import math
from pathlib import Path

import qrcode
from PIL import Image, ImageDraw

WEB = Path(__file__).resolve().parent.parent / "src" / "PaleCheck.Web" / "wwwroot"
APP_URL = "https://palecheck.zetalabs.in"


def icons():
    S = 1024  # draw large, downsample for smooth edges
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([0, 0, S - 1, S - 1], radius=230, fill=(180, 35, 75, 255))
    # Eye outline (almond) as a polygon from two arcs.
    top = [(S * 0.14 + i / 100 * S * 0.72, S * 0.5 - math.sin(math.pi * i / 100) * S * 0.24) for i in range(101)]
    bot = [(S * 0.86 - i / 100 * S * 0.72, S * 0.5 + math.sin(math.pi * i / 100) * S * 0.24) for i in range(101)]
    d.polygon(top + bot, fill=(253, 236, 240, 255))
    # Pulled-down lower lid: the pink conjunctiva crescent.
    lid = [(S * 0.25 + i / 100 * S * 0.5, S * 0.6 + math.sin(math.pi * i / 100) * S * 0.1) for i in range(101)]
    lid2 = [(S * 0.75 - i / 100 * S * 0.5, S * 0.6 + math.sin(math.pi * i / 100) * S * 0.035) for i in range(101)]
    d.polygon(lid + lid2, fill=(236, 92, 125, 255))
    # Iris + pupil.
    d.ellipse([S * 0.39, S * 0.33, S * 0.61, S * 0.55], fill=(180, 35, 75, 255))
    d.ellipse([S * 0.46, S * 0.40, S * 0.54, S * 0.48], fill=(40, 12, 22, 255))
    d.ellipse([S * 0.505, S * 0.385, S * 0.535, S * 0.415], fill=(255, 255, 255, 230))
    for size in (512, 192):
        img.resize((size, size), Image.LANCZOS).save(WEB / f"icon-{size}.png")


def qr_svg():
    """The app's address as a QR code, so a printed card opens the app when scanned."""
    qr = qrcode.QRCode(error_correction=qrcode.constants.ERROR_CORRECT_M, border=2)
    qr.add_data(APP_URL)
    qr.make(fit=True)
    m = qr.get_matrix()
    n = len(m)
    path = "".join(f"M{x},{y}h1v1h-1z" for y, row in enumerate(m) for x, on in enumerate(row) if on)
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {n} {n}" shape-rendering="crispEdges">'
           f'<rect width="{n}" height="{n}" fill="#fff"/><path d="{path}" fill="#000"/></svg>\n')
    (WEB / "img").mkdir(exist_ok=True)
    (WEB / "img" / "qr.svg").write_text(svg)


if __name__ == "__main__":
    icons()
    qr_svg()
    print("icons and QR written")
