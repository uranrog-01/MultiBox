"""Renders the MultiBox icon from its SVG masters and writes icon.ico plus PNG copies.

Run from the repository root:  python -I design/icon/build-icon.py
Needs Playwright's headless Chromium (%LOCALAPPDATA%\\ms-playwright) and Pillow.
"""
import glob
import io
import os
import struct
import subprocess
import sys
import tempfile

from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MASTER = os.path.join(ROOT, "design", "icon", "multibox-icon.svg")
SMALL = os.path.join(ROOT, "design", "icon", "multibox-icon-small.svg")
# Windows picks from these: 16/20/24 small UI and tray at 100-150 %, 32/40/48 for taskbar and Explorer, larger for tiles.
ICO_SIZES = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256]


def chromium():
    shells = sorted(glob.glob(os.path.join(os.environ["LOCALAPPDATA"], "ms-playwright", "chromium_headless_shell-*",
                                           "chrome-headless-shell-win64", "chrome-headless-shell.exe")))
    if not shells:
        sys.exit("Playwright's chromium headless shell was not found.")
    return shells[-1]


def render(svg_path, size, work):
    page = os.path.join(work, f"icon-{size}.html")
    out = os.path.join(work, f"icon-{size}.png")
    with open(svg_path, encoding="utf-8") as f:
        svg = f.read().replace("<svg ", f'<svg width="{size}" height="{size}" ', 1)
    with open(page, "w", encoding="utf-8") as f:
        f.write(f"<!doctype html><style>html,body{{margin:0;background:transparent}}svg{{display:block}}</style>{svg}")
    subprocess.run([chromium(), "--headless", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                    "--default-background-color=00000000", f"--window-size={size},{size}",
                    f"--screenshot={out}", "file:///" + page.replace("\\", "/")],
                   check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    image = Image.open(out).convert("RGBA")
    if image.size != (size, size):
        raise RuntimeError(f"{size}px render came back as {image.size}")
    return image


def png_bytes(image):
    buffer = io.BytesIO()
    image.save(buffer, format="PNG", optimize=True)
    return buffer.getvalue()


def write_ico(path, images):
    entries, blobs = [], []
    offset = 6 + 16 * len(images)
    for image in images:
        data = png_bytes(image)
        w, h = image.size
        entries.append(struct.pack("<BBBBHHII", w % 256, h % 256, 0, 0, 1, 32, len(data), offset))
        blobs.append(data)
        offset += len(data)
    with open(path, "wb") as f:
        f.write(struct.pack("<HHH", 0, 1, len(images)))
        f.writelines(entries)
        f.writelines(blobs)


def main():
    with tempfile.TemporaryDirectory() as work:
        frames = [render(SMALL if size <= 24 else MASTER, size, work) for size in ICO_SIZES]
        big = render(MASTER, 512, work)
        write_ico(os.path.join(ROOT, "icon.ico"), frames)
        big.save(os.path.join(ROOT, "icon.png"), optimize=True)
        big.save(os.path.join(ROOT, "docs", "assets", "icon.png"), optimize=True)
        frames[ICO_SIZES.index(256)].save(os.path.join(ROOT, "UI", "icon.png"), optimize=True)
        # Contact sheet for review: every ICO frame at 1:1, on dark and light backgrounds.
        sheet = Image.new("RGBA", (sum(ICO_SIZES) + 20 * (len(ICO_SIZES) + 1), 2 * 256 + 60), (0, 0, 0, 0))
        for row, color in enumerate([(24, 24, 27, 255), (243, 244, 246, 255)]):
            band = Image.new("RGBA", (sheet.width, 256 + 30), color)
            sheet.paste(band, (0, row * (256 + 30)))
            x = 20
            for size, frame in zip(ICO_SIZES, frames):
                sheet.alpha_composite(frame, (x, row * (256 + 30) + 15 + (256 - size) // 2))
                x += size + 20
        sheet.save(os.path.join(ROOT, "design", "icon", "icon-sizes-preview.png"))
    print("Wrote icon.ico (" + ", ".join(map(str, ICO_SIZES)) + " px), icon.png, UI/icon.png, docs/assets/icon.png")


if __name__ == "__main__":
    main()
