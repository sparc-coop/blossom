var login = document.GetElementById("login-component");

window.addEventListener("click", e => {
    e.stopImmediatePropagation();
    mouseClickHandler(e);
});

function mouseClickHandler(e) {
    var t = e.target;

    // click login menu
    if (t.closest(".login-component__toggle")) {
        document.getElementsByClassName("login-component__menu")[0].classList.add("show");
    }

    // login
    if (t.closest(".login-component__btn")) {
        koriAuthorized = true;
        if (koriAuthorized) {
            document.body.classList.add("kori-loggedin"); // add the class to <body>
            document.getElementById("user-loggedin").add("show");
            document.getElementsById("user-loggedout").remove("show");
            document.getElementsByClassName("login-component__menu")[0].remove("show");
        }
    }

    // logout
    if (t.closest(".login-component__logout-btn")) {
        koriAuthorized = false;
        if (koriAuthorized) {
            document.getElementById("login-component__menu").classList.remove("show");
            document.body.classList.remove("kori-loggedin"); // remove the class to <body>
            document.getElementById("user-loggedout").add("show");
            document.getElementsById("user-loggedin").remove("show");
            document.getElementsByClassName("login-component__menu")[0].remove("show");
        }
    }

    if (koriAuthorized) {
        // click kori widget
        if (t.closest(".kori-widget")) {
            if (t.closest('.options__edit')) {
                toggleEdit(true);
                return;
            } else if (t.closest('.kori-edit__back') || t.closest('.kori-edit__cancel')) {
                toggleEdit(false);
            } else if (t.closest('.options__translation')) {
                toggleTranslation(true);
                return;
            } else if (t.closest('.kori-translation__back')) {
                toggleTranslation(false);
            } else if (t.closest('.options__search')) {
                toggleSearch(true);
                return;
            } else if (t.closest('.kori-search__back')) {
                toggleSearch(false);
            } else {
                return;
            }
        }

        // click kori enabled elements
        toggleSelected(t);
    } else {
        console.log("please login to use kori services");
        return;
    }
}