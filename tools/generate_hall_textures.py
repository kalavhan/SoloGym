#!/usr/bin/env python3
"""Draws the tiling pixel-art textures for the room prototype (wall bricks, floor slabs).

Pixel density is 64 px per world unit so it matches the character sprites.
Usage: python3 tools/generate_hall_textures.py <out_dir>
"""
import random
import sys
from pathlib import Path
from PIL import Image, ImageDraw

BRICK = [(0x3b, 0x4c, 0x78), (0x42, 0x55, 0x84), (0x35, 0x44, 0x6e), (0x4a, 0x5d, 0x8e)]
MORTAR = (0x15, 0x1d, 0x38)
HI = (0x62, 0x7c, 0xb4)
SH = (0x25, 0x31, 0x55)
GOLD = (0xf4, 0xc5, 0x42)
GOLD_D = (0xc9, 0x8a, 0x1b)


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
    rnd = random.Random(seed)
    s = 128
    im = Image.new("RGB", (s, s), (0x1a, 0x25, 0x48))
    px = im.load()
    base = (0x3a, 0x4e, 0x7e)
    for y in range(2, s):
        for x in range(2, s):
            c = base
            if y == 2:
                c = (0x6a, 0x86, 0xbc)
            elif x == 2:
                c = (0x58, 0x74, 0xaa)
            elif y == s - 1:
                c = (0x24, 0x31, 0x58)
            elif x == s - 1:
                c = (0x2c, 0x3b, 0x66)
            else:
                n = rnd.random()
                if n < 0.05:
                    c = shade(base, 1.12)
                elif n < 0.09:
                    c = shade(base, 0.9)
                # soft vertical sheen so the floor reads as polished
                if (x // 6) % 11 == 0:
                    c = shade(c, 1.06)
            px[x, y] = c
    d = ImageDraw.Draw(im)
    for cx, cy in [(0, 0), (s, 0), (0, s), (s, s)]:
        d.polygon([(cx, cy - 6), (cx + 6, cy), (cx, cy + 6), (cx - 6, cy)], fill=GOLD_D)
        d.polygon([(cx, cy - 4), (cx + 4, cy), (cx, cy + 4), (cx - 4, cy)], fill=GOLD)
    return im


if __name__ == "__main__":
    out = Path(sys.argv[1] if len(sys.argv) > 1 else ".")
    out.mkdir(parents=True, exist_ok=True)
    brick_tile().save(out / "wall_brick.png")
    floor_slab().save(out / "floor_slab.png")
