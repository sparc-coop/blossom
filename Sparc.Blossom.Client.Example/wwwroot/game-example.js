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

    var ceiling = Bodies.rectangle(percentX(100) / 2, percentY(0) - 10, percentX(100), 20, { isStatic: true });
    var floor = Bodies.rectangle(percentX(100) / 2, percentY(100) + 10, percentX(100), 20, { isStatic: true });
    var rightWall = Bodies.rectangle(percentX(100) + 10, percentY(100) / 2, 20, percentY(100), { isStatic: true });
    var leftWall = Bodies.rectangle(percentX(0) - 10, percentY(100) / 2, 20, percentY(100), { isStatic: true });
    ceiling.render.visible = false;
    floor.render.visible = false;
    rightWall.render.visible = false;
    leftWall.render.visible = false;
    bodies.push(ceiling);
    bodies.push(floor);
    bodies.push(rightWall);
    bodies.push(leftWall);

    // create bodies from shapes and add to world

    var stack1 = [],
        stack2 = [],
        stack3 = [],
        stack4 = []

    // stack 1
    for (i = 0; i < 10; i++) {
        var id = "stack-1-" + i;
        var path = document.getElementById(id);
        stack1.push(path);
    }

    // stack 2
    for (i = 0; i < 10; i++) {
        var id = "stack-2-" + i;
        var path = document.getElementById(id);
        stack2.push(path);
    }

    // stack 3
    for (i = 0; i < 10; i++) {
        var id = "stack-3-" + i;
        var path = document.getElementById(id);
        stack3.push(path);
    }

    // stack 4
    for (i = 0; i < 10; i++) {
        var id = "stack-4-" + i;
        var path = document.getElementById(id);
        stack4.push(path);
    }

    // stacks
    const gap = 0;
    const positionEnd = (canvas.width - gap) - (134 * 10);

    var stack1 = Composites.stack(
        positionEnd,
        0,
        10,
        1,
        134,
        134,
        function (x, y) {
            foreach(path in stack1) {
                var color = "#FFFFFF";
                if (path.getAttribute("class").Contains("square")) {
                    color = "#F05A67";
                }
                if (path.getAttribute("class").Contains("triangle")) {
                    color = "#4D4ADF";
                }
                if (path.getAttribute("class").Contains("circle")) {
                    color = "#E2A30D";
                }
                if (path.getAttribute("class").Contains("diamond")) {
                    color = "#3BD7FF";
                }

                var body = Bodies.fromVertices(
                    x,
                    y,
                    Svg.pathToVertices(path, 1),
                    {
                        render: {
                            fillStyle: color,
                            strokeStyle: color,
                        }
                    },
                    true
                );
                return body;
            }
        }
    );

    var stack2 = Composites.stack(
        positionEnd,
        0,
        10,
        1,
        134,
        134,
        function (x, y) {
            foreach(path in stack2) {
                var color = "#FFFFFF";
                if (path.getAttribute("class").Contains("square")) {
                    color = "#F05A67";
                }
                if (path.getAttribute("class").Contains("triangle")) {
                    color = "#4D4ADF";
                }
                if (path.getAttribute("class").Contains("circle")) {
                    color = "#E2A30D";
                }
                if (path.getAttribute("class").Contains("diamond")) {
                    color = "#3BD7FF";
                }

                var body = Bodies.fromVertices(
                    x,
                    y,
                    Svg.pathToVertices(path, 1),
                    {
                        render: {
                            fillStyle: color,
                            strokeStyle: color,
                        }
                    },
                    true
                );
                return body;
            }
        }
    );

    var stack3 = Composites.stack(
        positionEnd,
        0,
        10,
        1,
        134,
        134,
        function (x, y) {
            foreach(path in stack3) {
                var color = "#FFFFFF";
                if (path.getAttribute("class").Contains("square")) {
                    color = "#F05A67";
                }
                if (path.getAttribute("class").Contains("triangle")) {
                    color = "#4D4ADF";
                }
                if (path.getAttribute("class").Contains("circle")) {
                    color = "#E2A30D";
                }
                if (path.getAttribute("class").Contains("diamond")) {
                    color = "#3BD7FF";
                }

                var body = Bodies.fromVertices(
                    x,
                    y,
                    Svg.pathToVertices(path, 1),
                    {
                        render: {
                            fillStyle: color,
                            strokeStyle: color,
                        }
                    },
                    true
                );
                return body;
            }
        }
    );

    var stack4 = Composites.stack(
        positionEnd,
        0,
        10,
        1,
        134,
        134,
        function (x, y) {
            foreach(path in stack4) {
                var color = "#FFFFFF";
                if (path.getAttribute("class").Contains("square")) {
                    color = "#F05A67";
                }
                if (path.getAttribute("class").Contains("triangle")) {
                    color = "#4D4ADF";
                }
                if (path.getAttribute("class").Contains("circle")) {
                    color = "#E2A30D";
                }
                if (path.getAttribute("class").Contains("diamond")) {
                    color = "#3BD7FF";
                }

                var body = Bodies.fromVertices(
                    x,
                    y,
                    Svg.pathToVertices(path, 1),
                    {
                        render: {
                            fillStyle: color,
                            strokeStyle: color,
                        }
                    },
                    true
                );
                return body;
            }
        }
    );

    Composite.add(world, [stack1, ground]);
    Composite.add(world, [stack2, ground]);
    Composite.add(world, [stack3, ground]);
    Composite.add(world, [stack4, ground]);

    //const squarePath = document.getElementById("squarePath"),
    //    trianglePath = document.getElementById("trianglePath"),
    //    circlePath = document.getElementById("circlePath"),
    //    diamondPath = document.getElementById("diamondPath");

    //var squareV,
    //    triangleV,
    //    circleV,
    //    diamondV;

    //squareV = Bodies.fromVertices(
    //    134,
    //    134,
    //    Svg.pathToVertices(squarePath, 1),
    //    {
    //        render: {
    //            fillStyle: "#F05A67",
    //            strokeStyle: "#F05A67",
    //        }
    //    },
    //    true
    //);

    //triangleV = Bodies.fromVertices(
    //    134,
    //    134,
    //    Svg.pathToVertices(trianglePath, 1),
    //    {
    //        render: {
    //            fillStyle: "#4D4ADF",
    //            strokeStyle: "#4D4ADF"
    //        }
    //    },
    //    true
    //);

    //circleV = Bodies.fromVertices(
    //    134,
    //    134,
    //    Svg.pathToVertices(circlePath, 1),
    //    {
    //        render: {
    //            fillStyle: "#E2A30D",
    //            strokeStyle: "#E2A30D"
    //        }
    //    },
    //    true
    //);

    //diamondV = Bodies.fromVertices(
    //    134,
    //    134,
    //    Svg.pathToVertices(diamondPath ,1),
    //    {
    //        render: {
    //            fillStyle: "#3BD7FF",
    //            strokeStyle: "#3BD7FF"
    //        }
    //    },
    //    true
    //);

    //bodies.push(squareV, triangleV, circleV, diamondV);
    //World.add(world, bodies);

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
}