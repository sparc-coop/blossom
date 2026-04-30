import TovikEngine from './TovikEngine.js';
TovikEngine.injectPreloadCSS();
if (/complete|interactive|loaded/.test(document.readyState))
    TovikEngine.hi();
else
    window.addEventListener('DOMContentLoaded', () => TovikEngine.hi());
//# sourceMappingURL=main.js.map