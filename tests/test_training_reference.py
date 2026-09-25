"""Behavior checks for draft training data; these are not clinical validation."""
import copy
from datetime import date
from pathlib import Path
import sys
import unittest

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))
from tools import training_reference as training


class TrainingReferenceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data = training.load_data()
        cls.profiles = {
            p["id"]: p
            for p in training.read_json(training.DATA / "example-profiles.json")["profiles"]
        }

    def profile(self, fixture="adult_home_beginner", **updates):
        profile = copy.deepcopy(self.profiles[fixture])
        profile.update(updates)
        return profile

    def session(self, profile=None, template="foundation_a"):
        return training.generate_session(profile or self.profile(), "2026-09-28", template, self.data)

    @staticmethod
    def block(session, block_id):
        return next(b for b in session["blocks"] if b["id"] == block_id)

    @staticmethod
    def completion_logs(session, factor=1):
        return [
            {"event_id": f"{b['id']}-{index}", "block_id": b["id"], "set_index": index,
             "quantity": b["quantity_min"] * factor, "unit": b["unit"]}
            for b in session["blocks"] if b["role"] == "main"
            for index in range(b["sets"])
        ]

    def test_catalog_and_fictional_profiles_validate(self):
        counts = training.validate_data(self.data)
        self.assertGreaterEqual(counts["exercises"], 40)
        for profile in self.profiles.values():
            with self.subTest(profile=profile["id"]):
                training.validate_profile(profile, self.data)

    def test_minimum_age_and_teen_adult_boundary(self):
        with self.assertRaises(ValueError):
            training.generate_week(self.profile(age=14), data=self.data)
        for age, expected in ((15, "teen"), (17, "teen"), (18, "adult")):
            with self.subTest(age=age):
                plan = training.generate_week(self.profile(age=age), data=self.data)
                self.assertEqual(expected, plan["age_group"])

    def test_illness_pain_and_injury_produce_rest_without_boss(self):
        for readiness in ("ill", "pain", "injury"):
            for template in ("foundation_a", "aerobic_base", "mobility_reset"):
                with self.subTest(readiness=readiness, template=template):
                    session = self.session(self.profile(readiness=readiness), template)
                    self.assertEqual("recovery", session["status"])
                    self.assertFalse(session["blocks"])
                    self.assertIsNone(session["boss"])
                    self.assertTrue(session["consistency_protected_on_rest_or_stop"])

    def test_unsupervised_teens_receive_no_strength_prescription(self):
        plan = training.generate_week(self.profile("teen_without_supervision", sessions_per_week=5), data=self.data)
        for session in plan["sessions"]:
            self.assertFalse(any(b["category"] == "strength" for b in session["blocks"]))
            if session["status"] == "needs_review":
                self.assertIsNone(session["boss"])
                self.assertIn("supervision_needed", {m["code"] for m in session["messages"]})
        self.assertTrue(any(s["status"] == "needs_review" for s in plan["sessions"]))

    def test_existing_need_for_professional_guidance_requires_review(self):
        for template in ("foundation_a", "aerobic_base", "mobility_reset"):
            with self.subTest(template=template):
                session = self.session(self.profile(professional_guidance_needed=True), template)
                self.assertEqual("needs_review", session["status"])
                self.assertFalse(session["blocks"])
                self.assertIsNone(session["boss"])

    def test_teen_mobility_plan_is_honestly_labeled_as_mobility(self):
        plan = training.generate_week(self.profile("teen_home_supervised", goal="mobility"), data=self.data)
        self.assertEqual("mobility", plan["goal_effective"])
        working = [s for s in plan["sessions"] if s["status"] == "draft_ready"]
        self.assertTrue(working)
        self.assertTrue(all(s["kind"] == "mobility" for s in working))

    def test_equipment_is_explicit_and_unknown_equipment_is_rejected(self):
        with self.assertRaises(ValueError):
            self.session(self.profile(equipment=["imaginary_machine"]))
        profile = self.profile("adult_no_equipment")
        session = self.session(profile)
        catalog = {e["id"]: e for e in self.data["exercises"]["exercises"]}
        for block in session["blocks"]:
            self.assertTrue(set(catalog[block["exercise_id"]]["equipment_all"]) <= set(profile["equipment"]))

    def test_no_equipment_pull_gap_is_disclosed_without_fake_substitution(self):
        session = self.session(self.profile("adult_no_equipment"))
        self.assertIn("pull", session["coverage_gaps"])
        self.assertIn("pull_coverage_gap", {m["code"] for m in session["messages"]})
        self.assertFalse(any(b["id"] == "pull" for b in session["blocks"]))
        self.assertFalse(any(b["category"] == "mobility" and b["role"] == "main" for b in session["blocks"]))

    def test_repetitions_static_seconds_and_per_side_time_are_distinct(self):
        # One light set: 12 reps per side at 4 sec/rep + 30 sec setup + 30 sec transition.
        core = self.block(self.session(self.profile(difficulty="light")), "core")
        self.assertEqual("dead_bug", core["exercise_id"])
        self.assertEqual("reps", core["unit"])
        self.assertTrue(core["per_side"])
        self.assertEqual(156, core["estimated_seconds"])
        profile = self.profile(experience="intermediate", difficulty="light", preferred_exercises={"core": "side_plank"})
        core = self.block(self.session(profile), "core")
        self.assertEqual("seconds", core["unit"])
        self.assertEqual(20, core["quantity_max"])
        self.assertTrue(core["per_side"])
        self.assertEqual(100, core["estimated_seconds"])

    def test_valid_time_budgets_keep_structure_or_require_changes(self):
        with self.assertRaises(ValueError):
            self.session(self.profile(session_minutes=14))
        for minutes in (15, 20, 30, 60):
            for template in ("foundation_a", "aerobic_base", "mobility_reset"):
                with self.subTest(minutes=minutes, template=template):
                    session = self.session(self.profile(session_minutes=minutes), template)
                    if session["status"] == "draft_ready":
                        self.assertLessEqual(session["estimated_seconds"], minutes * 60)
                        self.assertEqual("warmup", session["blocks"][0]["role"])
                        self.assertEqual("cooldown", session["blocks"][-1]["role"])
                        self.assertGreaterEqual(session["blocks"][0]["quantity_min"], 3)
                        self.assertGreaterEqual(session["blocks"][-1]["quantity_min"], 2)
                        for block in session["blocks"]:
                            if block["role"] == "main" and block["category"] == "strength":
                                self.assertGreaterEqual(block["rest_seconds"], 90)
                    else:
                        self.assertIsNone(session["boss"])

    def test_hard_does_not_assign_load_or_multiply_rewards_and_is_gated(self):
        medium = self.session(self.profile(experience="intermediate", session_minutes=60))
        hard = self.session(self.profile(experience="intermediate", session_minutes=60, difficulty="hard"))
        self.assertEqual("hard", hard["difficulty_effective"])
        self.assertEqual(medium["boss"]["reward_budget"], hard["boss"]["reward_budget"])
        self.assertTrue(all(b["load_kg"] is None for b in hard["blocks"]))
        for profile in (self.profile(difficulty="hard"), self.profile("teen_home_supervised", difficulty="hard")):
            with self.subTest(age=profile["age"]):
                self.assertEqual("medium", self.session(profile)["difficulty_effective"])

    def test_low_energy_reduces_hard_to_light_without_punishing_rewards(self):
        profile = self.profile(experience="intermediate", difficulty="hard", session_minutes=60)
        ready = self.session(profile)
        tired = self.session(dict(profile, readiness="low_energy"))
        self.assertEqual("light", tired["difficulty_effective"])
        self.assertEqual(ready["boss"]["reward_budget"], tired["boss"]["reward_budget"])
        self.assertLess(sum(b["sets"] for b in tired["blocks"]), sum(b["sets"] for b in ready["blocks"]))

    def test_repeat_generation_is_deterministic_and_preserves_inputs(self):
        profile = self.profile("adult_gym_intermediate")
        original = copy.deepcopy(profile)
        first = training.generate_week(profile, data=self.data)
        second = training.generate_week(profile, data=self.data)
        self.assertEqual(first, second)
        self.assertEqual(original, profile)

    def test_weekly_strength_dates_are_never_adjacent(self):
        for age in (17, 28):
            for goal in ("general_fitness", "strength", "muscle_growth", "endurance"):
                for frequency in (2, 3, 4, 5):
                    with self.subTest(age=age, goal=goal, frequency=frequency):
                        profile = self.profile("teen_home_supervised", age=age, goal=goal, sessions_per_week=frequency)
                        plan = training.generate_week(profile, data=self.data)
                        strength_dates = [date.fromisoformat(s["date"]) for s in plan["sessions"]
                                          if s["kind"] == "strength" and s["status"] == "draft_ready"]
                        for before, after in zip(strength_dates, strength_dates[1:]):
                            self.assertGreaterEqual((after - before).days, 2)

    def test_previous_sunday_blocks_monday_strength_and_future_history_is_rejected(self):
        profile = self.profile(recent_strength_dates=["2026-09-27"])
        monday = training.generate_week(profile, data=self.data)["sessions"][0]
        self.assertEqual("recovery", monday["status"])
        self.assertFalse(monday["blocks"])
        self.assertIn("recent_strength", {m["code"] for m in monday["messages"]})
        with self.assertRaises(ValueError):
            training.generate_week(self.profile(recent_strength_dates=["2026-09-28"]), data=self.data)

    def test_compatible_user_swap_preserves_original_and_plan_constraints(self):
        profile = self.profile()
        original = self.session(profile)
        before = copy.deepcopy(original)
        edited = training.swap_exercise(profile, original, "squat", "bodyweight_squat", self.data)
        self.assertEqual("bodyweight_squat", self.block(edited, "squat")["exercise_id"])
        self.assertEqual(before, original)
        self.assertEqual(original["boss"], edited["boss"])
        self.assertLessEqual(edited["estimated_seconds"], edited["budget_seconds"])

    def test_incompatible_unavailable_and_unsegmented_interval_edits_are_rejected(self):
        profile = self.profile(experience="intermediate")
        session = self.session(profile)
        for block_id, replacement in (("squat", "dead_bug"), ("squat", "dumbbell_goblet_squat"), ("core", "forearm_plank")):
            with self.subTest(replacement=replacement), self.assertRaises(ValueError):
                training.swap_exercise(profile, session, block_id, replacement, self.data)
        profile = self.profile(experience="intermediate", environment="gym")
        cardio = self.session(profile, "aerobic_base")
        with self.assertRaisesRegex(ValueError, "[Ii]nterval"):
            training.swap_exercise(profile, cardio, "cardio", "walk_jog_intervals", self.data)
        profile["preferred_exercises"] = {"cardio": "walk_jog_intervals"}
        with self.assertRaisesRegex(ValueError, "[Ii]nterval"):
            self.session(profile, "aerobic_base")

    def test_edit_after_new_pain_does_not_leave_an_active_strength_prescription(self):
        profile = self.profile()
        session = self.session(profile)
        for readiness in ("ill", "pain", "injury"):
            with self.subTest(readiness=readiness), self.assertRaises(ValueError):
                training.swap_exercise(dict(profile, readiness=readiness), session, "squat", "bodyweight_squat", self.data)

    def test_edit_observes_a_new_shorter_time_budget(self):
        profile = self.profile(session_minutes=60)
        session = self.session(profile)
        self.assertGreater(session["estimated_seconds"], 15 * 60)
        with self.assertRaises(ValueError):
            training.swap_exercise(dict(profile, session_minutes=15), session, "squat", "bodyweight_squat", self.data)

    def test_boss_caps_duplicates_extra_repetitions_and_manual_corrections(self):
        session = self.session()
        logs = self.completion_logs(session)
        completed = training.boss_progress(session, logs)
        self.assertTrue(completed["completed"])
        self.assertEqual(session["boss"]["max_health"], completed["damage"])
        self.assertEqual(0, completed["health_remaining"])
        self.assertEqual(completed, training.boss_progress(session, logs + logs))
        self.assertEqual(completed, training.boss_progress(session, self.completion_logs(session, factor=100)))
        correction = dict(logs[0], event_id="correct_first_set", quantity=0)
        corrected = training.boss_progress(session, logs + [correction])
        self.assertFalse(corrected["completed"])
        self.assertLess(corrected["damage"], completed["damage"])
        restoration = dict(logs[0], event_id="restore_first_set")
        self.assertEqual(completed, training.boss_progress(session, logs + [correction, restoration]))

    def test_boss_rejects_unplanned_sets_invalid_quantities_and_wrong_units(self):
        session = self.session()
        first = self.completion_logs(session)[0]
        for override in ({"set_index": 99}, {"quantity": -1}, {"quantity": float("nan")},
                         {"quantity": float("inf")}, {"quantity": True}, {"unit": "minutes"},
                         {"block_id": "warmup"}, {"event_id": ""}):
            with self.subTest(override=override), self.assertRaises(ValueError):
                training.boss_progress(session, [dict(first, **override)])

    def test_progression_is_review_only_and_pain_prevents_an_increase(self):
        successful = {"session_id": "session_1", "exercise_id": "chair_squat", "pain_reported": False, "readiness": "ready",
                      "upper_target_completed": True, "manageable_effort": True}
        second = dict(successful, session_id="session_2")
        self.assertEqual("repeat_and_observe", training.progression_suggestion([successful], self.data))
        self.assertEqual("repeat_and_observe", training.progression_suggestion([successful, successful], self.data))
        self.assertEqual("review_one_small_change_with_user_or_supervisor",
                         training.progression_suggestion([successful, second], self.data))
        self.assertEqual("review_or_reduce", training.progression_suggestion(
            [successful, dict(second, pain_reported=True)], self.data))
        self.assertEqual("repeat_and_observe", training.progression_suggestion(
            [successful, dict(second, exercise_id="wall_pushup")], self.data))
        self.assertNotIn("load_kg", successful)


if __name__ == "__main__":
    unittest.main()
