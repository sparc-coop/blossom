let translationCache = {};
let dotNet = {};
let app = {};
let observer = {};
let language = getBrowserLanguage();
var koriAuthorized = false;
let topBar = {};
let activeNode = null, activeMessageId = null;

let koriIgnoreFilter = function (node) {
    var approvedNodes = ['#text', 'IMG'];

    if (!approvedNodes.includes(node.nodeName) || node.parentNode.nodeName == 'SCRIPT' || node.koriTranslated == language)
        return NodeFilter.FILTER_SKIP;

    var closest = node.parentElement.closest('.kori-ignore');
    if (closest)
        return NodeFilter.FILTER_SKIP;

    return NodeFilter.FILTER_ACCEPT;
}

function init(targetElementId, selectedLanguage, dotNetObjectReference, serverTranslationCache) {
    
    language = selectedLanguage;
    dotNet = dotNetObjectReference;

    buildTranslationCache(serverTranslationCache);

    initKoriElement(targetElementId);

    window.addEventListener("click", e => {
        e.stopImmediatePropagation();
        mouseClickHandler(e);
    });

    initKoriTopBar();
}

function buildTranslationCache(serverTranslationCache) {
    if (serverTranslationCache) {
        translationCache = serverTranslationCache;
        for (let key in translationCache)
            translationCache[key].Nodes = [];
    }
    else {
        for (let key in translationCache) {
            translationCache[key].Submitted = false;
            translationCache[key].Translation = null;
            translationCache[key].Nodes = [];
        }
    }

    console.log('Kori translation cache initialized, ', translationCache);
}

function initKoriElement(targetElementId) {
    if (/complete|interactive|loaded/.test(document.readyState)) {
        initElement(targetElementId);
    } else {
        window.addEventListener('DOMContentLoaded', () => initElement(targetElementId));        
    }
}

function initKoriTopBar() {
    topBar = document.getElementById("kori-top-bar");

    console.log('Kori top bar initialized.');
}

function initElement(targetElementId) {
    console.log("Initializing element");

    app = document.getElementById(targetElementId);

    registerNodesUnder(app);
    translateNodes();    

    observer = new MutationObserver(observeCallback);
    observer.observe(app, { childList: true, characterData: true, subtree: true });

    console.log('Observer registered for ' + targetElementId + '.');

    addTargetTextStyle();
}

function registerNodesUnder(el) {
    var n, walk = document.createTreeWalker(el, NodeFilter.SHOW_TEXT | NodeFilter.SHOW_ELEMENT, koriIgnoreFilter);
    while (n = walk.nextNode()){
        registerNode(n);
    }
}

function registerNode(node) {
    if (node.koriRegistered == language || node.koriTranslated == language)
        return;

    var content = node.nodeName == 'IMG' ? node.src.trim() : node.textContent.trim();

    var tag = getTagContent(node) ?? (node.koriContent ?? content.trim());

    if (!tag)
        return;

    node.koriRegistered = language;
    node.koriContent = tag;
    node.parentElement?.classList.add('kori-initializing');

    if (tag in translationCache && translationCache[tag].Nodes.indexOf(node) < 0) {
        translationCache[tag].Nodes.push(node);

        if (translationCache[tag].id !== undefined && node.nodeName == '#text') {
            node.parentElement.setAttribute('kori-id', translationCache[tag].id);
        }

    } else {
        translationCache[tag] = {
            Nodes: [node],
            Translation: null
        };
    }
}

function getTagContent(node) {
    var tagContent = node.parentElement?.getAttribute('data-tag');
    if (tagContent) {
        return tagContent.trim();
    }
    return null;
}

function observeCallback(mutations) {
    console.log("Observe callback", mutations);

    mutations.forEach(function (mutation) {
        if (mutation.target.classList?.contains('kori-ignore') || mutation.target.parentElement?.classList.contains('kori-ignore'))
            return;

        if (mutation.type == 'characterData') {
            console.log('Character data mutation', mutation.target);
            registerNode(mutation.target, NodeType.TEXT);
        }
        else if (mutation.type == 'childList'){
            console.log('Mutaton childList');
        }
        else {
            mutation.addedNodes.forEach(registerNodesUnder);
        }
        
        translateNodes();
    });
}

function translateNodes() {
    console.log('translateNodes');

    var contentToTranslate = {};

    for (let key in translationCache) {
        if (!translationCache[key].Submitted && !translationCache[key].Translation) {
            translationCache[key].Submitted = true;

            let tag = key;

            translationCache[key].Nodes.forEach(node => {
                let text = node.textContent || "";

                if (isPlaceholder(text)) {
                    contentToTranslate[tag] = "";
                } else {
                    if (text.length > 0) {
                        contentToTranslate[tag] = text;
                    }
                }
            });
        }
    }

    console.log('translateAsync', contentToTranslate);

    dotNet.invokeMethodAsync("TranslateAsync", contentToTranslate).then(translations => {
        console.log('Received new translations', translations);

        for (var key in translations) {
            if (translations[key] === "") {
                translations[key] = " ";
            }
            translationCache[key].Translation = translations[key];
        }

        replaceWithTranslatedText();
    });
}

function isPlaceholder(text) {
    const placeholders = [
        "Type your title here",
        "Author Name",
        "Type blog post content here."
    ];

    return placeholders.includes(text);
}

function replaceWithTranslatedText() {
    observer.disconnect();

    console.log('replaceWithTranslatedText - translationCache', translationCache);

    for (let key in translationCache) {
        var translation = translationCache[key];

        //console.log('translation', translation);

        if (!translation.Translation)
            continue;

        for (let node of translation.Nodes) {
            // if the node is an img, replace the src attribute
            if (node.nodeName == 'IMG') {
                node.src = translation.Translation;
                node.koriTranslated = language;
            } else if (node.textContent != translation.Translation) {

                if (translation.text != undefined) {
                    node.textContent = translation.text || "";
                }
                node.koriTranslated = language;
            }

            node.parentElement?.classList.remove('kori-initializing');
            node.parentElement?.classList.add('kori-enabled');

            if (node.textContent.trim() == "") {
                node.parentElement?.classList.add('empty-content');
            }

            if (node.nodeName == '#text' && translation.html && node.parentElement) {
                node.parentElement.innerHTML = translation.html;
            }
        }
    }

    console.log('Translated page from Ibis and enabled Kori top bar.');

    observer.observe(app, { childList: true, characterData: true, subtree: true });
}

function getBrowserLanguage() {
    var lang = (navigator.languages && navigator.languages.length) ? navigator.languages[0] :
        navigator.userLanguage || navigator.language || navigator.browserLanguage || 'en';
    return lang.substring(0, 2);
}

let playAudio = function (url) {
    const sound = new Howl({
        src: [url]
    });
    sound.play();
}

// mouse click handler for kori widget and elements
function mouseClickHandler(e) {
    var t = e.target;

    // click login menu
    //if (t.closest(".kori-login__btn")) {
    //    koriAuthorized = true;
    //    if (koriAuthorized) {
    //        document.getElementById("kori-login").classList.remove("show");
    //        document.body.classList.add("kori-loggedin"); // add the class to <body>            
    //    }
    //}

    if (t.closest(".kori-login__tab")) {
        if (tabsParent && tabs) {
            if (tabs.length > 0) {
                tabs.forEach((tab) => {
                    // Remove active class
                    tabs.forEach((t, i) => {
                        t.classList.remove("active");
                    });
                })

                // Add active class to clicked tab
                t.classList.add("active");
                updateActiveIndicator(t);
            }
        }
        return;
    }

    // click kori enabled elements  
    if (t.closest(".kori-enabled")) {
        toggleSelected(t);
        //showTopBar(t);
        return;
    }

    toggleSelected(t);
}

// global login - mobile UI, tabs sliding active indicator

var tabsParent = document.getElementById("kori-login__tabs");
var tabs = document.querySelectorAll(".kori-login__tab");

function updateActiveIndicator(activeElement) {
    const tabsParentLeftDistance = tabsParent.getBoundingClientRect().left;
    console.log("tabsParentLeftDistance: " + tabsParentLeftDistance);

    const {
        width: elementSize,
        left: elementLeftDistance,
    } = activeElement.getBoundingClientRect();

    const distanceFromParent = elementLeftDistance - tabsParentLeftDistance;
    console.log("distancefromParent: " + distanceFromParent);
    console.log("elementSize: " + elementSize);

    tabsParent.style.setProperty("--indicator-offset", distanceFromParent + "px");
    tabsParent.style.setProperty("--indicator-width", elementSize + "px");
}

// selecting and unselecting kori-enabled elements
function toggleSelected(t) {
    var koriElem = t.closest(".kori-enabled");
    console.log("koriElem", koriElem);

    var topBar = document.getElementById("kori-top-bar");
    if (topBar && topBar.contains(t)) {
        return;
    }

    var koriContent = document.querySelector(".kori-content");

    if (!koriElem) {
        document.querySelector(".selected")?.classList.remove("selected");
        cancelEdit();
        activeNode = null;        
        dotNet.invokeMethodAsync("SetDefaultMode");
      
        if (koriContent) {
            koriContent.style.cursor = "default"; 
        }
        
        return;
    }

    var selectedElem = document.querySelector(".selected");

    if (selectedElem && selectedElem !== koriElem) {
        selectedElem.classList.remove("selected");
    }

    if (!koriElem.classList.contains("selected")) {
        koriElem.classList.add("selected");

        dotNet.invokeMethodAsync("EditAsync");  

        showTopBar(koriElem);

        if (koriContent) {
            koriContent.style.cursor = "pointer";
        }
    }
}

// showing kori top bar
function showTopBar(t) {
    var topBar = document.getElementById("kori-top-bar");

    document.body.appendChild(topBar);

    topBar.classList.add("show");

    if (topBar.classList.contains("show")) {
        // adjusts the top margin to match the top-bar height
        document.body.style.marginTop = '84px';
    }

    const koriId = t.getAttribute('kori-id');
    // search for matching node in translation cache
    for (let key in translationCache) {
        for (var i = 0; i < translationCache[key].Nodes.length; i++)

            if (t.contains(translationCache[key].Nodes[i])) {
                activeNode = translationCache[key].Nodes[i];
                activeMessageId = key;
                break;
            }

        if (koriId != null && koriId == translationCache[key].id) {
            activeNode = t;
            activeMessageId = key;
            break;
        }
    }

    console.log('Set active node', activeNode);
}

function getActiveImageSrc() {
    if (activeNode && activeNode.tagName === 'IMG') {
        return activeNode.src;
        //console.log('Active node is an image', activeNode.src);
    }

    //console.log('Active node is not an image', activeNode)
    return null;
}

function edit() {
    if (!activeNode) {
        console.log('Unable to edit element', activeNode);
        return;
    }

    var translation = translationCache[activeMessageId];

    if (isTranslationAlreadySaved(translation)) {
        var activeNodeParent = getActiveNodeParentByKoriId(translation);
        activateNodeEdition(activeNodeParent);
        replaceInnerHtmlBeforeTopBar(activeNodeParent, getTranslationRawMarkdownText(translation));
    }
    else {
        var parentElement = activeNode.parentElement;
        activateNodeEdition(parentElement);

        // If the Translation is empty or contains only whitespace
        if (!translation || !translation.Translation || translation.Translation.trim() === "") {

            // Insert a temporary space to ensure the cursor appears
            activeNode.textContent = "\u200B"; // Zero-width space character
        } else {
            activeNode.textContent = getTranslationRawMarkdownText(translation);
        }
    }

    document.getElementById("kori-top-bar").contentEditable = "false";
}

function getTranslationRawMarkdownText(translation) {
    return translation.text ?? translation.Translation;
}

function activateNodeEdition(node) {
    node.classList.add('kori-ignore');
    node.contentEditable = "true";
    node.focus();
}

function deactivateNodeEdition(node, translation) {
    node.contentEditable = "false";
    node.classList.remove('kori-ignore');
    node.classList.remove('selected');

    node.innerHTML = translation.html;
}

function getActiveNodeParentByKoriId(translation) {
    return document.querySelector(`[kori-id="${translation.id}"]`);
}

function isTranslationAlreadySaved(translation) {
    return translation.id;
}

function replaceInnerHtmlBeforeTopBar(node, markdownTxt) {
    if (node.firstChild && node.firstChild.nodeType === Node.TEXT_NODE && node.firstChild.nodeValue === markdownTxt) {
        return;
    }

    if (node.firstChild && node.firstChild.nodeType !== Node.TEXT_NODE) {
        node.removeChild(node.firstChild);
    }

    node.textContent = markdownTxt;

    node.contentEditable = "true";
}

function editImage() {
    console.log("Entered the edit image function");
}

function showSidebar() {
    document.body.style.marginRight = "317px";

    var topBar = document.getElementById('kori-top-bar');
    topBar.style.width = "calc(100% - 317px)";
}

function hideSidebar() {
    console.log("hide sidebar function");
    document.body.style.marginRight = "0px";

    var topBar = document.getElementById('kori-top-bar');
    topBar.style.width = "100%";
}

function cancelEdit() {
    var translation = translationCache[activeMessageId];

    if (isTranslationAlreadySaved(translation)) {
        var activeNodeParent = document.querySelector(`[kori-id="${translation.id}"]`);
        deactivateNodeEdition(activeNodeParent, translation);
    } else {
        activeNode.textContent = translation.Translation;
        activeNode.parentElement.contentEditable = "false";
        activeNode.parentElement.classList.remove('selected');
    }
}

function closeSearch() {
    console.log("close search function");

    var searchSidebar = document.getElementById('kori-search');
    if (searchSidebar) {
        searchSidebar.classList.remove('show');
    }

    hideSidebar();
}

function save() {
    if (!activeNode)
        return;

    var translation = translationCache[activeMessageId];
    var textContent = activeNode.textContent;
    var tagContent = translation.tag;
    
    dotNet.invokeMethodAsync("SaveAsync", translation.id, tagContent, textContent).then(content => {
        console.log('Saved new content.', content);

        translationCache[activeMessageId].Translation = content.text;
        translationCache[activeMessageId].tag = content.tag;
        translationCache[activeMessageId].text = content.text;
        translationCache[activeMessageId].html = content.html;

        activeNode.parentElement.contentEditable = "false";
        activeNode.parentElement.classList.remove('kori-ignore');

        if (translation.id) {

            var activeNodeParent = document.querySelector(`[kori-id="${translation.id}"]`);
            deactivateNodeEdition(activeNodeParent, translation);

        } else {
            translationCache[activeMessageId].id = content.id;
            activeNode.parentElement?.setAttribute('kori-id', content.id);
        }
        
        dotNet.invokeMethodAsync("SetDefaultMode");

    });
    
}

function isDescendantOfClass(element, className) {
    while (element) {
        if (element.classList && element.classList.contains(className)) {
            return true;
        }
        element = element.parentElement;
    }
    return false;
}

function checkSelectedContentType() {
    var selectedElement = document.getElementsByClassName("selected")[0];

    if (!selectedElement) {
        return "none";
    }

    if (selectedElement.tagName.toLowerCase() === 'img') {
        return "image";
    }

    // checks if the selected element has the classes 'kori-enabled' and 'selected'
    if (selectedElement.classList.contains('kori-enabled') && selectedElement.classList.contains('selected')) {
        var imgChildren = selectedElement.querySelectorAll('img');

        // Iterates over the child images to check for the presence of 'kori-ignore'
        for (var img of imgChildren) {
            // checks if the image is not inside a parent element with class 'kori-ignore'
            if (!isDescendantOfClass(img, 'kori-ignore')) {
                return "image";
            }
        }
    }

    // if it is not an image, assume it is text
    return "text";
}

// show and hide language menu
function toggleLanguage(isOpen) {
    console.log("opening language menu");
    var language = document.getElementById("kori-language");

    if (!language.classList.contains("show") && isOpen == true) {
        language.classList.add("show");
    }

    if (language.classList.contains("show") && isOpen == false) {
        language.classList.remove("show");
    }
}

// login to use kori services
function login() {
    console.log("logging in...");
}

function applyMarkdown(symbol, position) {
    
    const selectedText = window.getSelection().toString();
    
    if (selectedText) {

        var newText = selectedText;

        if (position == "wrap") {
            newText = symbol + newText + symbol;
        } else if (position == "before") {
            newText = symbol + newText;
        }

        insertMarkdownText(newText);
    }
}

function insertMarkdownText(newText) {
    const selection = window.getSelection();
    if (!selection.rangeCount) return;

    const range = selection.getRangeAt(0);
    range.deleteContents();
    range.insertNode(document.createTextNode(newText));

    range.setStartAfter(range.endContainer);
    range.collapse(true);
    selection.removeAllRanges();
    selection.addRange(range);
}

function updateImageSrc(currentSrc, newSrc) {
    var img = document.querySelector(`img[src="${currentSrc}"]`);

    if (img) {
        img.src = newSrc;
        translationCache[activeMessageId].text = newSrc;
        console.log("Image src updated", img);
    }
}

function resetState() {
    translationCache = {};
    language = getBrowserLanguage();
    koriAuthorized = false;
    topBar = {};
    activeNode = null;
    activeMessageId = null;

    var koriElements = document.getElementsByClassName('kori-content');

    if (koriElements.length > 0)
        var elementId = koriElements[0].id;

    var app = document.getElementById(elementId);

    if (observer instanceof MutationObserver) {
        observer.disconnect();
    }

    if (app instanceof Node) {
        observer = new MutationObserver(observeCallback);
        observer.observe(app, { childList: true, characterData: true, subtree: true });
    } else {
        console.warn("'app' element not found in DOM.");
    }

    console.log('State reset');
}

function observeUrlChange() {
    let oldHref = document.location.href;
    const body = document.querySelector('body');
    const observer = new MutationObserver(mutations => {
        if (oldHref !== document.location.href) {
            oldHref = document.location.href;
            console.log("URL changed to:", oldHref);
            resetState();
        }
    });
    observer.observe(body, { childList: true, subtree: true });
};

window.addEventListener("load", observeUrlChange());

function addTargetTextStyle() {
    const style = document.createElement('style');
    style.innerHTML = `
    ::target-text {
        background-color: rebeccapurple !important;
        color: white !important;
        font-weight: bold !important;
    }
    `;
    document.head.appendChild(style);
}

export { init, replaceWithTranslatedText, getBrowserLanguage, playAudio, edit, cancelEdit, save, checkSelectedContentType, editImage, applyMarkdown, getActiveImageSrc, updateImageSrc, showSidebar, closeSearch };
