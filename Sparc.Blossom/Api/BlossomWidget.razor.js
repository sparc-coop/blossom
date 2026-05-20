export function initialize(dotnet) {
    window.addEventListener('message', async (event) => {
        try {
            if (event?.data?.type == "response")
                await dotnet.invokeMethodAsync('OnResponse', event.data);
        } catch (e) {
            console.error('oops', e);
        }
    });
}

export function broadcast(eventName, body) {
    const event = { type: 'request', name: eventName, body: body };
    console.log('broadcasting', event);
    window.parent.postMessage(event, '*');
}