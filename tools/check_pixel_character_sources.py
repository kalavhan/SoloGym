#!/usr/bin/env python3
"""Verify preserved Barbarian exports, alpha-only derivatives and runtime framing. Requires Pillow."""
import hashlib
import json
from pathlib import Path
from PIL import Image, ImageChops

ROOT = Path(__file__).resolve().parents[1]


def verify():
    base = ROOT / 'assets/sprites/autosprite'
    sources = json.loads((base / 'barbarian-user-r1/manifest.json').read_text())
    manifest = json.loads((base / 'barbarian-viewport-r1/manifest.json').read_text())
    catalog = json.loads((ROOT / 'app/Assets/SoloGym/Resources/Characters/BarbarianR1/catalog.json').read_text())
    original_records = {entry['key']: entry for entry in sources['characters']}
    runtime_records = {entry['id']: entry for entry in catalog['characters']}
    expected = {f'{gender}-{body}' for gender in ('female', 'male') for body in ('skinny', 'medium', 'fat', 'muscular')}
    assert set(original_records) == set(runtime_records) == {entry['key'] for entry in manifest['characters']} == expected
    for entry in manifest['characters']:
        key = entry['key']
        for kind in ('source', 'transparent', 'runtime'):
            content = (ROOT / entry[kind + '_path']).read_bytes()
            assert hashlib.sha256(content).hexdigest() == entry[kind + '_sha256'], (key, kind, 'hash')
        assert entry['source_sha256'] == original_records[key]['sha256'], (key, 'preserved original')
        assert entry['runtime_sha256'] == entry['transparent_sha256'], (key, 'runtime copy')
        original = Image.open(ROOT / entry['source_path']).convert('RGB')
        transparent = Image.open(ROOT / entry['transparent_path'])
        assert transparent.mode == 'RGBA' and transparent.size == original.size == tuple(entry['dimensions']), key
        assert ImageChops.difference(original, transparent.convert('RGB')).getbbox() is None, (key, 'RGB changed')
        alpha = transparent.getchannel('A')
        assert alpha.getextrema() == tuple(entry['alpha_extrema']) and alpha.getextrema()[0] == 0 and alpha.getextrema()[1] >= 254, (key, 'alpha')
        x0, y0, x1, y1 = alpha.getbbox()
        assert [x0, y0, x1, y1] == entry['alpha_bounds_top_left'], key
        frame = runtime_records[key]
        assert frame == entry['framing'], (key, 'runtime metadata drift')
        crop = frame['sourceRect']
        assert crop == {'x': x0 - 2, 'y': transparent.height - y1 - 2, 'width': x1 - x0 + 4, 'height': y1 - y0 + 4}, (key, 'crop')
        assert frame['width'] == crop['width'] and frame['height'] == crop['height'], (key, 'extent')
        boots = alpha.crop((0, y1 - 64, transparent.width, y1)).point(lambda p: 255 if p >= 128 else 0).getbbox()
        assert frame['feet'] == {'x': (boots[0] + boots[2]) / 2 - crop['x'], 'y': 2}, (key, 'feet')
        print('PASS', key, 'original/hash/RGB/alpha/runtime/framing')
    assert manifest['credits_spent'] == sum(entry['cost'] for entry in manifest['characters']) == 8
    print('PASS all eight appearances; 8 removal credits; no generated characters')


if __name__ == '__main__':
    verify()
