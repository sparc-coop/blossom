import { ApplyRemoteChangesFunction, IPersistedContext, ISyncProtocol, PollContinuation, ReactiveContinuation } from '../Data/Dexie/api';
import { Dexie } from '../Data/Dexie/dexie';
import { IDatabaseChange } from '../Data/Dexie/dexie-observable/api';
import '../Data/Dexie/dexie-observable/dexie-observable';
import '../Data/Dexie/dexie-syncable/dexie-syncable';

class BlossomSyncProtocol implements ISyncProtocol {
    partialsThreshold?: number;
    sync(context: IPersistedContext,
        url: string,
        options: any,
        baseRevision: any,
        syncedRevision: any,
        changes: IDatabaseChange[],
        partial: boolean,
        applyRemoteChanges: ApplyRemoteChangesFunction,
        onChangesAccepted: () => void,
        onSuccess: (continuation: PollContinuation | ReactiveContinuation) => void,
        onError: (error: any, again?: number) => void): void {

        // This should be an adapter for the Blossom realtime hub
        // tailored after https://github.com/dexie/Dexie.js/blob/master/samples/remote-sync/websocket/WebSocketSyncProtocol.js
    }
}
