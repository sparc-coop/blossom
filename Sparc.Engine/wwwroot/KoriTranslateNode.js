import db from './KoriDb.js';
import SparcEngine from './SparcEngine.js';
export default class KoriTranslateNode extends HTMLElement {
    #original;
    #originalLang;
    #translated;
    constructor() {
        super();
    }
    connectedCallback() {
        this.#original = this.textContent.trim();
        this.#originalLang = this.lang || document.documentElement.lang;
        document.addEventListener('kori-language-changed', this.#languageChangedCallback);
        this.askForTranslation();
    }
    disconnectedCallback() {
        document.removeEventListener('kori-language-changed', this.#languageChangedCallback);
    }
    #languageChangedCallback = (event) => {
        this.askForTranslation();
    };
    askForTranslation() {
        const hash = SparcEngine.idHash(this.#original);
        db.translations.get(hash).then(translation => {
            if (translation) {
                this.render(translation);
            }
            else {
                this.classList.add('kori-translating');
                SparcEngine.translate(this.#original, this.#originalLang)
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
        }
        else {
            this.textContent = this.#original;
        }
    }
}
//# sourceMappingURL=KoriTranslateNode.js.map