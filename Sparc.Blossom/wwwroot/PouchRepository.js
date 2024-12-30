let dbs = {};

function db(dbName) {
    if (!dbs[dbName]) {
        dbs[dbName] = new PouchDb(dbName);
    }
    return dbs[dbName];
}

function find(dbName, id) {
    return db(dbName).get(id);
}

function add(dbName, doc) {
    doc._id = doc.id;
    db(dbName).put(doc);
}

function update(dbName, doc) {
    get(dbName, doc.id).then(function (result) {
        doc._rev = result._rev;
        db(dbName).put(doc);
    });
}

function remove(dbName, doc) {
    get(dbName, doc.id).then(function (result) {
        db(dbName).remove(doc.id, result._rev);
    });
}

function allDocs(dbName) {
    return db(dbName).allDocs({ include_docs: true });
}