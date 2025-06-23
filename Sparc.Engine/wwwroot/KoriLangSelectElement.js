import db from './KoriDb.js';
class KoriLangSelectElement extends HTMLElement {
    #select;
    #languages;
    #lang;
    constructor() {
        super();
    }
    connectedCallback() {
        this.#lang = this.lang || document.documentElement.lang;
        this.#select = document.createElement('select');
        this.getLanguages();
    }
    getLanguages() {
        db.languages.toArray().then(languages => {
            if (languages.length > 0) {
                this.renderLanguages(languages);
            }
            else {
                fetch('https://localhost:7185/translate/languages').then(response => {
                    if (response.ok) {
                        response.json().then(languages => {
                            this.renderLanguages(languages);
                            db.languages.bulkPut(languages);
                        });
                    }
                });
            }
        });
    }
    renderLanguages(languages) {
        this.#languages = languages;
    }
}
customElements.define('kori-langselect', KoriLangSelectElement);
//# sourceMappingURL=KoriLangSelectElement.js.map