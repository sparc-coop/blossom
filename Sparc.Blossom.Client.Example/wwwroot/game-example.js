function createGame() {
    var canvas = document.getElementById("game-container");
    canvas.width = canvas.offsetWidth;
    canvas.height = canvas.offsetHeight;
    window.addEventListener("resize", function () {
        document.getElementById("game-container").width = window.innerWidth;
        document.getElementById("game-container").height = window.innerHeight;
    });

    function percentX(percent) {
        return Math.round((percent / 100) * canvas.width);
    }

    function percentY(percent) {
        return Math.round((percent / 100) * canvas.height);
    }

    var Engine = Matter.Engine,
        Render = Matter.Render,
        Runner = Matter.Runner,
        Body = Matter.Body,
        Bodies = Matter.Bodies,
        Common = Matter.Common,
        Svg = Matter.Svg,
        Composite = Matter.Composite,
        Composites = Matter.Composites,
        Mouse = Matter.Mouse,
        MouseConstraint = Matter.MouseConstraint,
        World = Matter.World;

    // create an engine
    const engine = Engine.create();
    world = engine.world;

    // create runner
    const runner = Runner.create();
    Runner.run(runner, engine);

    // gravity
    world.gravity.y = 0, 000001;

    // create a renderer
    const render = Render.create({
        element: canvas,
        engine: engine,
        options: {
            wireframes: false,
            showInternalEdges: false,
            width: percentX(100) - 2,
            height: percentY(100) - 2,
            borderRadius: 40,
            background: "transparent",
        }
    });

    Render.run(render);

    // boundaries

    const bodies = []

    var ceiling = Bodies.rectangle(percentX(100) / 2, percentY(0) - 10, percentX(100), 20, { isStatic: true, label: "wall" });
    var floor = Bodies.rectangle(percentX(100) / 2, percentY(100) + 10, percentX(100), 20, { isStatic: true, label: "wall" });
    var rightWall = Bodies.rectangle(percentX(100) + 10, percentY(100) / 2, 20, percentY(100), { isStatic: true, label: "wall" });
    var leftWall = Bodies.rectangle(percentX(0) - 10, percentY(100) / 2, 20, percentY(100), { isStatic: true, label: "wall" });
    ceiling.render.visible = false;
    floor.render.visible = false;
    rightWall.render.visible = false;
    leftWall.render.visible = false;
    bodies.push(ceiling);
    bodies.push(floor);
    bodies.push(rightWall);
    bodies.push(leftWall);

    // stacks
    const gap = 67;
    const positionEnd = (canvas.width - gap) - (134 * 6);
    var row = 1;
    var index = 0; 
    
    var myStack = Composites.stack(
        100,
        40,
        10,
        4,
        0,
        2,
        function (x, y) {
            if (index > 9) {
                index = 0;
                row++;
            }

            var string = "stack-" + row + "-";
            var id = string + index;
            console.log(id);
            index++;

            var path = document.getElementById(id);

            if (path) {
                var cls = path.getAttribute("class");
                var color = "#FFFFFF";
                if (cls) {
                    if (cls.includes("square")) {
                        color = "#F05A67";
                        return Bodies.rectangle(x, y, 86, 86, {
                            chamfer: { radius: 23 },
                            render: {
                                fillStyle: color,
                                strokeStyle: color
                            },
                            /*isStatic: true*/
                        });
                    }
                    if (cls.includes("triangle")) {
                        color = "#4D4ADF";
                        return Bodies.fromVertices(
                            x,
                            y,
                            Svg.pathToVertices(path, 1),
                            {
                                render: {
                                    fillStyle: color,
                                    strokeStyle: color,
                                },
                                /*isStatic: true*/
                            },
                            true
                        );
                    }
                    if (cls.includes("circle")) {
                        color = "#E2A30D";
                        return Bodies.circle(x, y, 43, {
                            render: {
                                fillStyle: color,
                                strokeStyle: color
                            },
                            /*isStatic: true*/
                        });
                    }
                    if (cls.includes("diamond")) {
                        color = "#3BD7FF";
                        return Bodies.fromVertices(
                            x,
                            y,
                            Svg.pathToVertices(path, 1),
                            {
                                render: {
                                    fillStyle: color,
                                    strokeStyle: color,
                                },
                                /*isStatic: true*/
                            },
                            true
                        );
                    }
                }
            }
        }
    );

    bodies.push(myStack);
    Composite.add(world, bodies);

    //ScrollTrigger.create({
    //    trigger: ".game-container",
    //    start: "center bottom",
    //    once: true,
    //    // markers: true,
    //    onEnter: () => {
    //        const allBodies = Matter.Composite.allBodies(world);
    //        allBodies.forEach(body => {
    //            console.log(body);
    //            console.log(Body);
    //            if (body.label !== "wall") {
    //                Body.setStatic(body, false);
    //            }
    //        });
    //    },
    //});

    // mouse control
    let mouse = Mouse.create(render.canvas),
        mouseConstraint = MouseConstraint.create(engine, {
            mouse: mouse,
            constraint: {
                stiffness: 0.2,
                render: {
                    visible: false
                }
            }
        });

    World.add(world, mouseConstraint);

    // keep the mouse in sync with rendering
    Render.mouse = mouse;

    // Allow page scrolling in matter.js window
    mouse.element.removeEventListener("mousewheel", mouse.mousewheel);
    mouse.element.removeEventListener("DOMMouseScroll", mouse.mousewheel);
}