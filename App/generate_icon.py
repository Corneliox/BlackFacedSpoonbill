"""
Generates a high-resolution 1024x1024 macOS/Windows Application Icon for Spoonbill AI Studio.
Creates:
- App/icon.png (1024x1024 RGBA with macOS squircle geometry, dark studio gradient, and Spoonbill AI emblem)
- App/icon.ico (multi-size Windows icon)
"""

import os
import math
import numpy as np
import cv2
from PIL import Image, ImageDraw

OUT_DIR = os.path.dirname(os.path.abspath(__file__))


def create_studio_icon(size: int = 1024) -> Image.Image:
    # Create high-res RGBA canvas
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Outer macOS Big Sur / Sonoma Squircle Pad (with margin)
    pad = int(size * 0.08)
    radius = int(size * 0.21)
    x0, y0, x1, y1 = pad, pad, size - pad, size - pad

    # Base shadow / border
    draw.rounded_rectangle([x0, y0, x1, y1], radius=radius, fill=(20, 22, 28, 255), outline=(246, 130, 31, 190), width=int(size * 0.016))

    # Inner dark card panel
    in_pad = pad + int(size * 0.025)
    draw.rounded_rectangle(
        [in_pad, in_pad, size - in_pad, size - in_pad],
        radius=int(radius * 0.88),
        fill=(26, 29, 38, 255),
        outline=(59, 130, 246, 95),
        width=int(size * 0.008)
    )

    # 2. Draw AI Detection Bounding Box & Target Reticle Corners (Cyan/Blue accent)
    bx0, by0, bx1, by1 = int(size * 0.22), int(size * 0.22), int(size * 0.78), int(size * 0.78)
    corner_len = int(size * 0.09)
    stroke_w = int(size * 0.022)
    cyan = (56, 189, 248, 235)

    # Top-left reticle
    draw.line([(bx0, by0 + corner_len), (bx0, by0), (bx0 + corner_len, by0)], fill=cyan, width=stroke_w)
    # Top-right reticle
    draw.line([(bx1 - corner_len, by0), (bx1, by0), (bx1, by0 + corner_len)], fill=cyan, width=stroke_w)
    # Bottom-left reticle
    draw.line([(bx0, by1 - corner_len), (bx0, by1), (bx0 + corner_len, by1)], fill=cyan, width=stroke_w)
    # Bottom-right reticle
    draw.line([(bx1 - corner_len, by1), (bx1, by1), (bx1, by1 - corner_len)], fill=cyan, width=stroke_w)

    # 3. Draw Stylized Black-Faced Spoonbill Head & Spatulate Bill Silhouette
    # White head crest + neck
    white = (248, 250, 252, 255)
    orange = (246, 130, 31, 255)
    dark_mask = (17, 19, 24, 255)

    # Neck & head contour polygon
    head_cx, head_cy = int(size * 0.42), int(size * 0.45)
    head_r = int(size * 0.13)
    draw.ellipse([head_cx - head_r, head_cy - head_r, head_cx + head_r, head_cy + head_r], fill=white)

    # Breeding crest plumes (orange/gold accent)
    crest_pts = [
        (int(size * 0.34), int(size * 0.36)),
        (int(size * 0.24), int(size * 0.33)),
        (int(size * 0.31), int(size * 0.42)),
        (int(size * 0.23), int(size * 0.42)),
        (int(size * 0.33), int(size * 0.47)),
    ]
    draw.polygon(crest_pts, fill=orange)

    # Graceful S-curved neck down to chest
    neck_pts = [
        (int(size * 0.35), int(size * 0.48)),
        (int(size * 0.46), int(size * 0.50)),
        (int(size * 0.43), int(size * 0.72)),
        (int(size * 0.30), int(size * 0.72)),
    ]
    draw.polygon(neck_pts, fill=white)

    # Diagnostic Spatulate (Spoon-Shaped) Bill extending right-downward
    bill_shaft = [
        (int(size * 0.49), int(size * 0.42)),
        (int(size * 0.71), int(size * 0.56)),
        (int(size * 0.68), int(size * 0.61)),
        (int(size * 0.46), int(size * 0.48)),
    ]
    draw.polygon(bill_shaft, fill=orange)

    # Broad spoon tip of the bill
    spoon_cx, spoon_cy = int(size * 0.71), int(size * 0.60)
    spoon_rx, spoon_ry = int(size * 0.065), int(size * 0.042)
    draw.ellipse(
        [spoon_cx - spoon_rx, spoon_cy - spoon_ry, spoon_cx + spoon_rx, spoon_cy + spoon_ry],
        fill=orange
    )

    # Black facial lore + bright eye
    face_pts = [
        (int(size * 0.42), int(size * 0.40)),
        (int(size * 0.52), int(size * 0.43)),
        (int(size * 0.49), int(size * 0.49)),
        (int(size * 0.41), int(size * 0.47)),
    ]
    draw.polygon(face_pts, fill=dark_mask)
    eye_x, eye_y, eye_r = int(size * 0.44), int(size * 0.43), int(size * 0.018)
    draw.ellipse([eye_x - eye_r, eye_y - eye_r, eye_x + eye_r, eye_y + eye_r], fill=(246, 130, 31, 255))

    return img


if __name__ == "__main__":
    icon_img = create_studio_icon(1024)
    png_path = os.path.join(OUT_DIR, "icon.png")
    ico_path = os.path.join(OUT_DIR, "icon.ico")
    icon_img.save(png_path, format="PNG")
    icon_img.save(ico_path, format="ICO", sizes=[(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (16, 16)])
    print(f"Generated {png_path} and {ico_path}")
