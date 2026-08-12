import { base64UrlEncodeBytes } from "./util";


export async function startAssertion(optionsJson: string): Promise<string> {
    const options = PublicKeyCredential.parseRequestOptionsFromJSON(JSON.parse(optionsJson));
    console.log("we parsed the credentials")
    const credential = await navigator.credentials.get({publicKey: options})
    console.log("we got the credential")
    return credential ? JSON.stringify(serializeCredential(credential as PublicKeyCredential)) : "";
}

export async function startCreation(optionsJson: string): Promise<string> {
    console.log("buisness insider: how is a passkey created on the client")
    const options = PublicKeyCredential.parseCreationOptionsFromJSON(JSON.parse(optionsJson))
    console.log(options)
    const credential = await navigator.credentials.create({publicKey: options});
    console.log("we do the stuff mhmmm and then yeah that too. right and we return")
    return credential ? JSON.stringify(serializeCredential(credential as PublicKeyCredential)) : "";
}

function serializeCredential(credential: PublicKeyCredential) {
    const attestation = credential.response as AuthenticatorAttestationResponse;
    const assertion = credential.response as AuthenticatorAssertionResponse;

    return {
        id: credential.id, // already base64url per WebAuthn spec
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