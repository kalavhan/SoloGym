#!/usr/bin/env python3
"""Read-only provenance/alpha audit for the independently exported Home layers."""
import hashlib
import json
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
PACK = ROOT / "assets/sprites/autosprite/home-room-r1"


def main():
    manifest = json.loads((PACK / "provenance.json").read_text())
    for asset in manifest["assets"]:
        source = PACK / asset["file"]
        content = source.read_bytes()
        assert hashlib.sha256(content).hexdigest() == asset["sha256"], source
        assert content == (ROOT / asset["runtime"]).read_bytes(), "Runtime changed the exported PNG"
        meta = (ROOT / (asset["runtime"] + ".meta")).read_text()
        for setting in ("textureType: 8", "filterMode: 0", "enableMipMap: 0", "nPOTScale: 0", "textureCompression: 0"):
            assert setting in meta, (asset["id"], setting)
        assert "overridden: 1" not in meta, "Platform override can invalidate the shared sprite import"

    room = Image.open(PACK / "room-shell.png")
    exterior = Image.open(PACK / "castle-exterior.png")
    assert room.mode == "RGBA" and room.size == (1672, 941)
    assert exterior.size == (1024, 1536)
    alpha = room.getchannel("A")
    assert max(alpha.crop((910, 150, 1080, 380)).getdata()) < 8, "Window contains opaque/checkerboard pixels"
    assert min(alpha.crop((0, 600, 1672, 941)).getdata()) >= 240, "Floor has accidental holes"
    assert min(alpha.crop((40, 90, 450, 420)).getdata()) >= 240, "Wall has accidental holes"

    approved = ROOT / "design/fantasy-home-r2/home-approved.png"
    assert hashlib.sha256(approved.read_bytes()).hexdigest() == "832b77b23b811095d84ee8c4c7c27c5d77b4cf3ab5d3272b0b502cd8b630aa2c"
    # The complete screenshot must remain outside Unity's runtime asset tree.
    assert not list((ROOT / "app/Assets").rglob("home-approved.png"))
    print("PASS: source hashes, unchanged runtime copies, sprite imports, real window alpha, solid architecture, reference isolation")


if __name__ == "__main__":
    main()
