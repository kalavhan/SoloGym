#!/usr/bin/env bash
# Build the SoloGym MVP test APK locally (connected fitness flow, no review-only sample routes).
# Requires Unity 6000.3.24f1 with Android Build Support, and the Firebase SDK + config installed
# (python3 tools/install_firebase_unity.py, docs/engineering/firebase-auth-setup.md).
# Close the Unity editor for this project first: batch mode cannot open a project that is already open.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
UNITY="${SOLOGYM_UNITY:-$HOME/Unity/Hub/Editor/6000.3.24f1/Editor/Unity}"
LOG="${SOLOGYM_BUILD_LOG:-/tmp/sologym-android-test.log}"
if [ ! -x "$UNITY" ]; then echo "Unity not found at $UNITY (set SOLOGYM_UNITY)"; exit 1; fi
if [ ! -f "$ROOT/app/Assets/Firebase/Plugins/Firebase.Auth.dll" ]; then
  echo "WARNING: Firebase SDK not installed; sign-in/registration will be unavailable in this APK."
fi
if [ ! -f "$ROOT/app/Assets/SoloGym/Resources/Auth/FirebaseConfig.json" ]; then
  echo "WARNING: app/Assets/SoloGym/Resources/Auth/FirebaseConfig.json missing; Firebase stays unconfigured."
fi
echo "Building… (log: $LOG)"
"$UNITY" -batchmode -nographics -projectPath "$ROOT/app" \
  -executeMethod SoloGym.Editor.SoloGymBuild.BuildAndroidTest -quit -logFile "$LOG"
APK="$ROOT/app/Builds/Android/SoloGym-mvp-test.apk"
if [ -f "$APK" ]; then
  echo "APK ready: $APK"
  if command -v adb >/dev/null 2>&1 && adb get-state >/dev/null 2>&1; then
    echo "Phone detected: installing…"; adb install -r "$APK"
  fi
else
  echo "Build failed. Last log lines:"; tail -n 60 "$LOG"; exit 1
fi
