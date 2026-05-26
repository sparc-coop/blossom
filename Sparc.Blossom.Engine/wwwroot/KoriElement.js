import BlossomEvents from './BlossomEvents.js';
import TovikEngine from './TovikEngine.js';
export default class KoriElement extends HTMLElement {
    potentialTarget;
    target;
    verticalBox;
    horizontalBox;
    iframe;
    mode;
    constructor() {
        super();
        // Lock the context of these functions to this class, so they can be added and removed as event listeners without losing the context of 'this'
        this.highlightTarget = this.highlightTarget.bind(this);
        this.beginEdit = this.beginEdit.bind(this);
        this.positionBoxes = this.positionBoxes.bind(this);
        this.setMode = this.setMode.bind(this);
    }
    async connectedCallback() {
        // Add 2 boxes to this custom element, to be positioned absolutely on top of the target element as bordered identifiers for the element
        this.verticalBox = document.createElement('div');
        this.verticalBox.classList.add('kori-box', 'kori-box-vertical');
        this.appendChild(this.verticalBox);
        this.horizontalBox = document.createElement('div');
        this.horizontalBox.classList.add('kori-box', 'kori-box-horizontal');
        this.appendChild(this.horizontalBox);
        // TODO: Pull auth code from query string, attach to iframe src, and save to local storage
        const urlParams = new URLSearchParams(window.location.search);
        const authCode = urlParams.get('_wauth');
        const domain = urlParams.get('_kori');
        this.iframe = document.createElement('iframe');
        this.iframe.classList.add('kori-iframe');
        this.iframe.src = `${TovikEngine.widgetUrl}/sites/${domain}/widget?_wauth=${authCode}`;
        this.appendChild(this.iframe);
        BlossomEvents.on('mode', (mode) => this.setMode(mode));
        BlossomEvents.on('bold', () => document.execCommand('bold'));
        BlossomEvents.on('italic', () => document.execCommand('italic'));
    }
    disconnectedCallback() {
    }
    textNode(element) {
        var textNodes = Array.from(element.childNodes).filter(node => node['nodeType'] === Node.TEXT_NODE && node['nodeValue'].trim() !== '');
        return textNodes.length == 1 ? textNodes[0] : null;
    }
    isEditable(element) {
        return this.textNode(element) !== null;
    }
    setMode(mode) {
        console.log('Setting mode to', mode);
        if (mode == 'Edit') {
            document.addEventListener('mouseover', this.highlightTarget);
            document.addEventListener('click', this.beginEdit);
            document.addEventListener('scroll', this.positionBoxes);
        }
        else if (this.mode == 'Edit') {
            document.removeEventListener('mouseover', this.highlightTarget);
            document.removeEventListener('click', this.beginEdit);
            document.removeEventListener('scroll', this.positionBoxes);
            this.markTarget(null);
        }
        this.mode = mode;
    }
    markTarget(element) {
        console.log('marking target', element);
        if (!element) {
            this.verticalBox.style.display = 'none';
            this.horizontalBox.style.display = 'none';
            if (this.potentialTarget)
                this.potentialTarget.classList.remove('kori-editable');
            this.potentialTarget = null;
        }
        else {
            this.potentialTarget = element;
            this.positionBoxes();
            this.potentialTarget.classList.add('kori-editable');
            var self = this;
            this.potentialTarget.addEventListener('mouseleave', function onMouseMove(event) {
                if (self.potentialTarget == event.target && self.target != event.target)
                    self.markTarget(null);
            });
        }
    }
    highlightTarget(event) {
        if (!this.potentialTarget && this.isEditable(event.target)) {
            this.markTarget(event.target);
            event.stopPropagation();
        }
    }
    positionBoxes() {
        if (!this.potentialTarget)
            return;
        const rect = this.potentialTarget.getBoundingClientRect();
        this.verticalBox.style.left = `${rect.left}px`;
        this.verticalBox.style.width = `${rect.width}px`;
        this.verticalBox.style.display = 'block';
        this.horizontalBox.style.top = `${rect.top}px`;
        this.horizontalBox.style.height = `${rect.height}px`;
        this.horizontalBox.style.display = 'block';
    }
    beginEdit(event) {
        if (!this.isEditable(event.target))
            return;
        event.preventDefault();
        if (this.target != event.target) {
            this.markTarget(event.target);
            this.target = event.target;
            this.target.contentEditable = true;
            this.target.focus();
            var el = this.target;
            this.target.addEventListener('blur', () => this.save(el), { once: true });
            event.stopPropagation();
        }
    }
    async save(element) {
        if (!element)
            return;
        var originalText = this.textNode(element)['originalText'];
        if (originalText != element.textContent.trim()) {
            const hash = TovikEngine.idHash(originalText);
            const request = {
                id: hash,
                Text: element.textContent.trim(),
                OriginalText: originalText,
                LanguageId: TovikEngine.userLang
            };
            BlossomEvents.broadcast(this.iframe, 'Save', request);
            await TovikEngine.update(hash);
        }
        element.contentEditable = false;
        element.classList.remove('kori-editable');
        if (this.target == element)
            this.target = null;
        if (this.potentialTarget == element)
            this.markTarget(null);
    }
    cancel() {
        if (!this.target)
            return;
        var originalText = this.textNode(this.target)['originalText'];
        this.target.innerText = originalText;
        this.target.contentEditable = false;
        this.target = null;
    }
}
//# sourceMappingURL=KoriElement.js.map