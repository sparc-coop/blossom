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

async function syncAll(datasetId) {
    console.log("begin sync");

    var _db = getDb(datasetId);

    console.log("local db", _db);

    var cosmosDbUrl = `https://localhost:7185/data/${datasetId}`;

    console.log("cosmosDbUrl", cosmosDbUrl);

    var cosmosDb = new PouchDB(cosmosDbUrl, {
        fetch: async function (url, opts) {
            opts.headers.set("x-functions-key", "abc123");
            return PouchDB.fetch(url, opts);
        }
    });

    // Sync options
    var opts = {
        live: true,
        retry: true // Retry on failure
    };

    var changes = [];

    return new Promise((resolve, reject) => {
        _db.sync(cosmosDb, opts)
            .on('change', function (info) {
                if (info.change && info.change.docs) {
                    changes = changes.concat(info.change.docs);
                }
            })
            .on('paused', function (err) {
                // Replication paused (e.g., offline or up-to-date)
                if (err) {
                    console.warn("Sync paused due to an error:", err);
                } else {
                    console.log("Sync paused (up-to-date or offline).");
                }
            })
            .on('active', function () {
                // Replication resumed
                console.log("Sync resumed.");
            })
            .on('denied', function (err) {
                // Document write denied
                console.error("Sync denied:", err);
            })
            .on('complete', function (info) {
                // Sync completed
                console.log("Sync complete:", info);
                resolve(changes);
            })
            .on('error', function (err) {
                // Unhandled error
                console.error("Sync error:", err);
                reject(err);
            });
    });
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

async function count(dbName) {
    const result = await getDb(dbName).allDocs({ limit: 0 });
    return result.total_rows;
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

export { find, add, bulkAdd, update, remove, bulkRemove, getAll, index, query, count, syncAll };