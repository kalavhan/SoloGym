#!/usr/bin/env python3
"""Seeded fantasy name generator for SoloGym content.

Names are built from per-theme word pools in themes.json, so editing a pool
or the seed changes the names without touching code. Usage:
    python3 name_generator.py <theme> [count] [seed]
"""
import json, random, sys, pathlib

def load(path=None):
    p = pathlib.Path(path or pathlib.Path(__file__).with_name("themes.json"))
    return json.loads(p.read_text(encoding="utf-8"))

def make(theme, themes, rng):
    t = themes[theme]
    start, mid, end = rng.choice(t["start"]), rng.choice(t.get("mid", [""])), rng.choice(t["end"])
    if rng.random() < 0.5:
        mid = ""
    name = (start + mid + end).replace("  ", " ")
    return name[0].upper() + name[1:]

def generate(theme, count=6, seed=1, path=None):
    themes = load(path)
    rng = random.Random(f"{theme}:{seed}")
    seen, out = set(), []
    while len(out) < count:
        n = make(theme, themes, rng)
        if n not in seen and 5 <= len(n) <= 12:
            seen.add(n); out.append(n)
    return out

if __name__ == "__main__":
    theme = sys.argv[1]
    count = int(sys.argv[2]) if len(sys.argv) > 2 else 6
    seed = int(sys.argv[3]) if len(sys.argv) > 3 else 1
    print("\n".join(generate(theme, count, seed)))
