import * as Dexie from './dexie/dexie.mjs';

const db = new Dexie.Dexie('KoriTranslate');

db.version(1).stores({ translations: 'id' });

export default db;