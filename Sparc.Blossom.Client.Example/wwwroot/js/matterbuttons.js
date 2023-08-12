let Engine = Matter.Engine;
let Render = Matter.Render;
let Events = Matter.Events;
let Runner = Matter.Runner;
let Bodies = Matter.Bodies;
let Body = Matter.Body;
let Composite = Matter.Composite;
let World = Matter.World;
let Composites = Matter.Composites;
let MouseConstraint = Matter.MouseConstraint;
let Mouse = Matter.Mouse;
let Vertices = Matter.Vertices;
let Svg = Matter.Svg;

function percentX(width) {
  return Math.round((width / 100) * width);
}

function percentY(height) {
  return Math.round((height / 100) * height);
}

function randomNumber(min, max) {
  return Math.floor(Math.random() * (max - min + 1) + min);
}

class MatterButtons{
    constructor(a) {
        this.element = document.querySelector(a),
        this.width = this.element.getBoundingClientRect().width,
        this.height = this.element.getBoundingClientRect().height,
        this.restitution = .4,
        this.engine = Engine.create(),
        this.engine.gravity.y = .2,
        this.world = this.engine.world,
        this.render = Render.create({
            element: this.element,
            engine: this.engine,
            options: {
                width: this.width,
                height: this.height,
                wireframes: !1,
                background: "transparent"
            }
        }),
        this.setupMouse(),
        this.addWalls(),
        Render.run(this.render),
        this.runner = Runner.create(),
        Runner.run(this.runner, this.engine),
        this.addButtons(),
        this.addInputs(),
        this.addTooltip(),
        this.addToggles(),
        this.updateBodies()
    }

    addButtons() {
        let a = document.querySelectorAll(".js-matter-btn"),
            b = []; a.forEach(a => {
                let c = a.getBoundingClientRect().width,
                    d = a.getBoundingClientRect().height,
                    e = window.getComputedStyle(a).getPropertyValue("border-radius").split("px").join(""),
                    f = Bodies.rectangle(-200, -200, c, d, {
                        isStatic: !0,
                        restitution: this.restitution,
                        label: a.getAttribute("data-target"),
                        render: {
                            fillStyle: "transparent",
                            strokeStyle: "transparent"
                        }, chamfer: { radius: Number(e) }
                    });
                b.push(f),
                    Events.on(this.runner, "tick", b => {
                        a.style.top = f.position.y + "px",
                            a.style.left = f.position.x + "px",
                            a.style.transform = `translate(-50%, -50%) rotate(${f.angle}rad)`
                    })
            }),
            Composite.add(this.world, b)
    }

    addTooltip() {
        let a = document.querySelectorAll(".js-matter-tooltip"),
            b = []; a.forEach(a => {
                let c = a.getBoundingClientRect().width,
                    d = a.getBoundingClientRect().height,
                    e = window.getComputedStyle(a).getPropertyValue("border-radius").split("px").join(""),
                    f = Bodies.rectangle(-200, -200, c, d, {
                        isStatic: !0,
                        restitution: this.restitution,
                        label: "tooltip",
                        render: {
                            fillStyle: "transparent",
                            strokeStyle: "transparent"
                        },
                        chamfer: { radius: Number(e) }
                    }); 
                b.push(f),
                    Events.on(this.runner, "tick", b => {
                        a.style.top = f.position.y + "px",
                        a.style.left = f.position.x + "px",
                        a.style.transform = `translate(-50%, -50%) rotate(${f.angle}rad)`
                    })
            }),
            Composite.add(this.world, b)
    }

    addInputs() {
        let a = document.querySelectorAll(".js-matter-input"),
            b = []; a.forEach(a => {
                let c = a.getBoundingClientRect().width,
                    d = a.getBoundingClientRect().height,
                    e = window.getComputedStyle(a.querySelector(".sv-input")).getPropertyValue("border-radius").split("px").join(""),
                    f = Bodies.rectangle(-200, -200, c, d, {
                        restitution: this.restitution,
                        label: a.getAttribute("data-target"),
                        render: {
                            fillStyle: "transparent",
                            strokeStyle: "transparent"
                        }, chamfer: { radius: Number(e) }
                    });
                b.push(f),
                Events.on(this.runner, "tick", b => {
                    a.style.top = f.position.y + "px",
                        a.style.left = f.position.x + "px",
                        a.style.transform = `translate(-50%, -50%) rotate(${f.angle}rad)`
                })
            }),
            Composite.add(this.world, b)
    }

    addToggles() {
        let a = document.querySelectorAll(".js-matter-toggle"),
            b = []; a.forEach(a => {
                let c = a.getBoundingClientRect().width,
                    d = a.getBoundingClientRect().height,
                    e = window.getComputedStyle(a.querySelector(".slider")).getPropertyValue("border-radius").split("px").join(""),
                    f = Bodies.rectangle(-200, -200, c, d, {
                        restitution: this.restitution,
                        label: a.getAttribute("data-target"),
                        render: {
                            fillStyle: "transparent",
                            strokeStyle: "transparent"
                        },
                        chamfer: { radius: Number(e) }
                    });
                b.push(f),
                Events.on(this.runner, "tick", b => {
                    a.style.top = f.position.y + "px",
                        a.style.left = f.position.x + "px",
                        a.style.transform = `translate(-50%, -50%) rotate(${f.angle}rad)`
                })
            }),
            Composite.add(this.world, b)
    }

    addWalls() {
        let a = Bodies.rectangle(0, -150, 2 * this.width, 300, {
                isStatic: !0,
                label: "wall",
                render: {
                    fillStyle: "transparent",
                    strokeStyle: "transparent"
                }
            }),
            b = Bodies.rectangle(0, this.height + 25, 2 * this.width, 60, {
                label: "wall",
                isStatic: !0,
                render: {
                    fillStyle: "transparent",
                    strokeStyle: "transparent"
                }
            }),
            c = Bodies.rectangle(-150, 0, 300, 2 * this.height, {
                label: "wall",
                isStatic: !0,
                render: {
                    fillStyle: "transparent",
                    strokeStyle: "transparent"
                }
            }),
            d = Bodies.rectangle(this.width + 148, 0, 300, 2 * this.height, {
                label: "wall",
                isStatic: !0,
                render: {
                    fillStyle: "transparent",
                    strokeStyle: "transparent"
                }
            });
        Composite.add(this.world, [b, a, c, d])
    }

    setupMouse() {
        if (window.innerWidth < 1024) return;
        let a = Mouse.create(this.render.canvas),
            b = MouseConstraint.create(this.engine, {
                mouse: a,
                constraint: {
                    stiffness: .1,
                    render: { visible: !1 }
                }
            });
        Composite.add(this.world, b),
        this.render.mouse = a,
        a.element.removeEventListener("mousewheel", a.mousewheel),
        a.element.removeEventListener("DOMMouseScroll", a.mousewheel),
        this.interactWithElements(b)
    }

    interactWithElements(a) {
        Events.on(a, "mousedown", function (h)
        {
            if (a.body) {
                let c = a.body.label;
                if (c.includes("toggle"))
                {
                    let d = document.querySelector(`[data-target='${a.body.label}']`),
                        e = d.querySelector("[type='checkbox']"),
                        f = d.querySelector(".toggle-on"),
                        g = d.querySelector(".toggle-off");
                    if (!e.checked)
                    {
                        e.checked = !0,
                        g.classList.remove("opacity-1"),
                        f.classList.add("opacity-1");
                        return
                    }
                    e.checked = !1,
                    g.classList.add("opacity-1"),
                    f.classList.remove("opacity-1");
                    return
                }
                if (c.includes("input-1") && document.querySelector(`[data-target='${a.body.label}'] input`).focus(), c.includes("button"))
                {
                    let b = document.querySelector(`[data-target='${c}']`);
                    if (b.classList.contains("btn--swap-color-1"))
                    {
                        b.classList.remove("btn--swap-color-1"),
                        b.classList.add("btn--swap-color-2");
                        return
                    }
                    if (b.classList.contains("btn--swap-color-2"))
                    {
                        b.classList.remove("btn--swap-color-2"),
                        b.classList.add("btn--swap-color-3");
                        return
                    }
                    if (b.classList.contains("btn--swap-color-3"))
                    {
                        b.classList.remove("btn--swap-color-3"),
                        b.classList.add("btn--swap-color-4");
                        return
                    }
                    if (b.classList.contains("btn--swap-color-4"))
                    {
                        b.classList.remove("btn--swap-color-4"),
                        b.classList.add("btn--swap-color-5");
                        return
                    }
                    if (b.classList.contains("btn--swap-color-5"))
                    {
                        b.classList.remove("btn--swap-color-5"),
                        b.classList.add("btn--swap-color-6");
                        return
                    }
                    if (b.classList.contains("btn--swap-color-6"))
                    {
                        b.classList.remove("btn--swap-color-6"),
                        b.classList.add("btn--swap-color-7");
                        return
                    }
                    if (b.classList.contains("btn--swap-color-7"))
                    {
                        b.classList.remove("btn--swap-color-7"),
                        b.classList.add("btn--swap-color-8");
                        return
                    }
                    if (b.classList.contains("btn--swap-color-8"))
                    {
                        b.classList.remove("btn--swap-color-8"),
                        b.classList.add("btn--swap-color-9");
                        return
                    }
                    if (b.classList.contains("btn--swap-color-9"))
                    {
                        b.classList.remove("btn--swap-color-9"),
                        b.classList.add("btn--swap-color-1");
                        return
                    }
                }
                c.includes("input-2") && document.querySelector(`[data-target='${c}']`).classList.toggle("is-open")
            }
        })
    }

    updateBodies() {
        ScrollTrigger.create({
            trigger: ".sv-matter",
            start: "top bottom-=100",
            once: !0,
            onEnter: () => {
                let a = Matter.Composite.allBodies(this.engine.world),
                    b = this.width.toFixed(0),
                    c = 1,
                    d = setInterval(function () {
                        c += 1;
                        let e = randomNumber(0, b - 50);
                        a[c]?.label !== "wall" && a[c] && (Body.setStatic(a[c], !1),
                            Body.setPosition(a[c], { x: e, y: 0 }),
                            Body.setVelocity(a[c], { x: randomNumber(0, 10), y: randomNumber(0, 5) })),
                            c === a.length && clearInterval(d)
                    }, 100)
            }
        })
    }
}