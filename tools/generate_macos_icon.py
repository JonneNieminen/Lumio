#!/usr/bin/env python3
"""Generate Lumio's macOS app icons from the brand geometry.

macOS 26 and later put every legacy .icns icon on a grey "plate". Only icons made in Apple's
current format, an Icon Composer document (.icon) compiled by actool into Assets.car, render
as the app's own shape. So this script writes two things:

* assets/icons/Lumio.icon: the Icon Composer document (navy fill plus the mark as one layer).
  build/macos/make-app.sh compiles it into the bundle's Assets.car (CFBundleIconName = Lumio).
* assets/icons/lumio-icon.icns: the fallback for macOS 11 to 15, drawn on Apple's legacy icon
  grid (an 824 px continuous-corner squircle centered on a 1024 px canvas).

The mark uses the brand's 64-unit grid (see lumio-theme/THEME.md and tokens.json).
Requires ImageMagick (`magick`) and macOS `iconutil`.
Usage: python3 tools/generate_macos_icon.py
"""

from __future__ import annotations

import json
import math
import shutil
import subprocess
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
OUT_ICNS = ROOT / "assets" / "icons" / "lumio-icon.icns"
OUT_PNG = ROOT / "assets" / "icons" / "lumio-icon-macos-1024.png"
OUT_ICON_DOC = ROOT / "assets" / "icons" / "Lumio.icon"

NAVY = "#232850"
CREAM = "#F4EFE2"
AMBER = "#FFC857"

CANVAS = 1024
BODY = 824                       # Apple's macOS icon body size on a 1024 canvas
INSET = (CANVAS - BODY) / 2      # 100 px margin on every side
SQUIRCLE_EXPONENT = 5.0          # superellipse close to Apple's continuous-corner shape


def squircle_path(size: float, offset: float, n: float = SQUIRCLE_EXPONENT, steps: int = 720) -> str:
    """Superellipse |x|^n + |y|^n = 1 scaled to size x size at (offset, offset)."""
    r = size / 2
    cx = cy = offset + r
    points = []
    for i in range(steps):
        t = 2 * math.pi * i / steps
        c, s = math.cos(t), math.sin(t)
        x = cx + r * math.copysign(abs(c) ** (2 / n), c)
        y = cy + r * math.copysign(abs(s) ** (2 / n), s)
        points.append(f"{x:.2f},{y:.2f}")
    return "M" + " L".join(points) + " Z"


def mark_svg() -> str:
    """The mark alone on a transparent 1024 canvas, at the brand scale (64-unit grid = canvas)."""
    k = CANVAS / 64
    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{CANVAS}" height="{CANVAS}" viewBox="0 0 {CANVAS} {CANVAS}">
  <g transform="scale({k})">
    <circle cx="30" cy="32" r="14" fill="none" stroke="{CREAM}" stroke-opacity="0.16" stroke-width="7"/>
    <path d="M16 14V32A14 14 0 0 0 30 46H44" fill="none" stroke="{CREAM}" stroke-width="7"
          stroke-linecap="round" stroke-linejoin="round"/>
    <circle cx="47" cy="46" r="10" fill="{AMBER}" fill-opacity="0.25"/>
    <circle cx="47" cy="46" r="5" fill="{AMBER}"/>
  </g>
</svg>
"""


def srgb(hex_color: str) -> str:
    r, g, b = (int(hex_color[i:i + 2], 16) / 255 for i in (1, 3, 5))
    return f"srgb:{r:.5f},{g:.5f},{b:.5f},1.00000"


def write_icon_document(tmp_path: Path) -> None:
    """Icon Composer document: solid brand-navy fill, the mark as one flat layer.
    Brand rules: no shadows or gradients on the mark, so shadow and translucency are off."""
    if OUT_ICON_DOC.exists():
        shutil.rmtree(OUT_ICON_DOC)
    (OUT_ICON_DOC / "Assets").mkdir(parents=True)
    src = tmp_path / "mark.svg"
    src.write_text(mark_svg())
    subprocess.run(["magick", "-background", "none", "-density", "288", str(src),
                    "-resize", f"{CANVAS}x{CANVAS}", str(OUT_ICON_DOC / "Assets" / "mark.png")], check=True)
    doc = {
        "fill": {"solid": srgb(NAVY)},
        "groups": [
            {
                "layers": [{"image-name": "mark.png", "name": "mark"}],
                "shadow": {"kind": "none", "opacity": 0},
                "translucency": {"enabled": False, "value": 0},
            }
        ],
        "supported-platforms": {"squares": ["macOS"]},
    }
    (OUT_ICON_DOC / "icon.json").write_text(json.dumps(doc, indent=2) + "\n")


def svg() -> str:
    # The brand mark lives on a 64-unit grid; map that grid onto the squircle body.
    k = BODY / 64
    t = f"translate({INSET} {INSET}) scale({k})"
    return f"""<svg xmlns="http://www.w3.org/2000/svg" width="{CANVAS}" height="{CANVAS}" viewBox="0 0 {CANVAS} {CANVAS}">
  <path d="{squircle_path(BODY, INSET)}" fill="{NAVY}"/>
  <g transform="{t}">
    <circle cx="30" cy="32" r="14" fill="none" stroke="{CREAM}" stroke-opacity="0.16" stroke-width="7"/>
    <path d="M16 14V32A14 14 0 0 0 30 46H44" fill="none" stroke="{CREAM}" stroke-width="7"
          stroke-linecap="round" stroke-linejoin="round"/>
    <circle cx="47" cy="46" r="10" fill="{AMBER}" fill-opacity="0.25"/>
    <circle cx="47" cy="46" r="5" fill="{AMBER}"/>
  </g>
</svg>
"""


def main() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        tmp_path = Path(tmp)
        src = tmp_path / "icon.svg"
        src.write_text(svg())
        master = tmp_path / "icon-1024.png"
        subprocess.run(["magick", "-background", "none", "-density", "288", str(src),
                        "-resize", f"{CANVAS}x{CANVAS}", str(master)], check=True)

        iconset = tmp_path / "lumio.iconset"
        iconset.mkdir()
        for base in (16, 32, 128, 256, 512):
            for scale in (1, 2):
                px = base * scale
                name = f"icon_{base}x{base}{'@2x' if scale == 2 else ''}.png"
                subprocess.run(["magick", str(master), "-resize", f"{px}x{px}", str(iconset / name)], check=True)
        subprocess.run(["iconutil", "-c", "icns", str(iconset), "-o", str(OUT_ICNS)], check=True)
        shutil.copy(master, OUT_PNG)
        write_icon_document(tmp_path)
    for path in (OUT_ICON_DOC, OUT_ICNS, OUT_PNG):
        print(f"Wrote {path.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
