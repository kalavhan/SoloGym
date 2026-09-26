# Firebase Authentication preparation

WIN-002 uses `FirebaseWelcomeAuthService` behind `IWelcomeAuthService`. The local Android environment has an explicitly configured Firebase project with **Email/Password and Google enabled**. The public support contact is authorized, and the real web/Android OAuth clients are configured. The 0.2.1/code 3 Android build passed packaging, signature and configuration checks; physical-device provider checks remain pending. An absent or incomplete configuration returns unavailable; the adapter never substitutes a local/demo success. This milestone does not implement account creation, password recovery, onboarding or completed-profile lookup.

## Current project state

- Personal Firebase project **`sologym-66395`**, **Spark** plan, with Android package **`com.kalavhan.sologym`** and its actual Firebase app ID registered.
- The current installation-signing SHA-1 and SHA-256 fingerprints are registered. Future signing certificates need their own registration.
- **Email/Password enabled.** The environment-specific files at `app/Assets/google-services.json` and `app/Assets/SoloGym/Resources/Auth/FirebaseConfig.json` are ignored by Git. Existing Firebase SDK client fields are retained, with actual observed OAuth client entries added for Google; identifiers are not guessed.
- **Google enabled.** The user answered **“1”**, selecting the first option that authorized use of the chosen public support contact. This resolves the earlier automatic-review permission block. Firebase now shows both providers enabled. Actual OAuth web and Android clients are configured; the Android client's package and SHA-1 match `com.kalavhan.sologym` and the existing APK. The OAuth audience already shows **External / In production** in the Console; this was a read-only check, not an audience change. No personal email address is committed here.
- The **0.2.1/code 3** Android APK with both providers configured and `SOLOGYM_REVIEW` passed packaging, signature and configuration checks (about **34 MiB**). No real-device email/Google sign-in is recorded. Existing screenshots and the Linux player retain their **0.2.0** provenance because the UI is unchanged.

`SOLOGYM_REVIEW` exposes the labeled sample Home in local review builds, including configured builds. It does not authorize a profile or bypass Firebase authentication for a real account. Omit the review define for a future production build.

## Implemented boundary

- Email/password sign-in uses the official Firebase Unity SDK. Errors preserve email-enumeration protection; credentials and tokens are not written to PlayerPrefs or logs.
- Android Google sign-in opens the native Credential Manager account picker and exchanges its Google ID token with Firebase. There is no embedded OAuth WebView. The Google **web** client ID is required, in addition to Android registration/signing fingerprints.
- A successful identity routes only to WIN-006 with `firebase_identity_requires_onboarding`. It does not authorize Home, grant game items, infer consent, or manufacture a fitness profile. Later milestones need a trusted server checkpoint before bypassing onboarding.
- Firebase owns session storage. Cancelled sign-ins cannot navigate. Since Firebase requests cannot be cancelled directly, requests are serialized and a late successful cancelled request is signed out before another request begins.
- The official Auth package includes Analytics dependencies. Android manifest metadata deactivates Analytics collection for this milestone. `SoloGymAndroidPrivacyManifest` also applies `tools:node="remove"` in the generated **launcher** manifest to remove transitive advertising ID, AdServices attribution and install-referrer permissions at the highest merge priority. Required network/authentication permissions remain.
- iOS email integration can use its separately registered app ID when an iOS build is available. The native Google provider adapter currently supports Android only; iOS and desktop Google calls return unavailable. No iOS runtime verification is claimed.

## Reproducible SDK installation

The repository keeps integration source and an installer instead of large vendor binaries. Run from the repository root:

```bash
python3 tools/install_firebase_unity.py
```

The script downloads the official Firebase Unity **13.17.0** distribution, verifies the archive against SHA-256 `4fba6b1315eb24987dcf5a391c9f88518ea9c3c0eba6c7e45d88a991a034f7c0`, and installs only `FirebaseAuth.unitypackage` (Auth, App and the bundled External Dependency Manager), preserving vendor metadata and licenses. `--archive /path/to/firebase_unity_sdk_13.17.0.zip` reuses an existing download. The archive is about 726 MiB; the selected package is about 62 MiB compressed.

Open Unity once to import. `FirebaseIntegrationSetup.ScriptingDefinesForBuild` adds `SOLOGYM_FIREBASE_AUTH` to player builds when both official DLLs exist. It does not persist that symbol in ProjectSettings, which keeps a clean clone without the ignored SDK compilable. Unity Editor previews report authentication unavailable; configured native builds use the real adapter. Before an Android build, run External Dependency Manager → Android Resolver → Force Resolve if dependencies have not been resolved automatically. Resolution fetches Google/Maven dependencies. SDK vendor directories and generated AARs are ignored.

Android builds require the installer first because the committed Gradle templates reference Firebase's generated local Maven repository. On this workspace, Unity imported the SDK and resolved that repository automatically; no separate interactive resolver command was needed. `com.unity.modules.androidjni` is included for the native Google bridge.

Native Google dependencies are pinned to the versions in Firebase's Android authentication guide: Credential Manager **1.3.0** (`credentials` and `credentials-play-services-auth`) and Google ID **1.1.1**. The local Android library contains source and consumer keep rules; Gradle fetches these packages. No credentials are required to download these public SDKs.

`FirebaseAndroidGradleFix` normalizes EDM 1.2.189's malformed `file:////` local repository prefix on Linux after Gradle project generation. Gradle then converts the absolute directory safely, including spaces in this workspace path. It does not change remote repository URLs or dependency versions.

## Consolidated build verification

The **0.2.1/code 3** Android IL2CPP ARM64 build completed with Firebase App/Auth native libraries and the Java Credential Manager bridge. Its APK is **35,637,787 bytes**, SHA-256 `f56b0451b23c12cedff4948ea0e24b34de9624750731b23da646376774643e23`. APK v2 signature verification passed with the unchanged installation certificate. The compiled `default_web_client_id` resource and actual Unity-packaged `FirebaseConfig` asset match the configured OAuth web client.

All four advertising/attribution/referrer permissions are absent, `firebase_analytics_collection_deactivated` is true and `firebase_data_collection_default_enabled` is false. Provider settings show both Email/Password and Google enabled. Successful sign-in and cancellation on a physical device have not been verified; compilation, correct packaging and configuration do not establish that result. The retained visual captures and Linux player remain 0.2.0 with unchanged UI.

## Reproduce or finish project connection

Use the existing personal SoloGym Firebase project above; do not create a duplicate. No service-account private key belongs in the mobile app. The following steps also describe the requirements for an explicitly chosen future environment.

1. Register Android package `com.kalavhan.sologym`. Register the SHA-1 and SHA-256 for each actual install signing certificate (development and later Play App Signing). Register iOS separately when its bundle identifier/build pipeline is ready.
2. Email/Password and Google are already enabled, and the public support contact is authorized. The current OAuth audience is External / In production; no audience change was made. Keep email enumeration protection enabled. The sign-in form does not create accounts; any test-account creation remains an explicit separate action until the approved signup milestone is implemented.
3. Keep the Firebase Android client configuration at `app/Assets/google-services.json`. It contains client configuration, not an administrative credential. The current file retains the existing Firebase SDK fields plus the observed web and Android OAuth clients. Prefer re-exporting it from Firebase after future app/provider changes. The repository ignores it to keep environments explicit.
4. Copy [`firebase-auth.example.json`](../../config/firebase-auth.example.json) to `app/Assets/SoloGym/Resources/Auth/FirebaseConfig.json`. Fill `projectId`, `androidAppId` (`mobilesdk_app_id`) and `apiKey` (`current_key`) with the actual app values, and set `enabled: true`. Set `googleWebClientId` to the observed OAuth **web** client (`client_type: 3`), never the Android client. `iosAppId` is only for the separately registered iOS application. Never put a client secret or service-account key here.
5. Rebuild and perform one physical Android sign-in with each enabled provider plus cancellation. Until this happens, report **email and Google configured; physical-device verification pending**. Compilation and an unconfigured-provider smoke check do not verify a real sign-in.

Do not hand-enter guessed project/client IDs. Public client configuration must match the actual Firebase application and signing certificate. There is no automatic fallback to another Firebase project.

## Source provenance

Reviewed on 2026-09-25. The old `firebase.google.com/docs/unity/alt-setup` URL returned 404; the current official distribution contains `.unitypackage` files, not UPM tarballs.

- [Official Firebase Unity setup and dependency initialization](https://firebase.google.com/docs/unity/setup)
- [Official pinned Unity SDK download](https://dl.google.com/firebase/sdk/unity/firebase_unity_sdk_13.17.0.zip)
- [Firebase Unity email authentication](https://firebase.google.com/docs/auth/unity/password-auth)
- [Firebase Unity Google credential exchange](https://firebase.google.com/docs/auth/unity/google-signin)
- [Firebase Android Google authentication and dependency versions](https://firebase.google.com/docs/auth/android/google-signin)
- [Android Credential Manager button flow](https://developer.android.com/identity/sign-in/credential-manager-siwg-implementation)
- [Android manifest merge priorities and removal markers](https://developer.android.com/build/manage-manifests)

SDK licensing is included in the downloaded distribution and installed under the vendor directories. The local Java bridge and C# adapter are SoloGym integration source; SDK binaries are not copied into Git.
