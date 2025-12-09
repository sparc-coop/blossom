import * as AutomergeRepo from "https://esm.sh/@automerge/vanillajs/slim?bundle-deps";
await AutomergeRepo.initializeWasm(
    fetch("https://esm.sh/@automerge/automerge/dist/automerge.wasm")
);

const repo = new AutomergeRepo.Repo({
    storage: new AutomergeRepo.IndexedDBStorageAdapter(),
    network: [
        new AutomergeRepo.WebSocketClientAdapter("wss://sync.automerge.org"),
    ],
});

function db(dbName) {
    // Each dbName is a documentId in Automerge
    return repo.findDoc(dbName) || repo.create({ id: dbName });
}

export async function find(dbName, id) {
    const doc = repo.findDoc(dbName);
    if (!doc) return null;
    return doc.data[id] || null;
}

export async function add(dbName, doc) {
    let automergeDoc = repo.findDoc(dbName) || repo.create({ id: dbName });
    automergeDoc.change(d => {
        d[doc.id] = doc;
    });
}

export async function bulkAdd(dbName, docs) {
    let automergeDoc = repo.findDoc(dbName) || repo.create({ id: dbName });
    automergeDoc.change(d => {
        for (const doc of docs) {
            d[doc.id] = doc;
        }
    });
}

export async function update(dbName, doc) {
    let automergeDoc = repo.findDoc(dbName) || repo.create({ id: dbName });
    automergeDoc.change(d => {
        d[doc.id] = doc;
    });
}

export async function bulkUpdate(dbName, docs) {
    let automergeDoc = repo.findDoc(dbName) || repo.create({ id: dbName });
    automergeDoc.change(d => {
        for (const doc of docs) {
            d[doc.id] = doc;
        }
    });
}

export async function remove(dbName, doc) {
    let automergeDoc = repo.findDoc(dbName);
    if (!automergeDoc) return;
    automergeDoc.change(d => {
        delete d[doc.id];
    });
}

export async function bulkRemove(dbName, docs) {
    let automergeDoc = repo.findDoc(dbName);
    if (!automergeDoc) return;
    automergeDoc.change(d => {
        for (const doc of docs) {
            delete d[doc.id];
        }
    });
}

export async function getAll(dbName, revision) {
    const doc = repo.findDoc(dbName);
    if (!doc) return [];
    // revision is ignored for now; Automerge does not use revision numbers like Dexie
    return Object.values(doc.data);
}

