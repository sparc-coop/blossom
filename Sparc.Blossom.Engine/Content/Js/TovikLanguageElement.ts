import db from './TovikDb.js';
import TovikEngine from './TovikEngine.js';

export default class TovikLanguageElement extends HTMLElement {
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
            } else {
                TovikEngine.getLanguages().then(languages => {
                    this.renderLanguages(languages);
                    db.languages.bulkPut(languages);
                });
            }
        });
    }

    renderLanguages(languages) {
        this.innerHTML = '';
        let select = document.createElement('select');
        select.translate = false;

        languages.forEach(lang => {
            const option = document.createElement('option');
            option.value = lang.id;
            option.textContent = lang.nativeName;
            if (lang.id === TovikEngine.userLang) {
                option.selected = true;
            }
            select.appendChild(option);
        });

        document.addEventListener('tovik-language-set', async (event:any) => {
            // select the language if it exists in the select options
            if (languages.some(lang => lang.id === event.detail))
                select.value = event.detail;
        }); 

        select.addEventListener('change', () => {
            document.dispatchEvent(new CustomEvent('tovik-user-language-changed', { detail: select.value }));
        });

        this.appendChild(select);
    }
}
