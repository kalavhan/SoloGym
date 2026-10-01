#!/usr/bin/env python3
"""Check source fidelity, full character canvases and independent Home layers.

Run from any directory with Python 3 and Pillow. This does not generate art.
"""
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets/sprites/pixellab"
RESOURCES = ROOT / "app/Assets/SoloGym/Resources"


def read(path):
    return json.loads(path.read_text())


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def full_sprite(path, maximum=None):
    with Image.open(path) as source:
        assert source.mode == "RGBA", f"Missing alpha: {path}"
        if maximum:
            assert source.width <= maximum and source.height <= maximum, path
        box = source.getchannel("A").getbbox()
        assert box and 0 < box[0] < box[2] < source.width, path
        assert 0 < box[1] < box[3] < source.height, path


def main():
    fronts = ASSETS / "barbarian-256-r1"
    catalog = read(RESOURCES / "Characters/PixelLabR1/catalog.json")["characters"]
    source_entries = {c["id"]: c for c in read(fronts / "manifest.json")["characters"]}
    assert len(catalog) == len(source_entries) == 8
    for entry in catalog:
        source = fronts / source_entries[entry["id"]]["file"]
        assert digest(source) == source_entries[entry["id"]]["spriteSha256"]
        assert source.read_bytes() == (RESOURCES / (entry["resource"] + ".png")).read_bytes()
        assert source.read_bytes() == (RESOURCES / (entry["resource"] + "-portrait.png")).read_bytes()
        full_sprite(source, 256)
        rect = entry["portraitRect"]
        assert 0 <= rect["x"] and rect["x"] + rect["width"] <= 256
        assert 0 <= rect["y"] and rect["y"] + rect["height"] <= 256

    directions = ASSETS / "barbarian-directions-r1"
    directional = read(directions / "manifest.json")["characters"]
    expected = {"south", "south-east", "east", "north-east", "north", "north-west", "west", "south-west"}
    assert {c["id"] for c in directional} == set(source_entries)
    for character in directional:
        assert read(directions / character["metadata"]), "Preserve provider metadata"
        assert {v["direction"] for v in character["rotations"]} == expected
        for rotation in character["rotations"]:
            path = directions / rotation["file"]
            assert digest(path) == rotation["sha256"], path
            full_sprite(path, 256)

    hall = ASSETS / "training-hall-r1"
    manifest = read(hall / "manifest.json")
    runtime = RESOURCES / "Rooms/TrainingHallR1"
    props = read(runtime / "objects.json")["objects"]
    assert len(props) == 14
    assert len({p["resource"] for p in props}) == 14
    for item in manifest["objects"]:
        path = hall / item["file"]
        assert digest(path) == item["sha256"], path
        assert path.read_bytes() == (hall / item["source"]).read_bytes(), path
        assert path.read_bytes() == (runtime / path.name).read_bytes(), path
        full_sprite(path)

    room = read(runtime / "room.json")
    assert room["id"] == "home.training-hall.r1"
    architecture = Image.open(runtime / "architecture.png").convert("RGBA")
    exterior = Image.open(runtime / "exterior.png").convert("RGBA")
    assert architecture.size == (640, 360)
    layer = Image.new("RGBA", architecture.size)
    layer.paste(exterior, (room["exteriorRect"]["x"], room["exteriorRect"]["y"]))
    reconstructed = Image.alpha_composite(layer, architecture)
    assert reconstructed.tobytes() == Image.open(hall / "native/room-0.png").convert("RGBA").tobytes()
    rug = Image.open(runtime / "exercise-rug.png")
    trim = Image.open(runtime / "exercise-rug-trim.png")
    assert rug.size == trim.size == (384, 112)
    assert rug.getpixel((192, 56))[3] == 255, "Rug center must remain fabric, not a hole"
    assert digest(runtime / "exercise-rug-trim.png") == manifest["rugAssembly"]["overlaySha256"]
    for folder in (runtime, RESOURCES / "Characters/PixelLabR1"):
        for png in folder.glob("*.png"):
            meta = png.with_suffix(".png.meta").read_text()
            assert "enableMipMap: 0" in meta and "filterMode: 0" in meta, png
            assert "textureType: 8" in meta, png
    print("PASS: 8 unchanged frontal sprites; 64 complete direction exports; 14 independent props; exact room decomposition; opaque rug; point-filtered Unity imports.")


if __name__ == "__main__":
    main()
