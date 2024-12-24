function initLogin() {
    if (!/complete|interactive|loaded/.test(document.readyState)) {
        window.addEventListener('DOMContentLoaded');
    }

    document.getElementById("kori-login__tabs").addEventListener("click", e => {
        e.stopImmediatePropagation();
        mouseClickHandler(e);
    });
}

function mouseClickHandler(e) {
    var t = e.target;

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
    }
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

export { initLogin };