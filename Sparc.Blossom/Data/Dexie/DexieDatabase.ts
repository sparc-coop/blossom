import { Dexie } from './dexie';

export default class DexieDatabase {
    private static _db: Dexie | null = null;

    public static init(name: string, repositories: Map<string, string[]>, version: number) {
        if (DexieDatabase._db) {
            throw new Error('Database is already initialized.');
        }

        let stores = {};
        repositories.forEach((value, key) => {
            stores[key] = value.join(',');
        });

        DexieDatabase._db = new Dexie(name);
        DexieDatabase._db.version(version).stores(stores);
        //DexieDatabase._db.syncProtocol = new BlossomSyncProtocol();
    }

    public static get db(): Dexie {
        return DexieDatabase._db;
    }

    public static repository(name: string) {
        return DexieDatabase._db[name];
    }
}