package com.kalavhan.sologym.auth;

import android.app.Activity;
import android.os.CancellationSignal;
import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.GetCredentialException;
import androidx.credentials.exceptions.GetCredentialProviderConfigurationException;
import com.google.android.libraries.identity.googleid.GetSignInWithGoogleOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

/** Native account picker only; the ID token is exchanged by Firebase Unity, never logged. */
public final class GoogleIdentityBridge {
    public interface Callback { void onResult(String status, String idToken); }
    private CancellationSignal cancellation;

    public void signIn(Activity activity, String webClientId, Callback callback) {
        activity.runOnUiThread(() -> {
            if (webClientId == null || webClientId.isEmpty()) {
                callback.onResult("unavailable", "");
                return;
            }
            cancellation = new CancellationSignal();
            try {
                GetSignInWithGoogleOption option = new GetSignInWithGoogleOption.Builder(webClientId).build();
                GetCredentialRequest request = new GetCredentialRequest.Builder().addCredentialOption(option).build();
                CredentialManager.create(activity).getCredentialAsync(activity, request, cancellation,
                    activity::runOnUiThread,
                    new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>() {
                        @Override public void onResult(GetCredentialResponse response) {
                            try {
                                Credential credential = response.getCredential();
                                if (credential instanceof CustomCredential &&
                                    GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(credential.getType())) {
                                    String token = GoogleIdTokenCredential.createFrom(credential.getData()).getIdToken();
                                    callback.onResult("success", token);
                                } else callback.onResult("error", "");
                            } catch (Exception ignored) { callback.onResult("error", ""); }
                        }
                        @Override public void onError(GetCredentialException error) {
                            callback.onResult(error instanceof GetCredentialCancellationException ? "cancelled"
                                : error instanceof GetCredentialProviderConfigurationException ? "unavailable" : "error", "");
                        }
                    });
            } catch (Exception ignored) { callback.onResult("error", ""); }
        });
    }

    public void cancel() { if (cancellation != null) cancellation.cancel(); }
}
