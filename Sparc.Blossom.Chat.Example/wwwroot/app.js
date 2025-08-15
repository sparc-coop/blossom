function scrollToElement(id) {
    var content = document.getElementById(id);
    console.log(content);

    var show = setTimeout(checkClass(content, "show"), 1000);

    if (show) {
        content.scrollIntoView({ behavior: "smooth", block: "end", inline: "nearest" });
        console.log("scrolling");

    } else {
        return;
    }
}

function checkClass(elem, className) {
    if (elem.classList.contains(className)) {
        return true;
    } else {
        return false;
    }
}

function handleEnter(e) {
    // Check if the pressed key is Enter (keyCode 13 or key 'Enter')
    if (e.keyCode === 13 || e.key === 'Enter') {
        // Prevent the default behavior of Enter (creating a new line)
        e.preventDefault();
        // Submit the form
        document.getElementById('').submit();
    }
}