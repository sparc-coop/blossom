import { Dexie } from './Dexie/dexie';
export default class DexieDatabase {
    static _db = null;
    static init(name, repositories, version) {
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
    static get db() {
        return DexieDatabase._db;
    }
    static repository(name) {
        return DexieDatabase._db[name];
    }
}
//# sourceMappingURL=DexieDatabase.js.map