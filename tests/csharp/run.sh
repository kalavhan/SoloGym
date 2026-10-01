#!/usr/bin/env bash
# Pure-logic C# tests for the training engine, journal/dungeon and account layer, compiled with
# Roslyn under .NET (PowerShell 7 bundles both) against a tiny UnityEngine shim. No Unity needed.
#   PWSH=/path/to/pwsh tests/csharp/run.sh
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; export SOLOGYM_ROOT="$(cd "$HERE/../.." && pwd)"
PWSH="${PWSH:-pwsh}"; S="$SOLOGYM_ROOT/app/Assets/SoloGym/Scripts"
export SOLOGYM_PARITY_JSON="${TMPDIR:-/tmp}/sologym-python-sessions.json"
python3 "$HERE/generate_parity.py" "$SOLOGYM_PARITY_JSON"
CORE="$HERE/UnityShim.cs,$S/TrainingState.cs,$S/Training/TrainingEngine.cs,$S/Training/TrainingPlans.cs,$S/Workouts/WorkoutJournal.cs,$S/Boss/BossSession.cs"
"$PWSH" -NoProfile -File "$HERE/run-tests.ps1" -SourceList "$CORE,$HERE/EngineParityTests.cs"
"$PWSH" -NoProfile -File "$HERE/run-tests.ps1" -SourceList "$CORE,$HERE/LiveJournalTests.cs" | tail -3
"$PWSH" -NoProfile -File "$HERE/run-tests.ps1" -SourceList "$CORE,$S/Account/UserProfile.cs,$S/Account/AccountSession.cs,$S/FirebaseWelcomeAuthService.cs,$S/WelcomeState.cs,$S/AccountRegistrationState.cs,$HERE/AccountTests.cs"
