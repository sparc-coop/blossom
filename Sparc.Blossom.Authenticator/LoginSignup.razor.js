let client = {}; 

function init(apiKey) {
    client = new Passwordless.Client({
        apiKey: apiKey
    });
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

export { init, signInWithPasskey, signUpWithPasskey };