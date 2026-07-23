import KoriElement from './KoriElement.js';
import TovikElement from './TovikElement.js';
import TovikEngine from './TovikEngine.js';
import TovikLanguageElement from './TovikLanguageElement.js';
TovikEngine.injectPreloadCSS();
function initialize() {
    customElements.define('tovik-language', TovikLanguageElement);
    customElements.define('tovik-translate', TovikElement);
    customElements.define('kori-edit', KoriElement);
    TovikEngine.hi();
}
if (/complete|interactive|loaded/.test(document.readyState))
    initialize();
else
    window.addEventListener('DOMContentLoaded', () => initialize());
//# sourceMappingURL=main.js.map