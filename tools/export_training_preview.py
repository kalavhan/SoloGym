#!/usr/bin/env python3
"""Reproduce Unity review fixtures using the existing offline training rules."""
import argparse
import copy
import hashlib
import json
from pathlib import Path
from training_reference import load_data, generate_session, validate_data

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / 'app/Assets/SoloGym/Resources/Training/Preview.json'


def render():
    data = load_data()
    validate_data(data)
    profiles = json.loads((ROOT / 'data/training/example-profiles.json').read_text())['profiles']
    names = {
        'adult_home_beginner': ('Adult · Home · Beginner', 'Adulto · Casa · Principiante'),
        'adult_gym_intermediate': ('Adult · Gym · Intermediate', 'Adulto · Gimnasio · Intermedio'),
        'teen_home_supervised': ('Teen · Home · Beginner', 'Adolescente · Casa · Principiante'),
    }
    entries, selected = [], []
    for original in profiles:
        if original['id'] not in names:
            continue
        profile = copy.deepcopy(original)
        en, es = names[profile['id']]
        selected.append(dict(id=profile['id'], name=dict(en=en, es=es), teen=profile['age'] < 18,
                             equipment=[e['name'] for e in data['equipment']['equipment'] if e['id'] in profile['equipment']]))
        for minutes in (15, 25, 40):
            for readiness in ('ready', 'low_energy', 'ill', 'pain', 'injury'):
                for supervised in ((False, True) if profile['age'] < 18 else (False,)):
                    profile.update(session_minutes=minutes, readiness=readiness, teen_supervision_available=supervised)
                    session = generate_session(profile, '2026-09-28', 'foundation_a', data)
                    key = f"{profile['id']}:{minutes}:{readiness}:{int(supervised)}"
                    entries.append(dict(key=key, session=session))
    inputs = [ROOT / 'tools/training_reference.py', ROOT / 'data/training/example-profiles.json']
    inputs += [ROOT / f'data/training/{name}.json' for name in data]
    return json.dumps(dict(version=1, content_status='draft_pending_professional_and_youth_review',
        source_hashes=[dict(path=str(p.relative_to(ROOT)), sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in inputs],
        profiles=selected, entries=entries), ensure_ascii=False, indent=2) + '\n'


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    value = render()
    if args.check:
        assert OUTPUT.read_text() == value, 'Training preview is stale; run tools/export_training_preview.py'
    else:
        OUTPUT.parent.mkdir(parents=True, exist_ok=True)
        OUTPUT.write_text(value)
    print('Training preview: 60 deterministic fixtures verified' if args.check else 'Exported 60 deterministic training fixtures')
