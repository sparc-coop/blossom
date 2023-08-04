function createGame() {
    var canvas = document.getElementById("game-container");
    canvas.width = canvas.offsetWidth;
    canvas.height = canvas.offsetHeight;
    window.addEventListener("resize", function () {
        document.getElementById("game-container").width = canvas.width;
        document.getElementById("game-container").height = canvas.height;
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
    world.gravity.y = 1;
    world.gravity.scale = 0.0003;

    // create a renderer
    const render = Render.create({
        element: canvas,
        engine: engine,
        options: {
            wireframes: false,
            showInternalEdges: false,
            width: percentX(100),
            height: percentY(100),
            background: "transparent",
        }
    });

    Render.run(render);

    // walls
    const bodies = []

    var ceiling = Bodies.rectangle(
        percentX(100) / 2,
        percentY(0) - 10,
        percentX(100),
        20,
        {
            isStatic: true,
            label: "wall",
            slop: 0,
            render: {
                visible: false
            }
        }
    );

    var floor = Bodies.rectangle(
        percentX(100) / 2,
        percentY(100) + 10,
        percentX(100),
        20,
        {
            isStatic: true,
            label: "wall",
            slop: 0,
            render: {
                visible: false
            }
        }
    );

    var rightWall = Bodies.rectangle(
        percentX(100) + 10,
        percentY(100) / 2,
        20,
        percentY(100),
        {
            isStatic: true,
            label: "wall",
            slop: 0,
            render: {
                visible: false
            }
        }
    );

    var leftWall = Bodies.rectangle(
        percentX(0) - 10,
        percentY(100) / 2,
        20, percentY(100),
        {
            isStatic: true,
            label: "wall",
            slop: 0,
            render: {
                visible: false
            }
        }
    );

    bodies.push(ceiling);
    bodies.push(floor);
    bodies.push(rightWall);
    bodies.push(leftWall);

    // stack
    var row = 1;
    var index = 0; 
    
    var myStack = Composites.stack(
        100,
        40,
        10,
        4,
        0,
        0,
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
                        return Bodies.rectangle(x, y, 85, 85, {
                            isStatic: true,
                            slop: 0,
                            restitution: 0.75,
                            chamfer: { radius: 23 },
                            render: {
                                fillStyle: color,
                                strokeStyle: color
                            }
                        });
                    }
                    if (cls.includes("triangle-down")) {
                        color = "#4D4ADF";
                        var positionY = y - 4;
                        return Bodies.fromVertices(
                            x,
                            positionY,
                            Svg.pathToVertices(path, 1),
                            {
                                isStatic: true,
                                slop: 0,
                                restitution: 0.75,
                                render: {
                                    fillStyle: color,
                                    strokeStyle: color,
                                }
                            },
                            true
                        );
                    }
                    if (cls.includes("triangle-up")) {
                        color = "#4D4ADF";
                        var positionY = y + 15;
                        return Bodies.fromVertices(
                            x,
                            positionY,
                            Svg.pathToVertices(path, 1),
                            {
                                isStatic: true,
                                slop: 0,
                                restitution: 0.75,
                                render: {
                                    fillStyle: color,
                                    strokeStyle: color,
                                }
                            },
                            true
                        );
                    }

                    if (cls.includes("circle")) {
                        color = "#E2A30D";
                        return Bodies.circle(x, y, 42.5, {
                            isStatic: true,
                            slop: 0,
                            restitution: 0.75,
                            render: {
                                fillStyle: color,
                                strokeStyle: color
                            }
                        });
                    }
                    if (cls.includes("diamond")) {
                        color = "#3BD7FF";
                        return Bodies.fromVertices(
                            x,
                            y,
                            Svg.pathToVertices(path, 1),
                            {
                                isStatic: true,
                                slop: 0,
                                restitution: 0.75,
                                render: {
                                    fillStyle: color,
                                    strokeStyle: color,
                                }
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

    // bodies (not walls) set to not static on scroll
    ScrollTrigger.create({
        trigger: ".game-container",
        start: "center bottom",
        once: true,
        // markers: true,
        onEnter: () => {
            const allBodies = Matter.Composite.allBodies(world);
            allBodies.forEach(body => {
                console.log(body);
                console.log(Body);
                if (body.label !== "wall") {
                    Body.setStatic(body, false);
                }
            });
        },
    });

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