# MVP fitness test build (0.6.0)

Recorded 1 October 2026. Branch `mvp/fitness-core`. This build connects the existing native windows into one working fitness loop for a real account. The Hades-inspired action game is out of scope here.

## What a tester can do

**Create an account or sign in → age/country → character → privacy/terms → private profile and readiness → goals and experience → equipment → schedule → plan review → personal Home → workout journal → readiness check → workout dungeon → history.**

| Area | Behavior in this build |
| --- | --- |
| Account | Email/password registration through Firebase after age (15+), country and both document choices are completed on the device. Email and Google sign-in. Password reset email. Session restored on launch (works offline). Sign out. Delete account (Firebase identity + this device's data). |
| Setup | Equipment (home/gym/outdoor, bodyweight-only or catalog equipment), 2–5 training weekdays, 15/25/40/60-minute sessions, generated plan review with explicit acknowledgment. Editable later from Home → Settings → Edit training plan; history is kept. |
| Plan generation | On-device C# port of `tools/training_reference.py`: same exercise catalog, dose rules, teen limits, readiness handling, time budgets and equal boss shares. Verified identical to the Python reference on 32,400 generated sessions. |
| Home | The chosen Barbarian, the person's name, today's routine (or rest day / next session), streak and bosses defeated. Decorations and fasting saved per account. |
| Workouts | The person's own week and history, routine edits (session type, length, exercise swaps within eligibility), readiness before every session. Missed days show as "Not done" and never create extra work. |
| Dungeon | Manual set logging, rest, pause/resume, difficulty changes mid-session, stop early with partial progress kept. Extra reps or load never add damage beyond the planned 1000. |
| Fasting | Unchanged optional adult tracker; hidden below 18; stored per account. |

## Build the APK

On the machine that already builds SoloGym (Unity 6000.3.24f1 with Android support, Firebase SDK and `FirebaseConfig.json` installed):

```bash
git fetch origin && git checkout mvp/fitness-core
tools/build_android_test.sh          # writes app/Builds/Android/SoloGym-mvp-test.apk and installs it if a phone is connected via adb
```

Or in the editor: **SoloGym → Build → Android Test APK (MVP)**. Close the editor before running the script; batch mode cannot open a project that is already open.

The optional GitHub Actions workflow `.github/workflows/android-test-apk.yml` builds the same APK in the cloud once the Unity license and Firebase secrets listed at its top are added to the repository.

Firebase prerequisites (already enabled for this project per `app/README.md`): Email/Password and Google providers. Password-reset emails use Firebase's default template.

## Test checklist

1. Fresh install → **Crear cuenta** → age 16 and age 30 (two accounts) → country → character → both document choices → email/password → private profile → readiness **Listo** → goal → experience → equipment → days → length → plan review → acknowledge → **Comenzar**.
2. Home shows your character and today's routine or a rest day. Kill and relaunch the app: you return to Home without signing in.
3. **Rutinas**: today's routine → Preparar sesión → readiness → review → acknowledge → Entrar. Log every set (try entering more reps than prescribed), change difficulty mid-session, complete the guardian. Back in Home: "¡Jefe derrotado hoy!" and the counter increases.
4. Start another day's session (or create one for today) and stop early: history shows **Interrumpida**, no extra work is added.
5. Readiness **Tengo dolor** → no workout is prescribed; rest is offered.
6. Teen account: only general fitness/mobility goals; strength days ask for supervision before each session; fasting is not available.
7. Edit: Home → Ajustes → Editar plan → change equipment to bodyweight and days → completed history remains; future days follow the new setup.
8. Ajustes → Cerrar sesión → sign in again with the same email → Home. ¿Olvidaste tu contraseña? with the email typed → reset email arrives.
9. Ajustes → Eliminar cuenta → confirm → back at sign-in; the email can be registered again.
10. Spanish/English toggle on each screen; small and wide landscape phones; airplane mode after sign-in.

## Known limits of this test build

- **Local data only.** Profile, plan, journal and fasting live in a per-account folder on the phone. Another phone requires setup again; uninstalling removes them. Cloud sync (blueprint decision 7) is not implemented.
- **Eligibility and consent are recorded on the device**, with the authorized Lorem ipsum documents. A trusted server check, final legal documents and consent receipts are still required before any public release.
- **Exercise content remains a draft** pending professional and youth review. No exercise demonstrations/animations.
- No rewards ledger, XP economy, ads, purchases or running/speed progression yet.
- The equipment, schedule and plan-review steps reuse the approved guild-panel controls; they did not receive separate full-screen renders.
- Unity smoke/capture routes still exercise the fictional review fixtures; the connected flow was verified by compilation against Unity 6000.3.24f1 assemblies (runtime, Firebase and editor) and by the pure-logic suites in `tests/csharp` (engine parity, 783 journal/dungeon checks, account storage), not yet on a phone.
