#!/usr/bin/env python3
"""Offline, draft workout-data reference. This is not the mobile app or a clinical engine."""
from __future__ import annotations

import argparse
import copy
import hashlib
import json
import math
from datetime import date, timedelta
from pathlib import Path

from jsonschema import Draft202012Validator, FormatChecker

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "data" / "training"


def read_json(path):
    return json.loads(Path(path).read_text(encoding="utf-8"))


def load_data():
    return {name: read_json(DATA / f"{name}.json") for name in
            ("exercises", "equipment", "templates", "rules", "sources")}


def validate_schema(value, filename):
    schema = read_json(ROOT / "data" / "schemas" / filename)
    errors = sorted(Draft202012Validator(schema, format_checker=FormatChecker())
                    .iter_errors(value), key=lambda e: str(list(e.path)))
    if errors:
        raise ValueError("; ".join(f"{list(e.path)}: {e.message}" for e in errors))


def validate_data(data=None):
    data = data or load_data()
    validate_schema(data["exercises"], "exercise-catalog.schema.json")
    exercises = data["exercises"]["exercises"]
    catalog = {e["id"]: e for e in exercises}
    if len(catalog) != len(exercises):
        raise ValueError("Duplicate exercise IDs")
    source_ids = {s["id"] for s in data["sources"]["sources"]}
    equipment_ids = {e["id"] for e in data["equipment"]["equipment"]}
    for e in exercises:
        if not set(e["source_ids"]) <= source_ids:
            raise ValueError(f"Unknown source: {e['id']}")
        if not set(e["equipment_all"]) <= equipment_ids:
            raise ValueError(f"Unknown equipment: {e['id']}")
        if not set(e["regressions"] + e["progressions"]) <= set(catalog):
            raise ValueError(f"Unknown variation: {e['id']}")
    templates = data["templates"]["templates"]
    if len({t["id"] for t in templates}) != len(templates):
        raise ValueError("Duplicate template IDs")
    for template in templates:
        if len({s["id"] for s in template["slots"]}) != len(template["slots"]):
            raise ValueError("Duplicate template slot IDs")
        for slot in template["slots"]:
            for exercise_id in slot["exercise_ids"]:
                e = catalog[exercise_id]
                if e["category"] != slot["category"] or e["movement_pattern"] not in slot["patterns"]:
                    raise ValueError(f"Wrong slot: {template['id']}/{exercise_id}")
    for role in ("warmup_exercise_ids", "cooldown_exercise_ids"):
        if not set(data["templates"][role]) <= set(catalog):
            raise ValueError(f"Unknown exercise in {role}")
    return {"exercises": len(exercises), "templates": len(templates),
            "equipment": len(equipment_ids), "sources": len(source_ids)}


def validate_profile(profile, data):
    validate_schema(profile, "training-profile.schema.json")
    catalog_ids = {e["id"] for e in data["exercises"]["exercises"]}
    equipment_ids = {e["id"] for e in data["equipment"]["equipment"]}
    if not set(profile["equipment"]) <= equipment_ids:
        raise ValueError("Unrecognized equipment")
    if not set(profile["excluded_exercises"]) <= catalog_ids:
        raise ValueError("Unrecognized excluded exercise")
    slots = {s["id"] for t in data["templates"]["templates"] for s in t["slots"]}
    if not set(profile["preferred_exercises"]) <= slots:
        raise ValueError("Unrecognized preferred slot")
    if not set(profile["preferred_exercises"].values()) <= catalog_ids:
        raise ValueError("Unrecognized preferred exercise")


def age_group(profile):
    return "teen" if profile["age"] < 18 else "adult"


def profile_signature(profile):
    return hashlib.sha256(json.dumps(profile, sort_keys=True).encode()).hexdigest()[:16]


def effective_goal(profile):
    return "general_fitness" if age_group(profile) == "teen" and profile["goal"] != "mobility" else profile["goal"]


def eligible(exercise, profile):
    return (age_group(profile) in exercise["age_groups"]
            and profile["environment"] in exercise["environments"]
            and set(exercise["equipment_all"]) <= set(profile["equipment"])
            and exercise["id"] not in profile["excluded_exercises"]
            and not (profile["experience"] == "beginner" and exercise["min_experience"] == "intermediate")
            and not (age_group(profile) == "teen" and exercise["supervision_required_for_teens"]
                     and not profile["teen_supervision_available"]))


def message(code, en, es):
    return {"code": code, "text": {"en": en, "es": es}}


def estimate_block(block, exercise, timing):
    quantity = block["quantity_max"]
    work = quantity * {"reps": timing["seconds_per_rep_budget"], "seconds": 1, "minutes": 60}[block["unit"]]
    # Quantities for unilateral movements are PER SIDE, not a total across sides.
    work *= 2 if exercise["unilateral"] else 1
    return (math.ceil(work * block["sets"]) + (block["sets"] - 1) * block["rest_seconds"]
            + exercise["setup_seconds"] + (timing["transition_seconds"] if block["role"] == "main" else 0))


def make_block(exercise, slot, role, sets, quantity_min, quantity_max, rest, timing, effort=None):
    block = {"id": slot["id"], "role": role, "exercise_id": exercise["id"],
             "name": exercise["name"], "category": exercise["category"],
             "allowed_patterns": slot["patterns"], "unit": exercise["prescription_unit"],
             "sets": sets, "quantity_min": quantity_min, "quantity_max": quantity_max,
             "per_side": exercise["unilateral"], "rest_seconds": rest,
             "load_kg": None, "load_entry": "user_selected_if_applicable",
             "aerobic_effort_0_to_10": effort, "boss_share": 0}
    block["estimated_seconds"] = estimate_block(block, exercise, timing)
    return block


def assign_boss_shares(session, rewards):
    main = [b for b in session["blocks"] if b["role"] == "main"]
    if session["status"] != "draft_ready" or not main:
        session["boss"] = None
        return
    # Every planned exercise has equal share; no advantage from choosing heavier loads.
    per, remainder = divmod(1000, len(main))
    for i, block in enumerate(main):
        block["boss_share"] = per + (1 if i < remainder else 0)
    session["boss"] = {"max_health": 1000, "mapping": "manual_planned_set_completion",
                       "reward_budget": {"fitness_xp": rewards["fitness_xp_per_completed_session"], "coins": rewards["coins_per_completed_session"]},
                       "rewards_are_placeholders": True}


def generate_session(profile, on_date, template_id, data=None, recent_strength_dates=None):
    data = data or load_data()
    validate_profile(profile, data)
    day = date.fromisoformat(on_date)
    rules = data["rules"]
    catalog = {e["id"]: e for e in data["exercises"]["exercises"]}
    template = next((t for t in data["templates"]["templates"] if t["id"] == template_id), None)
    if template is None:
        raise ValueError("Unknown template")
    group = age_group(profile)
    selected_goal = effective_goal(profile)
    difficulty = profile["difficulty"]
    result = {"date": on_date, "template_id": template_id, "name": template["name"], "profile_signature": profile_signature(profile),
              "kind": template["kind"], "status": "draft_ready", "difficulty_requested": difficulty,
              "difficulty_effective": difficulty, "readiness_recheck_required": True,
              "budget_seconds": profile["session_minutes"] * 60, "estimated_seconds": 0,
              "blocks": [], "coverage_gaps": [], "messages": [], "boss": None,
              "consistency_protected_on_rest_or_stop": True}
    if profile["professional_guidance_needed"]:
        result["status"] = "needs_review"
        result["messages"].append(message("individual_guidance_needed", "Use individualized professional guidance before this routine. No workout has been prescribed.", "Busca orientación profesional individual antes de seguir esta rutina. No se ha indicado un entrenamiento."))
        return result
    if profile["readiness"] in rules["stop_readiness"]:
        result.update(status="recovery", kind="recovery")
        result["messages"].append(message("stop_for_readiness", "Rest or stop; do not train through illness, pain or injury. Seek appropriate advice before resuming.", "Descansa o detente; no entrenes con enfermedad, dolor o lesión. Busca orientación adecuada antes de volver."))
        return result
    if template["kind"] == "strength" and group == "teen" and not profile["teen_supervision_available"]:
        result["status"] = "needs_review"
        result["messages"].append(message("supervision_needed", "Arrange appropriate supervision for this strength session. No workout has been prescribed.", "Organiza supervisión adecuada para esta sesión de fuerza. No se ha indicado un entrenamiento."))
        return result
    recent = recent_strength_dates if recent_strength_dates is not None else profile["recent_strength_dates"]
    if template["kind"] == "strength" and any(0 <= (day - date.fromisoformat(d)).days < rules["recovery"]["strength_calendar_day_gap"] for d in recent):
        result.update(status="recovery", kind="recovery")
        result["messages"].append(message("recent_strength", "Keep this as recovery after recent strength work; do not make up missed volume.", "Mantén la recuperación tras el trabajo de fuerza reciente; no acumules el volumen pendiente."))
        return result
    if profile["readiness"] == "low_energy":
        difficulty = "light"
    elif difficulty == "hard" and (group == "teen" or profile["experience"] == "beginner"):
        difficulty = "medium"
    result["difficulty_effective"] = difficulty
    if difficulty != profile["difficulty"]:
        result["messages"].append(message("difficulty_adjusted", "Difficulty was reduced for readiness or training experience; reward value is unchanged.", "Se redujo la dificultad según tu preparación o experiencia; la recompensa no cambia."))
    timing = rules["timing"]
    for role, key, minutes in (("warmup", "warmup_exercise_ids", timing["warmup_minutes"]),
                               ("cooldown", "cooldown_exercise_ids", timing["cooldown_minutes"])):
        e = next((catalog[i] for i in data["templates"][key] if eligible(catalog[i], profile)), None)
        if e is None:
            result["status"] = "needs_changes"
            result["coverage_gaps"].append(role)
        else:
            block = make_block(e, {"id": role, "patterns": ["cardio"]}, role, 1, minutes, minutes, 0, timing, [2, 3])
            result["blocks"].append(block)
    main_blocks = []
    for slot in template["slots"]:
        ids = list(slot["exercise_ids"])
        preferred = profile["preferred_exercises"].get(slot["id"])
        if preferred:
            e = catalog[preferred]
            if e["category"] == slot["category"] and e["movement_pattern"] in slot["patterns"] and eligible(e, profile):
                # Intervals need segment timing and cannot enter through a preference override.
                if preferred == "walk_jog_intervals":
                    raise ValueError("Interval exercise requires a segmented prescription")
                ids = [preferred] + [i for i in ids if i != preferred]
            else:
                raise ValueError(f"Preferred exercise is incompatible with slot {slot['id']}")
        e = next((catalog[i] for i in ids if eligible(catalog[i], profile)), None)
        if e is None:
            result["coverage_gaps"].append(slot["id"])
            if not slot["optional"]:
                result["status"] = "needs_changes"
            continue
        if template["kind"] == "strength":
            dose = rules["strength_doses"][selected_goal]
            sets = dose[f"{profile['experience']}_sets"]
            if difficulty == "light":
                sets += rules["difficulty"]["light"]["strength_sets_delta"]
            elif difficulty == "hard" and not main_blocks:
                sets += rules["difficulty"]["hard"]["extra_sets_first_strength_slot"]
            sets = max(1, min(sets, rules["age_specific"][group]["max_sets"], 2 if profile["experience"] == "beginner" else 3))
            lo, hi = dose["reps_min"], dose["reps_max"]
            if e["prescription_unit"] == "seconds":
                lo = hi = timing["timed_core_seconds"]
            rest = dose["rest_seconds"]
            effort = None
        elif template["kind"] == "cardio":
            sets, rest = 1, 0
            minutes = rules["cardio"][f"{profile['experience']}_minutes"]
            minutes += rules["difficulty"][difficulty]["cardio_minutes_delta"]
            lo = hi = min(rules["cardio"]["maximum_minutes"], max(rules["cardio"]["minimum_minutes"], minutes))
            effort = rules["cardio"]["aerobic_effort_light" if difficulty == "light" else "aerobic_effort_medium"]
        else:
            sets, rest, effort = 1, 0, None
            lo = hi = 6 if e["prescription_unit"] == "reps" else timing["mobility_seconds"]
        main_blocks.append(make_block(e, slot, "main", sets, lo, hi, rest, timing, effort))
    # Place cool-down after the working blocks, regardless of the order above.
    result["blocks"] = ([b for b in result["blocks"] if b["role"] == "warmup"] + main_blocks
                        + [b for b in result["blocks"] if b["role"] == "cooldown"])
    if "pull" in result["coverage_gaps"]:
        result["messages"].append(message("pull_coverage_gap", "This setup lacks an eligible resisted pull. Add suitable equipment or review the plan; mobility is not a substitute.", "Este equipo no permite un tirón con resistencia adecuado. Añade equipo apropiado o revisa el plan; la movilidad no lo sustituye."))
    def total():
        return sum(b["estimated_seconds"] for b in result["blocks"])
    reduced = False
    while total() > result["budget_seconds"]:
        reducible = [b for b in main_blocks if b["sets"] > 1]
        if reducible:
            b = max(reducible, key=lambda x: (x["sets"], x["estimated_seconds"]))
            b["sets"] -= 1
        elif template["kind"] == "cardio" and main_blocks and main_blocks[0]["quantity_max"] > rules["cardio"]["minimum_minutes"]:
            b = main_blocks[0]
            b["quantity_min"] -= 1
            b["quantity_max"] -= 1
        else:
            result["status"] = "needs_changes"
            result["messages"].append(message("insufficient_time", "Choose more time or a different template; warm-up, rest and recovery have not been compressed.", "Elige más tiempo u otra plantilla; no se han reducido el calentamiento, las pausas ni la recuperación."))
            break
        reduced = True
        b["estimated_seconds"] = estimate_block(b, catalog[b["exercise_id"]], timing)
    if reduced:
        result["messages"].append(message("time_adjustment", "Work was reduced to fit the time budget while retaining rest and session structure.", "Se redujo el trabajo para respetar el tiempo disponible y conservar las pausas y la estructura."))
    result["estimated_seconds"] = total()
    if not main_blocks:
        result["status"] = "needs_changes"
    assign_boss_shares(result, rules["rewards"])
    return result


def generate_week(profile, week_start="2026-09-28", data=None):
    data = data or load_data()
    validate_profile(profile, data)
    start = date.fromisoformat(week_start)
    if start.weekday() != 0:
        raise ValueError("week_start must be a Monday")
    if any(date.fromisoformat(d) >= start for d in profile["recent_strength_dates"]):
        raise ValueError("Recent strength dates must precede the draft week")
    days = {2: [0, 3], 3: [0, 2, 4], 4: [0, 1, 3, 5], 5: [0, 1, 2, 3, 5]}[profile["sessions_per_week"]]
    strength_focus = age_group(profile) == "adult" and profile["goal"] in ("strength", "muscle_growth")
    if profile["goal"] == "mobility":
        strength_days = set()
    elif strength_focus and profile["sessions_per_week"] in (3, 5):
        strength_days = {0, 2, 4} if profile["sessions_per_week"] == 3 else {0, 2, 5}
    else:
        strength_days = {days[0], days[-1] if len(days) == 3 else days[1] if len(days) == 2 else days[2] if len(days) == 4 else days[3]}
    sessions, recent, strength_index = [], list(profile["recent_strength_dates"]), 0
    for offset in range(7):
        current = (start + timedelta(days=offset)).isoformat()
        if offset not in days:
            sessions.append({"date": current, "kind": "recovery", "status": "recovery", "blocks": [],
                             "estimated_seconds": 0, "boss": None, "consistency_protected_on_rest_or_stop": True,
                             "messages": [message("scheduled_recovery", "Rest day. Ordinary enjoyable movement is optional; there is no workout debt.", "Día de descanso. El movimiento cotidiano agradable es opcional; no debes recuperar entrenamientos.")]})
            continue
        if profile["goal"] == "mobility":
            template_id = "mobility_reset"
        elif offset in strength_days:
            if profile["environment"] == "gym" and "leg_press_machine" in profile["equipment"]:
                template_id = "machine_foundation"
            else:
                template_id = "foundation_a" if strength_index % 2 == 0 else "foundation_b"
            strength_index += 1
        else:
            template_id = "mobility_reset" if len(days) == 5 and offset == 2 else "aerobic_base"
        session = generate_session(profile, current, template_id, data, recent)
        sessions.append(session)
        if session["kind"] == "strength" and session["status"] == "draft_ready":
            recent.append(current)
    digest = hashlib.sha256(json.dumps({"profile": profile, "week": week_start, "data": data}, sort_keys=True).encode()).hexdigest()[:16]
    return {"schema_version": "1.0.0", "content_status": "draft_requires_professional_review", "plan_id": digest,
            "profile_id": profile["id"], "week_start": week_start, "age_group": age_group(profile),
            "goal_requested": profile["goal"], "goal_effective": effective_goal(profile),
            "sessions": sessions, "limitations": [
                "Reference weekly template, not a complete measure of WHO activity targets or individual recovery.",
                "Current readiness is applied to this draft; check readiness again on each actual training day.",
                "Manual logging only. No rep detection, load inference, diagnosis, or verified technique.",
                "Advanced, sport-specific and clinical programming need separate content and review.",
                "Bodyweight plans can have a declared pulling gap; generic mobility is not counted as a pull."]}


def swap_exercise(profile, session, block_id, replacement_id, data=None):
    """Validate a user's edit; never silently discard equipment, dose or time constraints."""
    data = data or load_data()
    validate_profile(profile, data)
    if session.get("profile_signature") != profile_signature(profile):
        raise ValueError("Profile or readiness changed; regenerate before editing")
    if session["status"] != "draft_ready":
        raise ValueError("Only a ready draft can be edited; resolve review/change messages first")
    output = copy.deepcopy(session)
    b = next((b for b in output["blocks"] if b["id"] == block_id and b["role"] == "main"), None)
    e = next((e for e in data["exercises"]["exercises"] if e["id"] == replacement_id), None)
    if b is None or e is None:
        raise ValueError("Unknown block or replacement")
    if not eligible(e, profile) or e["category"] != b["category"] or e["movement_pattern"] not in b["allowed_patterns"] or e["prescription_unit"] != b["unit"]:
        raise ValueError("Replacement requires compatible movement, unit, experience, supervision and equipment")
    if replacement_id == "walk_jog_intervals":
        raise ValueError("Intervals require a segmented prescription")
    if any(other["exercise_id"] == replacement_id for other in output["blocks"] if other["id"] != block_id):
        raise ValueError("Replacement duplicates another exercise")
    b.update(exercise_id=e["id"], name=e["name"], per_side=e["unilateral"])
    b["estimated_seconds"] = estimate_block(b, e, data["rules"]["timing"])
    output["estimated_seconds"] = sum(x["estimated_seconds"] for x in output["blocks"])
    if output["estimated_seconds"] > output["budget_seconds"]:
        raise ValueError("Replacement exceeds time budget; explicitly revise the routine first")
    return output


def boss_progress(session, logs):
    """Pure preview; a production reward ledger and server checks are separate work."""
    if session.get("boss") is None:
        return {"damage": 0, "health_remaining": 0, "completed": False}
    blocks = {b["id"]: b for b in session["blocks"] if b["role"] == "main"}
    latest, seen = {}, set()
    for entry in logs:
        if not isinstance(entry.get("event_id"), str) or not entry["event_id"]:
            raise ValueError("Log event needs an id")
        if entry["event_id"] in seen:
            continue
        seen.add(entry["event_id"])
        block = blocks.get(entry.get("block_id"))
        index, quantity = entry.get("set_index"), entry.get("quantity")
        if block is None or type(index) is not int or not 0 <= index < block["sets"]:
            raise ValueError("Log must refer to a prescribed set")
        if type(quantity) not in (int, float) or not math.isfinite(quantity) or quantity < 0:
            raise ValueError("Quantity must be finite and nonnegative")
        if entry.get("unit") != block["unit"]:
            raise ValueError("Log unit does not match the planned set")
        # For unilateral sets, log the lower completed quantity across the two sides.
        latest[(block["id"], index)] = min(1, quantity / block["quantity_min"])
    damage = math.floor(sum(b["boss_share"] * sum(latest.get((b["id"], i), 0) for i in range(b["sets"])) / b["sets"] for b in blocks.values()) + 1e-9)
    damage = min(session["boss"]["max_health"], damage)
    return {"damage": damage, "health_remaining": session["boss"]["max_health"] - damage,
            "completed": damage == session["boss"]["max_health"]}


def progression_suggestion(history, data=None):
    """Offer review, never prescribe an automatic weight increase."""
    data = data or load_data()
    required = data["rules"]["progression"]["successful_exposures_required"]
    if len(history) < required:
        return "repeat_and_observe"
    distinct = {}
    for entry in history:
        if isinstance(entry.get("session_id"), str) and entry["session_id"]:
            distinct[entry["session_id"]] = entry
    if len(distinct) < required:
        return "repeat_and_observe"
    recent = list(distinct.values())[-required:]
    if any(x.get("pain_reported") is not False or x.get("readiness") != "ready" for x in recent):
        return "review_or_reduce"
    if len({x.get("exercise_id") for x in recent}) != 1 or not all(x.get("exercise_id") for x in recent):
        return "repeat_and_observe"
    if all(x.get("upper_target_completed") is True and x.get("manageable_effort") is True for x in recent):
        return "review_one_small_change_with_user_or_supervisor"
    return "repeat_and_observe"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["validate", "examples", "generate"])
    parser.add_argument("--profile", type=Path)
    parser.add_argument("--week-start", default="2026-09-28")
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    data = load_data()
    counts = validate_data(data)
    if args.command == "validate":
        profiles = read_json(DATA / "example-profiles.json")["profiles"]
        for p in profiles:
            validate_profile(p, data)
        print(json.dumps({"valid": True, **counts, "example_profiles": len(profiles)}))
    elif args.command == "examples":
        dest = args.output or DATA / "examples"
        dest.mkdir(parents=True, exist_ok=True)
        for p in read_json(DATA / "example-profiles.json")["profiles"]:
            (dest / f"{p['id']}.json").write_text(json.dumps(generate_week(p, args.week_start, data), ensure_ascii=False, indent=2) + "\n")
        print(f"Draft examples written to {dest}")
    else:
        if args.profile is None:
            parser.error("generate requires --profile")
        output = json.dumps(generate_week(read_json(args.profile), args.week_start, data), ensure_ascii=False, indent=2) + "\n"
        if args.output:
            args.output.parent.mkdir(parents=True, exist_ok=True)
            args.output.write_text(output)
        else:
            print(output, end="")


if __name__ == "__main__":
    main()
