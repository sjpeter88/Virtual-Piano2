#!/usr/bin/env python3
"""Embed the photographic textures from images/textures/ into index.html as base64 data URIs.

The web app must stay a single self-contained file (no runtime fetches), so each source JPG is
downscaled/recompressed here and written into the <script id="texture-data"> block of index.html
between the BEGIN/END markers. Re-run this whenever a source texture changes:

    python tools/embed_textures.py

Requires Pillow (pip install pillow).
"""
import base64
import io
import json
import os
import re
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "images", "textures")
HTML = os.path.join(ROOT, "index.html")

# name -> (max width, max height, JPEG quality). Sized for how large each surface appears on screen.
SPECS = {
    "floor_oak": (1024, 1024, 78),
    "wall_plaster": (512, 512, 80),
    "brick_fireplace": (768, 768, 80),
    "bookcase_oak": (512, 512, 80),
    "pine_needles": (768, 768, 78),
    "knit_red": (384, 384, 80),
    "knit_green": (384, 384, 80),
    "book_spines": (1536, 384, 80),
    "window_night": (640, 960, 80),
    "rug_persian": (768, 768, 80),
    "gift_wrap_red": (256, 256, 80),
    "gift_wrap_gold": (256, 256, 80),
}

BEGIN = "/* <TEXTURE_DATA_BEGIN> */"
END = "/* <TEXTURE_DATA_END> */"


def encode(name, spec):
    max_w, max_h, quality = spec
    img = Image.open(os.path.join(SRC, name + ".jpg")).convert("RGB")
    scale = min(max_w / img.width, max_h / img.height, 1.0)
    if scale < 1.0:
        img = img.resize((round(img.width * scale), round(img.height * scale)), Image.LANCZOS)
    buf = io.BytesIO()
    img.save(buf, "JPEG", quality=quality, optimize=True, progressive=True)
    return "data:image/jpeg;base64," + base64.b64encode(buf.getvalue()).decode("ascii"), img.size, buf.tell()


def main():
    entries = {}
    total = 0
    for name, spec in SPECS.items():
        uri, size, nbytes = encode(name, spec)
        entries[name] = uri
        total += nbytes
        print(f"{name:18} {size[0]}x{size[1]}  {nbytes / 1024:6.0f} KB")
    print(f"total JPEG bytes: {total / 1024:.0f} KB  (base64 in HTML: {total * 4 / 3 / 1024:.0f} KB)")

    block = BEGIN + "\nwindow.TEXTURE_DATA = " + json.dumps(entries, indent=0) + ";\n" + END
    html = open(HTML, encoding="utf-8", newline="").read()
    pattern = re.compile(re.escape(BEGIN) + r".*?" + re.escape(END), re.S)
    if not pattern.search(html):
        sys.exit("Markers not found in index.html")
    html = pattern.sub(lambda m: block, html, count=1)
    open(HTML, "w", encoding="utf-8", newline="").write(html)
    print("index.html updated")


if __name__ == "__main__":
    main()
