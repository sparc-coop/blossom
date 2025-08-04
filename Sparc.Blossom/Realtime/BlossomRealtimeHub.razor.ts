import { ApplyRemoteChangesFunction, IPersistedContext, ISyncProtocol, PollContinuation, ReactiveContinuation } from './Dexie/api';
import { Dexie } from './Dexie/dexie';
import { IDatabaseChange } from './Dexie/dexie-observable/api';
import './Dexie/dexie-observable/dexie-observable';
import './Dexie/dexie-syncable/dexie-syncable';

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

    }
}
