import MD5 from "./MD5.js";
import db from './TovikDb.js';
import TovikLanguageElement from './TovikLanguageElement.js';
import TovikElement from './TovikElement.js';

function windowOrParentIncludes(str) {
    return window.location.href.includes(str)
        || (window.parent?.location && window.parent.location.href.includes(str));
}

const baseUrl = windowOrParentIncludes('localhost') ? 'https://localhost:7185'
    : windowOrParentIncludes('tovik-staging') ? 'https://sparcengine-staging-asdagffkefgheqfm.centralus-01.azurewebsites.net'
    : 'https://engine.sparc.coop';

export default class TovikEngine {
    static userLang;
    static documentLang;
    static detectedLang;
    static model;
    static sampleText;
    static isPreview;
    static rtlLanguages = ['ar', 'fa', 'he', 'ur', 'ps', 'ku', 'dv', 'yi', 'sd', 'ug'];

    static async getUserLanguage() {
        // If query parameter lang is set, use it
        const urlParams = new URLSearchParams(window.location.search);
        if (urlParams.has('lang')) {
            this.userLang = urlParams.get('lang');
            return this.userLang;
        }

        if (urlParams.has('plang')) {
            this.userLang = urlParams.get('plang');
            await localStorage.setItem('tovik-plang', this.userLang);
            this.isPreview = true;
            return this.userLang;
        }

        // Check for data-lang on the body element
        const htmlLang = document.body?.getAttribute('data-toviklang');
        if (htmlLang) {
            this.model = 'Live';
            this.userLang = htmlLang;
            window.addEventListener('message', async (event) => {
                var lang = event['data'];
                if (lang && lang.startsWith && lang.startsWith('tovik-lang')) {
                    lang = lang.split(':')[1];
                    await this.setLanguage(lang);
                } else if (event['data'] == 'tovik-forcereload') {
                    await db.translations.clear();
                    window.location.reload();
                }
            });

            return this.userLang;
        }

        if (this.userLang)
            return this.userLang;

        var tovikLang = await localStorage.getItem('tovik-plang');
        if (tovikLang) {
            this.userLang = tovikLang;
            this.isPreview = true;
        } else {
            this.userLang = navigator.language;
            //await localStorage.setItem('tovik-lang', this.userLang);
        }
        return this.userLang;
    }

    static async getLanguages() {
        return await this.fetch('translate/languages');
    }

    static async setLanguage(language) {
        if (this.userLang != language) {
            this.userLang = language;
            document.dispatchEvent(new CustomEvent('tovik-language-changed', { detail: this.userLang }));
        }

        document.dispatchEvent(new CustomEvent('tovik-language-set', { detail: this.userLang }));
        document.documentElement.lang = this.userLang;
        document.documentElement.setAttribute('dir', this.rtlLanguages.some(x => this.userLang.startsWith(x)) ? 'rtl' : 'ltr');
    }

    static injectPreloadCSS() {
        const style = document.createElement('style');
        style.textContent = 'html.tovik-initializing, html.tovik-initializing * { color: transparent !important; caret-color: transparent !important; }'
            + '.tovik-preview { position: fixed; bottom: 20px; right: 20px; z-index: 1000000; background-color: #1F5068; color: white; font-size: 16px; padding: 16px 24px; border-radius: 20px; cursor: pointer; display: flex; align-items: center; gap: 16px; }'
            + '.tovik-preview img { width: 36px; height: 36px; }';
        document.head.appendChild(style);
        document.documentElement.classList.add('tovik-initializing');
    }

    static async hi() {
        let lang = await this.getUserLanguage();
        this.documentLang = document.documentElement.lang;

        await this.setLanguage(lang);
        document.addEventListener('tovik-user-language-changed', async (event: CustomEvent) => {
            if (!this.isPreview)
                await this.setLanguage(event.detail);
        });

        customElements.define('tovik-language', TovikLanguageElement);
        customElements.define('tovik-translate', TovikElement);

        // If the document does not have a <tovik-translate> element, create one and point it to the body
        if (!document.querySelector('tovik-translate')) {
            var bodyElement = document.createElement('tovik-translate');
            bodyElement.setAttribute('for', 'html');

            document.head.appendChild(bodyElement);
        }
    }

    static async initBody() {
        if (this.isPreview) {
            let languageName = new Intl.DisplayNames([navigator.language], { type: 'language' }).of(this.userLang);
            var previewHtml = `<div class="tovik-preview" translate="no" onclick="document.dispatchEvent(new CustomEvent('tovik-exit-preview'))"><img src="https://tovik.app/images/TovikChar.svg" /> ${languageName} <span>✕</span></div>`;
            document.body.insertAdjacentHTML('beforeend', previewHtml);
            document.addEventListener('tovik-exit-preview', this.exitPreview);
        }
    }
    
    static isRegisteringVisit = false;
    static async registerVisit() {
        if (this.isRegisteringVisit || !this.sampleText || this.sampleText.length < 100)
            return;

        this.isRegisteringVisit = true;

        this.fetch('translate/visit', {
            LanguageId: this.documentLang,
            Language: { Id: this.documentLang },
            Text: this.sampleText.substring(0, 1000)
        }).then(x => {
            this.detectedLang = x.id;
            this.isRegisteringVisit = false;
        });
    }

    static async exitPreview() {
        await localStorage.removeItem('tovik-plang');
        window.location.href = window.location.pathname;
    }
    
    static idHash(text, lang = null) {
        if (!lang)
            lang = this.userLang;

        return MD5(text.trim() + ':' + lang);
    }

    static async stream(pendingTranslations, textMap, fromLang, onTranslation) {
        if (!pendingTranslations.length)
            return;

        const uniqueMap = new Map();
        for (const item of pendingTranslations) {
            if (!uniqueMap.has(item.hash))
                uniqueMap.set(item.hash, { hash: item.hash, text: textMap(item.element) });
        }

        const requests = Array.from(uniqueMap.values()).map(item => TovikEngine.toRequest(item, fromLang));

        if (!this.userLang) {
            await this.getUserLanguage();
        }

        var result = await this.fetch('translate/stream', { content: requests, options: { additionalContext: document.body.innerText } }, this.userLang);
        if (result.continuationToken) {
            var source = new EventSource(`${baseUrl}/translate/stream/${result.continuationToken}`);
            source.addEventListener('done', () => source.close());
            source.addEventListener('ContentTranslated', (event) => {
                var translation = JSON.parse(event.data).data.translatedContent;
                this.replace(pendingTranslations, translation, onTranslation);
            });
        }

        for (let translation of result.content)
            this.replace(pendingTranslations, translation, onTranslation);
    }

    static replace(pendingTranslations, translation, onTranslation) {
        const items = pendingTranslations.filter(item => item.hash === translation.id);
        for (let item of items)
            onTranslation(item.element, translation);

        db.translations.put(translation);
    }

    static getWindowedSample(firstItem, lastItem, totalChars) {
        if (!this.sampleText)
            return '';

        var text = this.sampleText;

        const firstItemIndex = text.indexOf(firstItem.text);
        const lastItemIndex = text.indexOf(lastItem.text);
        const numSamples = firstItemIndex > -1 && lastItemIndex > -1
            ? lastItemIndex - firstItemIndex > totalChars ? 2 : 1
            : firstItemIndex > -1 || lastItemIndex > -1 ? 1
                : 0;
        let sample;

        if (numSamples === 0) {
            sample = text.substring(0, totalChars);
        } else if (numSamples == 2) {
            const firstStartIndex = Math.max(0, firstItemIndex - totalChars / 4);
            const firstEndIndex = Math.min(text.length, firstItemIndex + totalChars / 4);
            const lastStartIndex = Math.max(0, lastItemIndex - totalChars / 4);
            const lastEndIndex = Math.min(text.length, lastItemIndex + totalChars / 4);
            sample = text.substring(firstStartIndex, firstEndIndex) + text.substring(lastStartIndex, lastEndIndex);
        } else {
            var index = firstItemIndex > -1 ? firstItemIndex : lastItemIndex;
            let start = Math.max(0, index - totalChars / 2);
            let end = Math.min(text.length, index + totalChars / 2);

            // ensure we get as close to totalChars as possible
            if (end - start < totalChars) {
                start = Math.max(0, end - totalChars);
                end = Math.min(text.length, start + totalChars);
            }

            sample = text.substring(start, end);
        }

        return sample;
    }

    static toRequest(item, fromLang) {
        return {
            id: item.hash || this.idHash(item.text, fromLang),
            LanguageId: fromLang,
            Language: { Id: fromLang },
            Text: item.text
        };
    };

    static async fetch(url: string, body: any = null, language: string = null) {
        const options: any = {
            credentials: 'include',
            method: body ? 'POST' : 'GET',
            headers: new Headers(),
            referrerPolicy: 'no-referrer-when-downgrade'
        };

        if (body) {
            if (this.model)
                body.model = this.model;

            options.headers.append('Content-Type', 'application/json');
            options.body = JSON.stringify(body);
        }

        if (language) {
            options.headers.append('Accept-Language', language);
        }

        const response = await fetch(`${baseUrl}/${url}`, options);

        if (response.ok)
            return await response.json();
        else if (response.status === 429) {
            console.warn(`Tovik tried to translate your website into ${language}, but your site has reached the Tovik translation limit!`);
        }
        else {
            console.error(`Tovik was unable to translate part of your website. Contact Tovik support to assist: Error code ${response.status}`);
        }
    }
}