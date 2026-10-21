function base64UrlEncodeBytes(bytes: Uint8Array): string {
    let binary = '';
    const chunk = 0x8000;
    for (let i = 0; i < bytes.length; i += chunk) {
        binary += String.fromCharCode(...bytes.subarray(i, i + chunk));
    }
    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}


export async function startAssertion(optionsJson: string): Promise<string> {
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(JSON.parse(optionsJson));
    const credential = await navigator.credentials.get({publicKey: options})
    return credential ? JSON.stringify(serializeCredential(credential as PublicKeyCredential)) : "";
}

export async function startCreation(optionsJson: string): Promise<string> {
    const options = PublicKeyCredential.parseCreationOptionsFromJSON(JSON.parse(optionsJson))
    const credential = await navigator.credentials.create({publicKey: options});
    return credential ? JSON.stringify(serializeCredential(credential as PublicKeyCredential)) : "";
}

function serializeCredential(credential: PublicKeyCredential) {
    const attestation = credential.response as AuthenticatorAttestationResponse;
    const assertion = credential.response as AuthenticatorAssertionResponse;

    return {
        id: credential.id,
        rawId: base64UrlEncodeBytes(new Uint8Array(credential.rawId)),
        type: credential.type,
        response: {
            attestationObject: attestation.attestationObject
                ? base64UrlEncodeBytes(new Uint8Array(attestation.attestationObject))
                : null,
            authenticatorData: assertion.authenticatorData
                ? base64UrlEncodeBytes(new Uint8Array(assertion.authenticatorData))
                : null,
            signature: assertion.signature
                ? base64UrlEncodeBytes(new Uint8Array(assertion.signature))
                : null,
            userHandle: assertion.userHandle
                ? base64UrlEncodeBytes(new Uint8Array(assertion.userHandle))
                : null,
            clientDataJSON: base64UrlEncodeBytes(new Uint8Array(credential.response.clientDataJSON)),
        },
        clientExtensionResults: credential.getClientExtensionResults(),
    };
}
