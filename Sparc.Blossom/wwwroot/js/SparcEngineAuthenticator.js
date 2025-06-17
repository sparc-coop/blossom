let client = {};
function initSparcEngineAuthenticator(apiKey) {
    console.log("initializing passwordless client...");
    client = new Passwordless.Client({
        apiKey: apiKey
    });
    console.log(client);
}

async function signInWithPasskey(alias) {
    console.log("Starting sign in...");

    try {
        const { token, error } = alias
            ? await client.signinWithAlias(alias)
            : await client.signinWithDiscoverable();

        if (error) {
            console.log(JSON.stringify(error, null, 2));
            console.error("Sign in failed, received the following error");
            return;
        }

        console.log("Received token", token);

        return token;

    } catch (e) {
        console.error("Things went really bad: ", e);
        Status("Things went bad, check console");
    }
}

async function signUpWithPasskey(registerToken) {
    console.log("initializing handleRegisterClick...")

    try {
        const { token, error } = await client.register(registerToken);

        if (token) {
            console.log("Successfully registered WebAuthn. You can now sign in!");
            return token;
        }

        console.log(JSON.stringify(error, null, 2))
        console.error("We failed to register a passkey: ");
    } catch (e) {
        console.error("Things went bad", e);
    }
}

//export { init, signInWithPasskey, signUpWithPasskey };

window.initSparcEngineAuthenticator = initSparcEngineAuthenticator;
window.signInWithPasskey = signInWithPasskey;
window.signUpWithPasskey = signUpWithPasskey;

// passwordless cdn script
!function (r, e) { "object" == typeof exports && "undefined" != typeof module ? e(exports) : "function" == typeof define && define.amd ? define(["exports"], e) : e((r = "undefined" != typeof globalThis ? globalThis : r || self).Passwordless = {}) }(this, (function (r) { "use strict"; async function e() { return !!t() && PublicKeyCredential.isUserVerifyingPlatformAuthenticatorAvailable() } function t() { return void 0 !== window.PublicKeyCredential && "function" == typeof window.PublicKeyCredential } async function i() { const r = window.PublicKeyCredential; return !!r.isConditionalMediationAvailable && r.isConditionalMediationAvailable() } function n(r) { if ("string" != typeof r) { const e = "Cannot convert from Base64Url to ArrayBuffer: Input was not of type string"; throw console.error(e, r), new TypeError(e) } const e = r.replace(/-/g, "+").replace(/_/g, "/"); const t = (4 - e.length % 4) % 4, i = e.padEnd(e.length + t, "="), n = window.atob(i), o = new Uint8Array(n.length); for (let r = 0; r < n.length; r++)o[r] = n.charCodeAt(r); return o } function o(r) { const e = (() => { if (Array.isArray(r)) return Uint8Array.from(r); if (r instanceof ArrayBuffer) return new Uint8Array(r); if (r instanceof Uint8Array) return r; const e = "Cannot convert from ArrayBuffer to Base64Url. Input was not of type ArrayBuffer, Uint8Array or Array"; throw console.error(e, r), new Error(e) })(); let t = ""; for (let r = 0; r < e.byteLength; r++)t += String.fromCharCode(e[r]); const i = window.btoa(t); return i.replace(/\+/g, "-").replace(/\//g, "_").replace(/=*$/g, "") } function s(r) { return function (r) { if ("object" == typeof (e = r) && null !== e && "message" in e && "string" == typeof e.message) return r; var e; try { return new Error(JSON.stringify(r)) } catch (e) { return new Error(String(r)) } }(r).message } r.Client = class { constructor(r) { this.config = { apiUrl: "https://v4.passwordless.dev", apiKey: "", origin: window.location.origin, rpid: window.location.hostname }, this.abortController = new AbortController, Object.assign(this.config, r) } async register(r, e) { var t; try { this.assertBrowserSupported(); const i = await this.registerBegin(r); if (i.error) return console.error(i.error), { error: i.error }; i.data.challenge = n(i.data.challenge), i.data.user.id = n(i.data.user.id), null === (t = i.data.excludeCredentials) || void 0 === t || t.forEach((r => { r.id = n(r.id) })); const o = await navigator.credentials.create({ publicKey: i.data }); if (!o) { const r = { from: "client", errorCode: "failed_create_credential", title: "Failed to create credential (navigator.credentials.create returned null)" }; return console.error(r), { error: r } } return await this.registerComplete(o, i.session, e) } catch (r) { const e = { from: "client", errorCode: "unknown", title: s(r) }; return console.error(r), console.error(e), { error: e } } } async signinWithId(r) { return this.signin({ userId: r }) } async signinWithAlias(r) { return this.signin({ alias: r }) } async signinWithAutofill() { if (!await i()) throw new Error("Autofill authentication (conditional meditation) is not supported in this browser"); return this.signin({ autofill: !0 }) } async signinWithDiscoverable() { return this.signin({ discoverable: !0 }) } abort() { this.abortController && this.abortController.abort() } isPlatformSupported() { return e() } isBrowserSupported() { return t() } isAutofillSupported() { return i() } async registerBegin(r) { const e = await fetch(`${this.config.apiUrl}/register/begin`, { method: "POST", headers: this.createHeaders(), body: JSON.stringify({ token: r, RPID: this.config.rpid, Origin: this.config.origin }) }), t = await e.json(); return e.ok ? t : { error: { ...t, from: "server" } } } async registerComplete(r, e, t) { const i = r.response, n = await fetch(`${this.config.apiUrl}/register/complete`, { method: "POST", headers: this.createHeaders(), body: JSON.stringify({ session: e, response: { id: r.id, rawId: o(r.rawId), type: r.type, extensions: r.getClientExtensionResults(), response: { AttestationObject: o(i.attestationObject), clientDataJson: o(i.clientDataJSON) } }, nickname: t, RPID: this.config.rpid, Origin: this.config.origin }) }), s = await n.json(); return n.ok ? s : { error: { ...s, from: "server" } } } async signin(r) { var e; try { this.assertBrowserSupported(), this.handleAbort(), r || (r = { discoverable: !0 }); const t = await this.signinBegin(r); if (t.error) return t; t.data.challenge = n(t.data.challenge), null === (e = t.data.allowCredentials) || void 0 === e || e.forEach((r => { r.id = n(r.id) })); const i = await navigator.credentials.get({ publicKey: t.data, mediation: "autofill" in r ? "conditional" : void 0, signal: this.abortController.signal }); return await this.signinComplete(i, t.session) } catch (r) { const e = { from: "client", errorCode: "unknown", title: s(r) }; return console.error(r), console.error(e), { error: e } } } async signinBegin(r) { const e = await fetch(`${this.config.apiUrl}/signin/begin`, { method: "POST", headers: this.createHeaders(), body: JSON.stringify({ userId: "userId" in r ? r.userId : void 0, alias: "alias" in r ? r.alias : void 0, RPID: this.config.rpid, Origin: this.config.origin }) }), t = await e.json(); return e.ok ? t : { error: { ...t, from: "server" } } } async signinComplete(r, e) { const t = r.response, i = await fetch(`${this.config.apiUrl}/signin/complete`, { method: "POST", headers: this.createHeaders(), body: JSON.stringify({ session: e, response: { id: r.id, rawId: o(new Uint8Array(r.rawId)), type: r.type, extensions: r.getClientExtensionResults(), response: { authenticatorData: o(t.authenticatorData), clientDataJson: o(t.clientDataJSON), signature: o(t.signature) } }, RPID: this.config.rpid, Origin: this.config.origin }) }), n = await i.json(); return i.ok ? n : { error: { ...n, from: "server" } } } handleAbort() { this.abort(), this.abortController = new AbortController } assertBrowserSupported() { if (!t()) throw new Error("WebAuthn and PublicKeyCredentials are not supported on this browser/device") } createHeaders() { return { ApiKey: this.config.apiKey, "Content-Type": "application/json", "Client-Version": "js-1.1.0" } } }, r.isAutofillSupported = i, r.isBrowserSupported = t, r.isPlatformSupported = e }));