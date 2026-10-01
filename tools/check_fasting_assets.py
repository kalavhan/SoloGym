"""Verify original PixelLab exports, separate-frame metadata and runtime copies."""
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
art = ROOT / 'assets/sprites/pixellab/arcane-clock-r1'
runtime = ROOT / 'app/Assets/SoloGym/Resources/Rooms/FastingR1'
atlas = json.loads((art / 'motion-atlas.json').read_text())
assert len(atlas['frames']) == 9
assert (art / 'motion-atlas.json').read_bytes() == (runtime / 'motion-atlas.json').read_bytes()
for name, source in [('clock', art / 'native/clock.png'), ('background', ROOT / 'assets/sprites/pixellab/quiet-chamber-r1/native/chamber.png')]:
    assert source.read_bytes() == (runtime / (name + '.png')).read_bytes(), name
    assert Image.open(source).size == ((256, 256) if name == 'clock' else (640, 360))
for i, frame in enumerate(atlas['frames']):
    source = art / frame['file']
    assert i == frame['index'] and frame['durationMs'] == 650
    assert hashlib.sha256(source.read_bytes()).hexdigest() == frame['sha256']
    assert Image.open(source).size == (256, 256)
    assert source.read_bytes() == (runtime / 'Motion' / source.name).read_bytes()
assert atlas['presentation']['ringInnerRadiusSourcePixels'] == 61
assert atlas['presentation']['ringOuterRadiusSourcePixels'] == 82
assert atlas['presentation']['maximumOverlayOpacity'] == 0.32
print('Fasting: original room, clock, nine animation frames, hashes and runtime metadata match.')
