import * as Dexie from './dexie/dexie.mjs';

const db = new Dexie.Dexie('TovikTranslate');

db.version(5).stores({
    translations: 'id',
    edits: 'id'
});

export default db;