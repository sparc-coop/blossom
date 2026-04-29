import * as Dexie from './dexie/dexie.mjs';

const db = new Dexie.Dexie('TovikTranslate');

db.version(4).stores({
    translations: 'id',
    languages: 'id',
    profiles: 'id'
});

export default db;