import BlossomEvent from "./BlossomEvent";

export default class BlossomEvents {
    static on(eventName: string, callback) {
        window.addEventListener('message', async (e: any) => {
            if (!e.data)
                return;

            try {
                if (!e.data || e.data.name !== eventName)
                    return;

                callback(e.data.body);
                e.data.type = 'response';
                e.source.postMessage(e.data, e.origin);
            } catch (e) { }
        });
    }

    static broadcast(broadcastTo, eventName: string, body: any = null) {
        const event = new BlossomEvent('request', eventName, body);
        console.log('posting message', event);
        broadcastTo.contentWindow.postMessage(event, '*');
    }
}