#!/usr/bin/env python3
"""Difficulty fixtures and eligible substitutions from the existing training rules."""
import argparse, copy, hashlib, json
from pathlib import Path
from training_reference import load_data, generate_session, swap_exercise, estimate_block
from export_training_preview import render as preview
ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'app/Assets/SoloGym/Resources/Training/BossOptions.json'
def render():
    data = load_data()
    sources = {p['id']: p for p in json.loads((ROOT/'data/training/example-profiles.json').read_text())['profiles']}
    exercises = {e['id']: e for e in data['exercises']['exercises']}
    def enrich(b):
        more = dict(b, sets=b['sets']+1)
        b['additional_set_seconds'] = estimate_block(more, exercises[b['exercise_id']], data['rules']['timing']) - b['estimated_seconds']
        return b
    entries = []
    for original in json.loads(preview())['entries']:
        if original['session']['status'] != 'draft_ready': continue
        pid, minutes, ready, supervised = original['key'].split(':')
        for difficulty in ('light', 'medium', 'hard'):
            profile = copy.deepcopy(sources[pid])
            profile.update(session_minutes=int(minutes), readiness=ready, teen_supervision_available=supervised=='1', difficulty=difficulty)
            plan = generate_session(profile, '2026-09-28', 'foundation_a', data)
            options = []
            for block in plan['blocks']:
                if block['role'] != 'main': continue
                for exercise in data['exercises']['exercises']:
                    try: changed = swap_exercise(profile, plan, block['id'], exercise['id'], data)
                    except ValueError: continue
                    options.append(dict(block=next(b for b in changed['blocks'] if b['id']==block['id']), secondsDelta=changed['estimated_seconds']-plan['estimated_seconds']))
            for b in plan['blocks']: enrich(b)
            for option in options: enrich(option['block'])
            entries.append(dict(key=original['key']+':'+difficulty, plan=plan, options=options))
    return json.dumps(dict(version=1, context='review-fixtures-only', previewSha256=hashlib.sha256(preview().encode()).hexdigest(), entries=entries), indent=2, ensure_ascii=False)+'\n'
if __name__=='__main__':
    ap=argparse.ArgumentParser();ap.add_argument('--check',action='store_true');args=ap.parse_args();value=render()
    if args.check: assert OUTPUT.read_text()==value, 'Boss options are stale'
    else: OUTPUT.write_text(value)
    print(f"Boss options: {len(json.loads(value)['entries'])} contexts verified")
