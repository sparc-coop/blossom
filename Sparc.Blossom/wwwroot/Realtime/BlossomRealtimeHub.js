import '../Data/Dexie/dexie-observable/dexie-observable';
import '../Data/Dexie/dexie-syncable/dexie-syncable';
class BlossomSyncProtocol {
    partialsThreshold;
    sync(context, url, options, baseRevision, syncedRevision, changes, partial, applyRemoteChanges, onChangesAccepted, onSuccess, onError) {
        // This should be an adapter for the Blossom realtime hub
        // tailored after https://github.com/dexie/Dexie.js/blob/master/samples/remote-sync/websocket/WebSocketSyncProtocol.js
    }
}
//# sourceMappingURL=BlossomRealtimeHub.js.map