import BlossomEvents from './BlossomEvents.js';
import TovikEngine from './TovikEngine.js';
const debounce = (callback, wait) => {
    let timeoutId = null;
    return (...args) => {
        window.clearTimeout(timeoutId);
        timeoutId = window.setTimeout(() => {
            callback.apply(null, args);
        }, wait);
    };
};
export default class KoriElement extends HTMLElement {
    potentialTarget;
    target;
    verticalBox;
    horizontalBox;
    iframe;
    mode;
    debounceSave;
    constructor() {
        super();
        // Lock the context of these functions to this class, so they can be added and removed as event listeners without losing the context of 'this'
        this.highlightTarget = this.highlightTarget.bind(this);
        this.beginEdit = this.beginEdit.bind(this);
        this.positionBoxes = this.positionBoxes.bind(this);
        this.setMode = this.setMode.bind(this);
        this.format = this.format.bind(this);
        this.endEdit = this.endEdit.bind(this);
        this.save = this.save.bind(this);
        this.debounceSave = debounce(this.save, 500);
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
        BlossomEvents.on('bold', () => this.format('bold'));
        BlossomEvents.on('italic', () => this.format('italic'));
        BlossomEvents.on('saved', (item) => TovikEngine.update(item));
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
    format(command) {
        document.execCommand(command);
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
            this.endEdit();
            this.markTarget(event.target);
            this.target = event.target;
            this.target.contentEditable = true;
            this.target.focus();
            this.target.addEventListener('input', this.debounceSave);
            event.stopPropagation();
        }
    }
    async save() {
        if (!this.target)
            return;
        var originalText = this.textNode(this.target)['originalText'];
        const hash = TovikEngine.idHash(originalText);
        const request = {
            id: hash,
            Text: this.target.textContent.trim(),
            OriginalText: originalText,
            LanguageId: TovikEngine.userLang
        };
        BlossomEvents.broadcast(this.iframe, 'Save', request);
        this.target.isDirty = false;
    }
    endEdit() {
        if (this.target) {
            this.target.classList.remove('kori-editable');
            this.target.contentEditable = false;
            this.target.isDirty = false;
            this.target.removeEventListener('input', this.debounceSave);
            this.target.removeEventListener('blur', this.endEdit);
        }
        this.target = null;
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