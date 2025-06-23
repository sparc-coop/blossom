export default class KoriTranslateElement extends HTMLElement {
    observer;
    #observedElement;
    
    constructor() {
        super();
    }

    connectedCallback() {
        this.#observedElement = this;
        
        // if the attribute 'for' is set, observe the element with that selector
        if (this.hasAttribute('for')) {
            const selector = this.getAttribute('for');
            this.#observedElement = document.querySelector(selector);
        }

        this.wrapTextNodes();

        // Observe changes in the DOM if the attribute 'live' is set
        if (this.hasAttribute('live')) {
            this.observer = new MutationObserver(this.#observer);
            this.observer.observe(this.#observedElement, { childList: true, characterData: true, subtree: true });
        }
    }

    disconnectedCallback() {
        if (this.observer)
            this.observer.disconnect();
    }

    wrapTextNodes() {
        var nodes = [];
        var treeWalker = document.createTreeWalker(this.#observedElement, NodeFilter.SHOW_TEXT, this.#koriIgnoreFilter);
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
            // wrap the text node in a KoriTranslateElement
            const wrapper = document.createElement('kori-t');
            wrapper.textContent = node.textContent;
            node.parentElement.replaceChild(wrapper, node);
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
            }
            else {
                mutation.addedNodes.forEach(this.wrapTextNode);
            }
        }
    }

    #koriIgnoreFilter = function (node) {
        var approvedNodes = ['#text'];

        if (!approvedNodes.includes(node.nodeName) || node.parentNode.nodeName == 'SCRIPT')
            return NodeFilter.FILTER_SKIP;

        var closest = node.parentElement.closest('.kori-ignore');
        if (closest)
            return NodeFilter.FILTER_SKIP;

        return NodeFilter.FILTER_ACCEPT;
    }
}
