import { Dexie } from './dexie.mjs';
let _db = null;

export function init(name, repositories, version) {
    console.log('initting', name, repositories, version);

    if (_db) {
        throw new Error('Database is already initialized.');
    }
    let stores = {};
    for (let [key, value] of Object.entries(repositories)) {
        stores[key] = value.join(',');
    }
    _db = new Dexie(name);
    _db.version(version).stores(stores);
    //DexieDatabase._db.syncProtocol = new BlossomSyncProtocol();
}

export function db() { return _db; }

export function repository(name) {
    return _db[name];
}