import db from './KoriDb.js';
import MD5 from './MD5.js';
import KoriTranslator from './KoriTranslator.js';

class KoriTranslateElement extends HTMLElement {
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
        console.log('kori connected!', this.#original, this.#lang);
        this.askForTranslation();
    }

    get originalHash() { return MD5(this.#original + ':' + this.#originalLang); }

    get hash() { return MD5(this.#original + ':' + this.#lang); } 

    static observedAttributes = ['lang'];
    
    attributeChangedCallback(name, oldValue, newValue) {
        if (name === 'lang' && oldValue != newValue) {
            this.#lang = newValue || document.documentElement.lang || navigator.language;
            this.askForTranslation();
        }
    }

    askForTranslation() {
        console.log('oh lawd', this.#original, this.#lang, this.hash);
        db.translations.get(this.hash).then(translation => {
            if (translation) {
                this.render(translation);
            } else {
                const request = {
                    id: this.originalHash,
                    Domain: window.location.host,
                    LanguageId: this.#originalLang,
                    Language: { Id: this.#originalLang },
                    Text: this.#original
                };

                console.log('OH LAWD', request);

                fetch('https://localhost:7185/translate', {
                    method: 'POST',
                    body: JSON.stringify(request),
                    headers: {
                        'Accept-Language': this.#lang,
                        'Content-Type': 'application/json'
                    }
                }).then(response => {
                    if (response.ok) {
                        response.json().then(newTranslation => {
                            console.log('OH LAWD!!!!!', newTranslation);
                            this.render(newTranslation);
                            db.translations.put(newTranslation);
                        });
                    }
                });
            }
        })
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

customElements.define('kori-translate', KoriTranslateElement);
document.addEventListener('DOMContentLoaded', () => new KoriTranslator().init(document.body));
