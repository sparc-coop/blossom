import KoriTranslateNode from './KoriTranslateNode.js';
import KoriLangSelectElement from './KoriLangSelectElement.js';
import KoriTranslateElement from './KoriTranslateElement.js';
customElements.define('kori-t', KoriTranslateNode);
customElements.define('kori-langselect', KoriLangSelectElement);
customElements.define('kori-translate', KoriTranslateElement);
document.addEventListener('DOMContentLoaded', () => {
    // If the document does not have a <kori-translate> element, create one and point it to the body
    if (!document.querySelector('kori-translate')) {
        var bodyElement = document.createElement('kori-translate');
        bodyElement.setAttribute('for', 'body');
        document.body.appendChild(bodyElement);
    }
});
//# sourceMappingURL=kori-translate.js.map