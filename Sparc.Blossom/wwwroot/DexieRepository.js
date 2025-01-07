import './dexie-4.0.10.js';

let dbs = {};

function db(dbName) {
    if (!dbs[dbName]) {
        dbs[dbName] = new Dexie(dbName);
        dbs[dbName].version(1).stores({
            docs: 'id'
        });
    }
    return dbs[dbName];
}

export async function find(dbName, id) {
    return await db(dbName).docs.get(id);
}

export async function register(dbName, doc, callback) {
    await bulkUpdate(dbName, doc);
    var query = liveQuery(() => db(dbName).docs.get(doc.id));
    var subscription = query.subscribe(callback);
    return subscription;
}

export async function add(dbName, doc) {
    console.log('adding', doc);
    await db(dbName).docs.add(doc);
}

export async function bulkAdd(dbName, docs) {
    await db(dbName).docs.bulkAdd(docs);
}

export async function update(dbName, doc) {
    await db(dbName).docs.put(doc);
}

export async function bulkUpdate(dbName, docs) {
    await db(dbName).docs.bulkPut(docs);
}

export async function remove(dbName, doc) {
    await db(dbName).docs.delete(doc.id);
}

export async function bulkRemove(dbName, docs) {
    var ids = docs.map(doc => doc.id);
    await db(dbName).docs.bulkDelete(ids);
}
