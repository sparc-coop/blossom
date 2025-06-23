import db from './KoriDb.js';
import SparcEngine from './SparcEngine.js';
export default class KoriLangSelectElement extends HTMLElement {
    constructor() {
        super();
    }
    connectedCallback() {
        this.getLanguages();
    }
    getLanguages() {
        db.languages.toArray().then(languages => {
            if (languages.length > 0) {
                this.renderLanguages(languages);
            }
            else {
                SparcEngine.getLanguages().then(languages => {
                    this.renderLanguages(languages);
                    db.languages.bulkPut(languages);
                });
            }
        });
    }
    renderLanguages(languages) {
        this.innerHTML = '';
        let select = document.createElement('select');
        select.className = 'kori-ignore';
        languages.forEach(lang => {
            const option = document.createElement('option');
            option.value = lang.id;
            option.textContent = lang.nativeName;
            if (lang.id === SparcEngine.userLang) {
                option.selected = true;
            }
            select.appendChild(option);
        });
        select.addEventListener('change', () => {
            SparcEngine.setLanguage(select.value);
        });
        this.appendChild(select);
    }
}
//# sourceMappingURL=KoriLangSelectElement.js.map