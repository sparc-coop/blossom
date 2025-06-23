import KoriTranslateElement from './KoriTranslate.js';
import KoriLangSelectElement from './KoriLangSelectElement.js';
import KoriTranslator from './KoriTranslator.js';
customElements.define('kori-translate', KoriTranslateElement);
customElements.define('kori-langselect', KoriLangSelectElement);
document.addEventListener('DOMContentLoaded', () => new KoriTranslator().init(document.body));
//# sourceMappingURL=kori-translate.js.map