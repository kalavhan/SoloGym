#!/usr/bin/env python3
"""Export eligible edits through the existing reference rules; no new prescriptions."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
from training_reference import load_data, generate_session, swap_exercise
from export_training_preview import render as preview

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'app/Assets/SoloGym/Resources/Training/JournalOptions.json'


def render():
    data = load_data()
    catalog = json.loads(preview())
    originals = {p['id']: p for p in json.loads((ROOT / 'data/training/example-profiles.json').read_text())['profiles']}
    options = []
    for entry in catalog['entries']:
        if entry['session']['status'] != 'draft_ready':
            continue
        pid, minutes, readiness, supervision = entry['key'].split(':')
        profile = copy.deepcopy(originals[pid])
        profile.update(session_minutes=int(minutes), readiness=readiness, teen_supervision_available=supervision == '1')
        for block in entry['session']['blocks']:
            if block['role'] != 'main':
                continue
            for exercise in data['exercises']['exercises']:
                try:
                    changed = swap_exercise(profile, entry['session'], block['id'], exercise['id'], data)
                except ValueError:
                    continue
                replacement = next(b for b in changed['blocks'] if b['id'] == block['id'])
                options.append(dict(key=entry['key'], block=replacement,
                                    secondsDelta=changed['estimated_seconds'] - entry['session']['estimated_seconds']))
    mobility = []
    for p in catalog['profiles']:
        profile = copy.deepcopy(originals[p['id']])
        profile.update(session_minutes=15, readiness='ready', goal='mobility', teen_supervision_available=p['teen'])
        mobility.append(dict(profileId=p['id'], plan=generate_session(profile, '2026-09-28', 'mobility_reset', data)))
    return json.dumps(dict(version=1, context='review-fixtures-only',
        previewSha256=hashlib.sha256(preview().encode()).hexdigest(), options=options, mobility=mobility), indent=2, ensure_ascii=False) + '\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    value = render()
    if args.check:
        assert OUTPUT.read_text() == value, 'Journal edit options are stale'
    else:
        OUTPUT.write_text(value)
    print(f"Journal options: {len(json.loads(value)['options'])} eligible substitutions verified")
