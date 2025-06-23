import * as Dexie from './dexie/dexie.mjs';

const db = new Dexie.Dexie('KoriTranslate');

db.version(2).stores({
    translations: 'id',
    languages: 'id'
});

export default db;