#!/usr/bin/env python3
"""Write Python reference sessions for the C# TrainingEngine parity test."""
import itertools, json, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools"))
import training_reference as tr  # noqa: E402

data = tr.load_data()
templates = [t["id"] for t in data["templates"]["templates"]]
equip = {"none": [], "home": ["wall", "chair", "stable_surface"],
         "homeplus": ["wall", "chair", "stable_surface", "dumbbells", "resistance_band", "band_anchor", "stationary_bike"],
         "gym": ["wall", "stable_surface", "dumbbells", "cable_machine", "lat_pulldown_machine", "leg_press_machine", "stationary_bike", "bench", "chest_press_machine"]}
cases = []
for age, goal, exp, (env, eq), mins, diff, ready, sup in itertools.product(
        [16, 30], ["general_fitness", "strength", "muscle_growth", "endurance", "mobility"], ["beginner", "intermediate"],
        [("home", "none"), ("home", "home"), ("home", "homeplus"), ("gym", "gym"), ("outdoor", "none")],
        [15, 25, 40, 60], ["light", "medium", "hard"], ["ready", "low_energy", "pain"], [False, True]):
    if age >= 18 and sup:
        continue
    p = {"id": "t", "age": age, "experience": exp, "goal": goal, "environment": env, "equipment": equip[eq],
         "session_minutes": mins, "sessions_per_week": 3, "difficulty": diff, "readiness": ready,
         "teen_supervision_available": sup, "excluded_exercises": [], "preferred_exercises": {},
         "recent_strength_dates": [], "professional_guidance_needed": False}
    for t in templates:
        cases.append({"profile": p, "template": t, "session": tr.generate_session(p, "2026-09-28", t, data, [])})
Path(sys.argv[1]).write_text(json.dumps(cases))
print(f"{len(cases)} reference sessions")
