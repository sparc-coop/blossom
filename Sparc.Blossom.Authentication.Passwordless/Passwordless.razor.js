let client = {};

function initialize(apiKey) {
    client = new Passwordless.Client({
        apiKey: apiKey
    });
}

async function register(email) {
    const registrationResponse = await fetch('/_auth/register', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ Email: email })
    });

    // If no error then deserialize and use returned token to create now our passkeys
    if (registrationResponse.ok) {
        const registrationResponseJson = await registrationResponse.json();
        const token = registrationResponseJson.token;

        const registeredPasskeyResponse = await client.register(token, email);
    }
}

async function login(alias, redirectUrl) {
    const loginPasskeyResponse =
        alias ? await client.signinWithAlias(alias)
            : await client.signInWithDiscoverable();

    if (!loginPasskeyResponse) {
        return;
    }
    const loginRequest = {
        Token: loginPasskeyResponse.token
    };
    const loginResponse = await fetch('/_auth/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(loginRequest)
    });

    if (loginResponse.ok) {
        console.log('login successful: ' + (await loginResponse.text()));

        // Redirect to authorized page /Authorized/HelloWorld
        window.location.href = redirectUrl || '/';
    }
}
