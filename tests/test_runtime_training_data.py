"""The app bundles a runtime copy of the training content; it must not drift from data/training."""
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "app/Assets/SoloGym/Resources/Training/Rules"


class RuntimeTrainingDataTest(unittest.TestCase):
    def test_runtime_copies_match_source(self):
        for name in ("exercises", "templates", "rules"):
            with self.subTest(name=name):
                source = json.loads((ROOT / "data/training" / f"{name}.json").read_text(encoding="utf-8"))
                runtime = json.loads((RUNTIME / f"{name}.json").read_text(encoding="utf-8"))
                self.assertEqual(source, runtime, f"Copy data/training/{name}.json to {RUNTIME.relative_to(ROOT)}")

    def test_runtime_assets_have_unity_meta(self):
        for name in ("exercises", "templates", "rules"):
            self.assertTrue((RUNTIME / f"{name}.json.meta").exists())


if __name__ == "__main__":
    unittest.main()
