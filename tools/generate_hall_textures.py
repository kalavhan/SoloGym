#!/usr/bin/env python3
"""Draws the tiling pixel-art textures for the room prototype (wall bricks, floor slabs).

Pixel density is 64 px per world unit so it matches the character sprites.
Usage: python3 tools/generate_hall_textures.py <out_dir>
"""
import random
import sys
from pathlib import Path
from PIL import Image, ImageDraw

# Neutral warm stone: the room's colour comes from its lights (warm torches, blue portal), not the texture.
BRICK = [(0x5a, 0x55, 0x5c), (0x62, 0x5c, 0x60), (0x52, 0x4d, 0x55), (0x6a, 0x63, 0x64)]
MORTAR = (0x22, 0x1e, 0x26)
HI = (0x86, 0x7e, 0x7e)
SH = (0x3a, 0x34, 0x3c)
GOLD = (0xf4, 0xc5, 0x42)
GOLD_D = (0xc9, 0x8a, 0x1b)
SLAB = [(0x5c, 0x58, 0x60), (0x55, 0x51, 0x5a), (0x63, 0x5d, 0x62), (0x58, 0x55, 0x5e)]
GROUT = (0x24, 0x20, 0x28)


def shade(c, k):
    return tuple(max(0, min(255, int(v * k))) for v in c)


def brick_tile(seed=3):
    rnd = random.Random(seed)
    im = Image.new("RGB", (64, 64), MORTAR)
    px = im.load()
    for row in range(4):
        off = 16 if row % 2 else 0
        for col in range(3):
            x0 = col * 32 - off
            base = rnd.choice(BRICK)
            for y in range(row * 16, row * 16 + 15):
                for x in range(x0 + 1, x0 + 31):
                    xx = x % 64
                    c = base
                    if y == row * 16:
                        c = HI
                    elif y == row * 16 + 14:
                        c = SH
                    elif x == x0 + 1:
                        c = shade(base, 1.18)
                    elif x == x0 + 30:
                        c = shade(base, 0.82)
                    elif rnd.random() < 0.06:
                        c = shade(base, rnd.choice([0.88, 1.12]))
                    px[xx, y] = c
    return im


def floor_slab(seed=5):
    """128 px = one 3.2-unit repeat holding 2x2 polished slabs (1.6 units each), with a gold stud where four meet."""
    rnd = random.Random(seed)
    s, h = 128, 64
    im = Image.new("RGB", (s, s), GROUT)
    px = im.load()
    for sy in range(2):
        for sx in range(2):
            base = SLAB[(sx + sy * 2 + seed) % len(SLAB)]
            x0, y0 = sx * h, sy * h
            for y in range(y0 + 1, y0 + h):
                for x in range(x0 + 1, x0 + h):
                    c = base
                    if y == y0 + 1:
                        c = (0x80, 0x7a, 0x7e)
                    elif x == x0 + 1:
                        c = (0x72, 0x6c, 0x72)
                    elif y == y0 + h - 1:
                        c = (0x3a, 0x36, 0x40)
                    elif x == x0 + h - 1:
                        c = (0x44, 0x40, 0x4a)
                    else:
                        n = rnd.random()
                        if n < 0.05:
                            c = shade(base, 1.1)
                        elif n < 0.09:
                            c = shade(base, 0.9)
                        if (x + y // 3) % 23 == 0:  # faint diagonal sheen: polished stone
                            c = shade(c, 1.07)
                    px[x, y] = c
            # one hairline crack on some slabs
            if rnd.random() < 0.5:
                x, y = x0 + rnd.randint(12, 50), y0 + rnd.randint(12, 50)
                for _ in range(rnd.randint(6, 12)):
                    px[x, y] = shade(base, 0.7)
                    x += rnd.choice([-1, 0, 1]); y += 1
    d = ImageDraw.Draw(im)
    for cx, cy in [(0, 0), (s, 0), (0, s), (s, s)]:
        d.polygon([(cx, cy - 4), (cx + 4, cy), (cx, cy + 4), (cx - 4, cy)], fill=GOLD_D)
        d.polygon([(cx, cy - 2), (cx + 2, cy), (cx, cy + 2), (cx - 2, cy)], fill=GOLD)
    return im


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    out.mkdir(parents=True, exist_ok=True)
    brick_tile().save(out / "wall_brick.png")
    floor_slab().save(out / "floor_slab.png")
