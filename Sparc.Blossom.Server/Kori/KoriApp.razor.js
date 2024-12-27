let app = {};
let observer = {};
var koriAuthorized = false;
let topBar = {};

let koriIgnoreFilter = function (node) {
    var approvedNodes = ['#text', 'IMG'];

    if (!approvedNodes.includes(node.nodeName) || node.parentNode.nodeName == 'SCRIPT')
        return NodeFilter.FILTER_SKIP;

    var closest = node.parentElement.closest('.kori-ignore');
    if (closest)
        return NodeFilter.FILTER_SKIP;

    return NodeFilter.FILTER_ACCEPT;
}

export async function init(appId) {
    if (/complete|interactive|loaded/.test(document.readyState)) {
        initApp(appId);
    } else {
        window.addEventListener('DOMContentLoaded', () => initApp(appId));
    }

    //window.addEventListener("click", e => {
    //    e.stopImmediatePropagation();
    //    mouseClickHandler(e);
    //});

    topBar = document.getElementById("kori-top-bar");
    return getBrowserLanguage();
}

function initApp(appId) {
    console.log("Initializing element", appId);

    app = document.getElementById(appId);
    registerNodesUnder(app);

    observer = new MutationObserver(observeCallback);
    observer.observe(app, { childList: true, characterData: true, subtree: true });

    console.log('Observer registered for ' + appId + '.');
}

function registerNodesUnder(el) {
    var nodes = [];
    var content;
    var n, walk = document.createTreeWalker(el, NodeFilter.SHOW_TEXT | NodeFilter.SHOW_ELEMENT, koriIgnoreFilter);
    while (n = walk.nextNode()) {
        if (content = isValidNode(n))
            nodes.push({ n, content });
    }

    nodes.forEach(n => replaceNode(n.n, n.content));
}

function isValidNode(node) {
    console.log('registering', node);
    if (node.nodeName == 'IMG')
        return;

    var content = node.nodeName == 'IMG' ? node.src.trim() : node.textContent.trim();
    if (!content)
        return;

    return content;
}

function replaceNode(node, content) {
    if (node.parentElement)
        // replace text with a placeholder
        node.parentElement.innerHTML = '<kori-content text="' + content + '"></kori-content>';
}

function observeCallback(mutations) {
    var content;
    console.log("Observe callback", mutations);

    mutations.forEach(function (mutation) {
        if (mutation.target.tagName == "KORI-CONTENT"
            || mutation.target.parentElement?.tagName == "KORI-CONTENT"
            || mutation.target.classList?.contains('kori-ignore')
            || mutation.target.parentElement?.classList.contains('kori-ignore'))
            return;

        if (mutation.type == 'characterData') {
            console.log('Character data mutation', mutation.target);
            if (content = isValidNode(mutation.target))
                replaceNode(mutation.target, content);
        }
        else if (mutation.type == 'childList'){
            console.log('Mutaton childList', mutation.target);
        }
        else {
            mutation.addedNodes.forEach(registerNodesUnder);
        }
    });
}

function getBrowserLanguage() {
    var lang = (navigator.languages && navigator.languages.length) ? navigator.languages[0] :
        navigator.userLanguage || navigator.language || navigator.browserLanguage || 'en';
    return lang.substring(0, 2);
}

//let playAudio = function (url) {
//    const sound = new Howl({
//        src: [url]
//    });
//    sound.play();
//}

//// global login - mobile UI, tabs sliding active indicator

//var tabsParent = document.getElementById("kori-login__tabs");
//var tabs = document.querySelectorAll(".kori-login__tab");

//function updateActiveIndicator(activeElement) {
//    const tabsParentLeftDistance = tabsParent.getBoundingClientRect().left;
//    console.log("tabsParentLeftDistance: " + tabsParentLeftDistance);

//    const {
//        width: elementSize,
//        left: elementLeftDistance,
//    } = activeElement.getBoundingClientRect();

//    const distanceFromParent = elementLeftDistance - tabsParentLeftDistance;
//    console.log("distancefromParent: " + distanceFromParent);
//    console.log("elementSize: " + elementSize);

//    tabsParent.style.setProperty("--indicator-offset", distanceFromParent + "px");
//    tabsParent.style.setProperty("--indicator-width", elementSize + "px");
//}

//// selecting and unselecting kori-enabled elements
//function toggleSelected(t) {
//    var koriElem = t.closest(".kori-enabled");
//    console.log("koriElem", koriElem);

//    var topBar = document.getElementById("kori-top-bar");
//    if (topBar && topBar.contains(t)) {
//        return;
//    }

//    var koriContent = document.querySelector(".kori-content");

//    if (!koriElem) {
//        document.querySelector(".selected")?.classList.remove("selected");
//        cancelEdit();
//        activeNode = null;        
//        dotNet.invokeMethodAsync("SetDefaultMode");
      
//        if (koriContent) {
//            koriContent.style.cursor = "default"; 
//        }
        
//        return;
//    }

//    var selectedElem = document.querySelector(".selected");

//    if (selectedElem && selectedElem !== koriElem) {
//        selectedElem.classList.remove("selected");
//    }

//    if (!koriElem.classList.contains("selected")) {
//        koriElem.classList.add("selected");

//        dotNet.invokeMethodAsync("EditAsync");  

//        showTopBar(koriElem);

//        if (koriContent) {
//            koriContent.style.cursor = "pointer";
//        }
//    }
//}

//// showing kori top bar
//function showTopBar(t) {
//    var topBar = document.getElementById("kori-top-bar");

//    document.body.appendChild(topBar);

//    topBar.classList.add("show");

//    if (topBar.classList.contains("show")) {
//        // adjusts the top margin to match the top-bar height
//        document.body.style.marginTop = '84px';
//    }

//    const koriId = t.getAttribute('kori-id');
//    // search for matching node in translation cache
//    for (let key in translationCache) {
//        for (var i = 0; i < translationCache[key].Nodes.length; i++)

//            if (t.contains(translationCache[key].Nodes[i])) {
//                activeNode = translationCache[key].Nodes[i];
//                activeMessageId = key;
//                break;
//            }

//        if (koriId != null && koriId == translationCache[key].id) {
//            activeNode = t;
//            activeMessageId = key;
//            break;
//        }
//    }

//    console.log('Set active node', activeNode);
//}

//function getActiveImageSrc() {
//    if (activeNode && activeNode.tagName === 'IMG') {
//        return activeNode.src;
//        //console.log('Active node is an image', activeNode.src);
//    }

//    //console.log('Active node is not an image', activeNode)
//    return null;
//}

//function edit() {
//    if (!activeNode) {
//        console.log('Unable to edit element', activeNode);
//        return;
//    }

//    var translation = translationCache[activeMessageId];

//    if (isTranslationAlreadySaved(translation)) {
//        var activeNodeParent = getActiveNodeParentByKoriId(translation);
//        activateNodeEdition(activeNodeParent);
//        replaceInnerHtmlBeforeTopBar(activeNodeParent, getTranslationRawMarkdownText(translation));
//    }
//    else {
//        var parentElement = activeNode.parentElement;
//        activateNodeEdition(parentElement);

//        // If the Translation is empty or contains only whitespace
//        if (!translation || !translation.Translation || translation.Translation.trim() === "") {

//            // Insert a temporary space to ensure the cursor appears
//            activeNode.textContent = "\u200B"; // Zero-width space character
//        } else {
//            activeNode.textContent = getTranslationRawMarkdownText(translation);
//        }
//    }

//    document.getElementById("kori-top-bar").contentEditable = "false";
//}

//function getTranslationRawMarkdownText(translation) {
//    return translation.text ?? translation.Translation;
//}

//function activateNodeEdition(node) {
//    node.classList.add('kori-ignore');
//    node.contentEditable = "true";
//    node.focus();
//}

//function deactivateNodeEdition(node, translation) {
//    node.contentEditable = "false";
//    node.classList.remove('kori-ignore');
//    node.classList.remove('selected');

//    node.innerHTML = translation.html;
//}

//function getActiveNodeParentByKoriId(translation) {
//    return document.querySelector(`[kori-id="${translation.id}"]`);
//}

//function isTranslationAlreadySaved(translation) {
//    return translation.id;
//}

//function replaceInnerHtmlBeforeTopBar(node, markdownTxt) {
//    if (node.firstChild && node.firstChild.nodeType === Node.TEXT_NODE && node.firstChild.nodeValue === markdownTxt) {
//        return;
//    }

//    if (node.firstChild && node.firstChild.nodeType !== Node.TEXT_NODE) {
//        node.removeChild(node.firstChild);
//    }

//    node.textContent = markdownTxt;

//    node.contentEditable = "true";
//}

//function editImage() {
//    console.log("Entered the edit image function");
//}

//function showSidebar() {
//    document.body.style.marginRight = "317px";

//    var topBar = document.getElementById('kori-top-bar');
//    topBar.style.width = "calc(100% - 317px)";
//}

//function hideSidebar() {
//    console.log("hide sidebar function");
//    document.body.style.marginRight = "0px";

//    var topBar = document.getElementById('kori-top-bar');
//    topBar.style.width = "100%";
//}

//function cancelEdit() {
//    var translation = translationCache[activeMessageId];

//    if (isTranslationAlreadySaved(translation)) {
//        var activeNodeParent = document.querySelector(`[kori-id="${translation.id}"]`);
//        deactivateNodeEdition(activeNodeParent, translation);
//    } else {
//        activeNode.textContent = translation.Translation;
//        activeNode.parentElement.contentEditable = "false";
//        activeNode.parentElement.classList.remove('selected');
//    }
//}

//function closeSearch() {
//    console.log("close search function");

//    var searchSidebar = document.getElementById('kori-search');
//    if (searchSidebar) {
//        searchSidebar.classList.remove('show');
//    }

//    hideSidebar();
//}

//function save() {
//    if (!activeNode)
//        return;

//    var translation = translationCache[activeMessageId];
//    var textContent = activeNode.textContent;
//    var tagContent = translation.tag;
    
//    dotNet.invokeMethodAsync("SaveAsync", translation.id, tagContent, textContent).then(content => {
//        console.log('Saved new content.', content);

//        translationCache[activeMessageId].Translation = content.text;
//        translationCache[activeMessageId].tag = content.tag;
//        translationCache[activeMessageId].text = content.text;
//        translationCache[activeMessageId].html = content.html;

//        activeNode.parentElement.contentEditable = "false";
//        activeNode.parentElement.classList.remove('kori-ignore');

//        if (translation.id) {

//            var activeNodeParent = document.querySelector(`[kori-id="${translation.id}"]`);
//            deactivateNodeEdition(activeNodeParent, translation);

//        } else {
//            translationCache[activeMessageId].id = content.id;
//            activeNode.parentElement?.setAttribute('kori-id', content.id);
//        }
        
//        dotNet.invokeMethodAsync("SetDefaultMode");

//    });
    
//}

//function isDescendantOfClass(element, className) {
//    while (element) {
//        if (element.classList && element.classList.contains(className)) {
//            return true;
//        }
//        element = element.parentElement;
//    }
//    return false;
//}

//function checkSelectedContentType() {
//    var selectedElement = document.getElementsByClassName("selected")[0];

//    if (!selectedElement) {
//        return "none";
//    }

//    if (selectedElement.tagName.toLowerCase() === 'img') {
//        return "image";
//    }

//    // checks if the selected element has the classes 'kori-enabled' and 'selected'
//    if (selectedElement.classList.contains('kori-enabled') && selectedElement.classList.contains('selected')) {
//        var imgChildren = selectedElement.querySelectorAll('img');

//        // Iterates over the child images to check for the presence of 'kori-ignore'
//        for (var img of imgChildren) {
//            // checks if the image is not inside a parent element with class 'kori-ignore'
//            if (!isDescendantOfClass(img, 'kori-ignore')) {
//                return "image";
//            }
//        }
//    }

//    // if it is not an image, assume it is text
//    return "text";
//}

//// show and hide language menu
//function toggleLanguage(isOpen) {
//    console.log("opening language menu");
//    var language = document.getElementById("kori-language");

//    if (!language.classList.contains("show") && isOpen == true) {
//        language.classList.add("show");
//    }

//    if (language.classList.contains("show") && isOpen == false) {
//        language.classList.remove("show");
//    }
//}

//// login to use kori services
//function login() {
//    console.log("logging in...");
//}

//function applyMarkdown(symbol, position) {
    
//    const selectedText = window.getSelection().toString();
    
//    if (selectedText) {

//        var newText = selectedText;

//        if (position == "wrap") {
//            newText = symbol + newText + symbol;
//        } else if (position == "before") {
//            newText = symbol + newText;
//        }

//        insertMarkdownText(newText);
//    }
//}

//function insertMarkdownText(newText) {
//    const selection = window.getSelection();
//    if (!selection.rangeCount) return;

//    const range = selection.getRangeAt(0);
//    range.deleteContents();
//    range.insertNode(document.createTextNode(newText));

//    range.setStartAfter(range.endContainer);
//    range.collapse(true);
//    selection.removeAllRanges();
//    selection.addRange(range);
//}

//function updateImageSrc(currentSrc, newSrc) {
//    var img = document.querySelector(`img[src="${currentSrc}"]`);

//    if (img) {
//        img.src = newSrc;
//        translationCache[activeMessageId].text = newSrc;
//        console.log("Image src updated", img);
//    }
//}

//function resetState() {
//    translationCache = {};
//    language = getBrowserLanguage();
//    koriAuthorized = false;
//    topBar = {};
//    activeNode = null;
//    activeMessageId = null;

//    var koriElements = document.getElementsByClassName('kori-content');

//    if (koriElements.length > 0)
//        var elementId = koriElements[0].id;

//    var app = document.getElementById(elementId);

//    if (observer instanceof MutationObserver) {
//        observer.disconnect();
//    }

//    if (app instanceof Node) {
//        observer = new MutationObserver(observeCallback);
//        observer.observe(app, { childList: true, characterData: true, subtree: true });
//    } else {
//        console.warn("'app' element not found in DOM.");
//    }

//    console.log('State reset');
//}

//function observeUrlChange() {
//    let oldHref = document.location.href;
//    const body = document.querySelector('body');
//    const observer = new MutationObserver(mutations => {
//        if (oldHref !== document.location.href) {
//            oldHref = document.location.href;
//            console.log("URL changed to:", oldHref);
//            resetState();
//        }
//    });
//    observer.observe(body, { childList: true, subtree: true });
//};

//window.addEventListener("load", observeUrlChange());

//function addTargetTextStyle() {
//    const style = document.createElement('style');
//    style.innerHTML = `
//    ::target-text {
//        background-color: rebeccapurple !important;
//        color: white !important;
//        font-weight: bold !important;
//    }
//    `;
//    document.head.appendChild(style);
//}

//export { init, playAudio, edit, cancelEdit, save, checkSelectedContentType, editImage, applyMarkdown, getActiveImageSrc, updateImageSrc, showSidebar, closeSearch };
