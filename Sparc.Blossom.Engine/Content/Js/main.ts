import TovikLanguageElement from './TovikLanguageElement.js';
import TovikElement from './TovikElement.js';
import TovikEngine from './TovikEngine.js';

// do an initial ping to Sparc Engine to set the cookie
TovikEngine.hi().then(() => {
    customElements.define('tovik-language', TovikLanguageElement);
    customElements.define('tovik-translate', TovikElement);

    // If the document does not have a <tovik-translate> element, create one and point it to the body
    if (!document.querySelector('tovik-translate')) {
        var bodyElement = document.createElement('tovik-translate');
        bodyElement.setAttribute('for', 'body');
        document.body.appendChild(bodyElement);
    }
});