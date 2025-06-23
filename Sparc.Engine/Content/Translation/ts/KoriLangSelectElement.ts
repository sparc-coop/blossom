import db from './KoriDb.js';

export default class KoriLangSelectElement extends HTMLElement {
    #lang;

    constructor() {
        super();
    }

    connectedCallback() {
        this.#lang = this.lang || navigator.language;
        this.getLanguages();
    }

    getLanguages() {
        db.languages.toArray().then(languages => {
            if (languages.length > 0) {
                this.renderLanguages(languages);
            } else {
                fetch('https://engine.sparc.coop/translate/languages', {
                    credentials: 'include'
                }).then(response => {
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
        this.innerHTML = '';
        let select = document.createElement('select');
        select.className = 'kori-ignore';

        languages.forEach(lang => {
            const option = document.createElement('option');
            option.value = lang.id;
            option.textContent = lang.nativeName;
            if (lang.id === this.#lang) {
                option.selected = true;
            }
            select.appendChild(option);
        });

        select.addEventListener('change', () => {
            this.#lang = select.value;
            document.documentElement.lang = this.#lang;
            document.dispatchEvent(new CustomEvent('kori-language-changed', { detail: this.#lang }));
        });

        this.appendChild(select);
    }
}
