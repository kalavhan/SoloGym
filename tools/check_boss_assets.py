#!/usr/bin/env python3
"""Verify the native dungeon preserves independent reviewed source exports."""
import hashlib,json
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
manifest=json.loads((ROOT/'design/fantasy-boss-r1/manifest.json').read_text())
for item in ('background','render'):
    record=manifest[item];p=ROOT/record['file'];assert hashlib.sha256(p.read_bytes()).hexdigest()==record['sha256'],p
room=ROOT/manifest['background']['file']
meta=json.loads((ROOT/manifest['guardianManifest']).read_text())
boss=ROOT/'assets/sprites/pixellab/stone-guardian-r1'/meta['file']
assert hashlib.sha256(boss.read_bytes()).hexdigest()==meta['sha256']
im=Image.open(boss);assert im.size==(256,256) and im.mode=='RGBA' and im.getchannel('A').getextrema()==(0,255)
box=im.getbbox();assert box[0]>0 and box[1]>0 and box[2]<256 and box[3]<256,box
assert Image.open(room).size==(640,360)
for name,source in [('background',room),('guardian',boss)]:
    assert (ROOT/f'app/Assets/SoloGym/Resources/Rooms/BossR1/{name}.png').read_bytes()==source.read_bytes()
for character in (ROOT/'assets/sprites/pixellab/barbarian-256-r1/native').glob('*.png'):
    assert Image.open(character).size==(256,256)
    assert (ROOT/'app/Assets/SoloGym/Resources/Characters/PixelLabR1'/character.name).read_bytes()==character.read_bytes()
print('Boss assets: separate room/guardian, source hashes, transparent margins and unchanged Barbarian copies verified')
