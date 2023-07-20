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

//window.canvasInterop = {
//    setupCanvas: function () {
//        var canvas = document.getElementById("canvas--game-example");
//        var context = canvas.getContext("2d");
//        // Perform various operations on the canvas using the context object
//        // Example: Draw a rectangle
//        //context.fillStyle = "red";
//        //context.fillRect(10, 10, 100, 100);
//    }
//};

gsap.registerPlugin(ScrollTrigger);

class MatterRectCirc {
    constructor(element) {
        this.element = document.querySelector(element);
        this.width = this.element.getBoundingClientRect().width;
        this.height = document.querySelector(".js-banner-blue").getBoundingClientRect().height;

        // create an engine
        this.engine = Engine.create();
        this.engine.gravity.y = 1;
        this.world = this.engine.world;

        // create a renderer
        this.render = Render.create({
            element: this.element,
            engine: this.engine,
            options: {
                width: this.width,
                height: this.height,

                wireframes: false,
                background: "transparent",
            },
        });
        this.addWalls();
        this.addBodies();
        this.updateBodies();
        this.setupMouse();

        // run the renderer
        Render.run(this.render);

        // create runner
        this.runner = Runner.create();

        // run the engine
        Runner.run(this.runner, this.engine);
    }

    addWalls() {
        console.log("add walls");

        let roof = Bodies.rectangle(0, 0 - 75 * 2, this.width * 2, 60 * 5, {
            isStatic: true,
            label: "wall",
            render: {
                fillStyle: "transparent",
                strokeStyle: "transparent",
            },
        });
        let ground = Bodies.rectangle(0, this.height + 25, this.width * 2, 60, {
            isStatic: true,
            label: "wall",
            render: {
                fillStyle: "transparent",
                strokeStyle: "transparent",
            },
        });
        let leftWall = Bodies.rectangle(0 - 75 * 2, 0, 60 * 5, this.height * 2, {
            isStatic: true,
            label: "wall",
            render: {
                fillStyle: "transparent",
                strokeStyle: "transparent",
            },
        });
        let rightWall = Bodies.rectangle(this.width + 74 * 2, 0, 60 * 5, this.height * 2, {
            isStatic: true,
            label: "wall",
            render: {
                fillStyle: "transparent",
                strokeStyle: "transparent",
            },
        }
        );

        Composite.add(this.world, [ground, roof, leftWall, rightWall]);
    }

    addBodies() {
        console.log("add bodies");

        let borderRadius;
        const reduceSize = 8;
        let rectSize;
        let circleSize;
        let gap;

        if (window.innerWidth <= 375) {
            rectSize = Math.round(this.height / 2.5);
            circleSize = Math.round(this.height / 5.5);
            borderRadius = 24;
            gap = 0;
        } else {
            rectSize = Math.round((this.height / 2) - reduceSize);
            circleSize = Math.round(this.height / 4);
            borderRadius = 32;
            gap = 32;
        }

        let positionStart;
        let positionBottom;
        let positionTop;

        function leftColumnPosition(prop) {
            if (prop.body === "rectangle") {
                if (prop.coord === "x") return positionStart = Math.round(rectSize / 2) + gap;
                if (prop.coord === "y") return positionBottom = (positionStart + rectSize) - gap;
            }

            if (prop.body === "circle") {
                if (prop.coord === "x") return positionStart = circleSize + gap;
                if (prop.coord === "y") return positionTop = circleSize;
            }
        }

        function rightColumnPosition(prop) {
            if (prop.body === "rectangle") {
                if (prop.coord === "x") return positionStart = rectSize + circleSize + gap;
                if (prop.coord === "y") return positionTop = (rectSize / 2);
            }

            if (prop.body === "circle") {
                if (prop.coord === "x") return positionStart = circleSize + rectSize + gap;
                if (prop.coord === "y") return positionBottom = circleSize + rectSize;
            }
        }

        const boxBottom = Bodies.rectangle(
            leftColumnPosition({ body: "rectangle", coord: "x" }),
            leftColumnPosition({ body: "rectangle", coord: "y" }),
            rectSize,
            rectSize,
            {
                isStatic: true,
                chamfer: { radius: borderRadius },
                render: {
                    fillStyle: "#19A97B",
                    strokeStyle: "transparent",
                },
            }
        );
        const circleTop = Bodies.circle(
            leftColumnPosition({ body: "circle", coord: "x" }),
            leftColumnPosition({ body: "circle", coord: "y" }),
            circleSize,
            {
                isStatic: true,
                render: {
                    fillStyle: "#19A97B",
                    strokeStyle: "transparent",
                },
            }
        );
        const circleBottom = Bodies.circle(
            rightColumnPosition({ body: "circle", coord: "x" }),
            rightColumnPosition({ body: "circle", coord: "y" }),
            circleSize,
            {
                isStatic: true,
                render: {
                    fillStyle: "#19A97B",
                    strokeStyle: "transparent",
                },
            }
        );
        const boxTop = Bodies.rectangle(
            rightColumnPosition({ body: "rectangle", coord: "x" }),
            rightColumnPosition({ body: "rectangle", coord: "y" }),
            rectSize,
            rectSize,
            {
                isStatic: true,
                chamfer: { radius: borderRadius },
                render: {
                    fillStyle: "#19A97B",
                    strokeStyle: "transparent",
                },
            }
        );
        Composite.add(this.world, [boxBottom, circleTop, circleBottom, boxTop]);
    }

    updateBodies() {
        console.log("update bodies");

        ScrollTrigger.create({
            trigger: ".matter-rect-circ",
            start: "center bottom",
            once: true,
            // markers: true,
            onEnter: () => {
                const allBodies = Matter.Composite.allBodies(this.engine.world);
                allBodies.forEach((body) => {
                    if (body.label !== "wall") {
                        Body.setStatic(body, false);
                    }
                });
            },
        });
    }

    setupMouse() {
        console.log("setup mouse");

        if (window.innerWidth < 1024) return;

        const mouse = Mouse.create(this.render.canvas);
        const mouseConstraint = MouseConstraint.create(this.engine, {
            mouse: mouse,
            constraint: {
                stiffness: 0.1,
                render: {
                    visible: false,
                },
            },
        });

        Composite.add(this.world, mouseConstraint);

        // keep the mouse in sync with rendering
        this.render.mouse = mouse;

        // Allow page scrolling in matter.js window
        mouse.element.removeEventListener("mousewheel", mouse.mousewheel);
        mouse.element.removeEventListener("DOMMouseScroll", mouse.mousewheel);
    }
}

window.addEventListener("DOMContentLoaded", function () {
    new MatterRectCirc(".matter-rect-circ");
});