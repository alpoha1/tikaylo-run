#!/usr/bin/env python3
"""Вырезает Тилкайо из фона и сохраняет квадратный PNG 512x512 с прозрачностью.
Использование:  pip install rembg pillow numpy ; python make_sprite.py source.png out.png
"""
import sys
from PIL import Image
import numpy as np
from rembg import remove, new_session

src, dst = sys.argv[1], sys.argv[2]
im = Image.open(src).convert("RGB")
cut = remove(im, session=new_session("u2net"))
a = np.array(cut)
alpha = a[..., 3]
ys, xs = np.where(alpha > 40)
x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
cut = cut.crop((x0, y0, x1 + 1, y1 + 1))
w, h = cut.size
print("bbox", x0, y0, x1, y1, "size", w, h)
S = 512
scale = (S - 8) / max(w, h)
cut = cut.resize((max(1, int(w * scale)), max(1, int(h * scale))), Image.LANCZOS)
canvas = Image.new("RGBA", (S, S), (0, 0, 0, 0))
canvas.paste(cut, ((S - cut.width) // 2, S - cut.height - 2), cut)
canvas.save(dst)
print("saved", dst, cut.size, "offset", (S - cut.width) // 2, S - cut.height - 2, "scale", scale, "orig bbox", (x0,y0))
