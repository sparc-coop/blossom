import db from './KoriDb.js';
import MD5 from './MD5.js';
export default class KoriTranslateElement extends HTMLElement {
    observer;
    #observedElement;
    #mode;
    #originalLang;
    #originalText = {};
    #lang;
    constructor() {
        super();
    }
    connectedCallback() {
        this.#observedElement = this;
        this.#mode = 'static';
        this.#originalLang = this.lang || document.documentElement.lang;
        this.#lang = navigator.language;
        // if the attribute 'for' is set, observe the element with that selector
        if (this.hasAttribute('for')) {
            const selector = this.getAttribute('for');
            this.#observedElement = document.querySelector(selector);
        }
        if (this.hasAttribute('live')) {
            this.#mode = 'live';
        }
        this.wrapTextNodes(this.#observedElement);
        if (this.#mode === 'live') {
            console.log('observing', this.#observedElement);
            this.observer = new MutationObserver(this.#observer);
            this.observer.observe(this.#observedElement, { childList: true, characterData: true, subtree: true });
        }
    }
    disconnectedCallback() {
        if (this.observer)
            this.observer.disconnect();
    }
    wrapTextNodes(element) {
        var nodes = [];
        var treeWalker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT, this.#koriIgnoreFilter);
        while (treeWalker.nextNode()) {
            const node = treeWalker.currentNode;
            if (this.isValid(node)) {
                nodes.push(node);
            }
        }
        nodes.forEach(node => this.wrapTextNode(node));
    }
    wrapTextNode(node) {
        if (this.isValid(node)) {
            if (this.#mode === 'live') {
                if (!node.hash) {
                    var text = node.textContent.trim();
                    node.hash = MD5(text + ':' + this.#originalLang);
                    this.#originalText[node.hash] = text;
                    document.addEventListener('kori-language-changed', (event) => {
                        this.#lang = event.detail;
                        this.askForTranslation(node);
                    });
                    console.log('live node registered', node.textContent);
                }
                this.askForTranslation(node);
            }
            else if (this.#mode === 'static') {
                const wrapper = document.createElement('kori-t');
                wrapper.textContent = node.textContent.trim();
                node.parentElement.replaceChild(wrapper, node);
            }
        }
    }
    isValid(node) {
        return node
            && node.textContent
            && node.textContent.trim()
            && !(node.parentElement && node.parentElement.tagName === 'KORI-T');
    }
    #observer = mutations => {
        for (let mutation of mutations) {
            if (mutation.target.classList?.contains('kori-ignore')
                || mutation.target.parentElement?.classList.contains('kori-ignore'))
                return;
            if (mutation.type == 'characterData') {
                console.log('Character data mutation', mutation.target);
            }
            else if (mutation.type == 'childList') {
                console.log('Mutation childList', mutation.target);
                this.wrapTextNodes(mutation.target);
            }
            else {
                mutation.addedNodes.forEach(this.wrapTextNode);
            }
        }
    };
    #koriIgnoreFilter = function (node) {
        var approvedNodes = ['#text'];
        if (!approvedNodes.includes(node.nodeName) || node.parentNode.nodeName == 'SCRIPT')
            return NodeFilter.FILTER_SKIP;
        var closest = node.parentElement.closest('.kori-ignore');
        if (closest)
            return NodeFilter.FILTER_SKIP;
        return NodeFilter.FILTER_ACCEPT;
    };
    askForTranslation(textNode) {
        const hash = MD5(textNode.textContent.trim() + ':' + this.#lang);
        db.translations.get(hash).then(translation => {
            if (translation) {
                console.log('translation found!', hash, textNode, this.#lang);
                textNode.textContent = translation.text;
            }
            else {
                const request = {
                    id: textNode.hash,
                    Domain: window.location.host,
                    LanguageId: this.#originalLang,
                    Language: { Id: this.#originalLang },
                    Text: this.#originalText[textNode.hash]
                };
                console.log('translation not found???', hash, textNode, this.#lang);
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
                            textNode.textContent = newTranslation.text;
                            db.translations.put(newTranslation);
                            console.log('translation loaded', hash, newTranslation);
                        });
                    }
                });
            }
        });
    }
}
//# sourceMappingURL=KoriTranslateElement.js.map