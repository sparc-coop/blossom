import db from './KoriDb.js';
import SparcEngine from './SparcEngine.js';

export default class KoriTranslateNode extends HTMLElement {
    #original;
    #originalLang;
    #translated;
    #lang;

    constructor() {
        super();
    }

    connectedCallback() {
        this.#original = this.textContent.trim();
        this.#lang = this.lang || navigator.language;
        this.#originalLang = this.lang || document.documentElement.lang;

        document.addEventListener('kori-language-changed', this.#languageChangedCallback);
        this.askForTranslation();
    }

    disconnectedCallback() {
        document.removeEventListener('kori-language-changed', this.#languageChangedCallback);
    }

    static observedAttributes = ['lang'];

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === 'lang' && oldValue != newValue) {
            this.#lang = newValue || document.documentElement.lang || navigator.language;
            this.askForTranslation();
        }
    }

    #languageChangedCallback = (event: any) => {
        console.log('Language changed to:', event.detail);
        if (event.detail === this.#lang) return; // No change in language
        this.#lang = event.detail;
        this.askForTranslation();
    }

    askForTranslation() {
        const hash = SparcEngine.idHash(this.#original, this.#lang);
        db.translations.get(hash).then(translation => {
            if (translation) {
                this.render(translation);
            } else {
                this.classList.add('kori-translating');
                SparcEngine.translate(this.#original, this.#originalLang, this.#lang)
                    .then(newTranslation => {
                        this.render(newTranslation);
                        db.translations.put(newTranslation);
                    });
                this.classList.remove('kori-translating');
            }
        });
    }

    render(translation) {
        this.#translated = translation.text;

        if (this.#translated) {
            this.textContent = this.#translated;
        } else {
            this.textContent = this.#original;
        }
    }
}