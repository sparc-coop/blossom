import './pouchdb-9.0.0.js';
import './pouchdb.find.js';

let dbs = {};

function getDb(dbName) {
    if (!dbs[dbName]) {
        dbs[dbName] = {
            db: new PouchDB(dbName),
            indexes: {}
        }
    }
    return dbs[dbName].db;
}

async function find(dbName, id) {
    return await getDb(dbName).get(id);
}

async function add(dbName, doc) {
    doc._id = doc.id;
    await getDb(dbName).put(doc);
}

async function bulkAdd(dbName, docs) {
    await getDb(dbName).bulkDocs(docs);
}

async function update(dbName, doc) {
    var result = await find(dbName, doc.id);
    doc._id = doc.id;
    doc._rev = result._rev;
    await getDb(dbName).put(doc);
}

async function remove(dbName, doc) {
    var result = await find(dbName, doc.id);
    await getDb(dbName).remove(doc.id, result._rev);
}

async function bulkRemove(dbName, docs) {
    // add _deleted: true to each doc
    docs = docs.map(doc => ({ ...doc, _deleted: true }));
    await getDb(dbName).bulkDocs(docs);
}

async function getAll(dbName) {
    var result = await getDb(dbName).allDocs({ include_docs: true });
    return result.rows.map(row => row.doc);
}

async function index(dbName, spec) {
    const { fields = [] } = spec;
    const db = getDb(dbName);
    await db.createIndex({ index: { fields: fields } });
}

async function query(dbName, spec) {
    var db = getDb(dbName);
    var result = await db.find(spec);
    return result.docs;
}

export { find, add, bulkAdd, update, remove, bulkRemove, getAll, index, query };