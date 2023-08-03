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

    var array1 = [],
        array2 = [],
        array3 = [],
        array4 = []

    // stack 1
    for (i = 0; i < 10; i++) {
        var id = "stack-1-" + i;
        var path = document.getElementById(id);
        array1.push(path);
    }

    // stack 2
    for (i = 0; i < 10; i++) {
        var id = "stack-2-" + i;
        var path = document.getElementById(id);
        array2.push(path);
    }

    // stack 3
    for (i = 0; i < 10; i++) {
        var id = "stack-3-" + i;
        var path = document.getElementById(id);
        array3.push(path);
    }

    // stack 4
    for (i = 0; i < 10; i++) {
        var id = "stack-4-" + i;
        var path = document.getElementById(id);
        array4.push(path);
    }

    // stacks
    const gap = 0;
    //const positionEnd = (canvas.width - gap) - (134 * 10);
    const positionEnd = canvas.width;

    var myStack = Composites.stack(
        positionEnd,
        0,
        6,
        4,
        0,
        0,
        function (x, y) {
            var num = Common.random(0, 4);
            if (num > 0 && num < 1) {
                var path = document.getElementById("squarePath");
                var color = "#F05A67";
                return Bodies.fromVertices(
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
            } else if (num > 1 && num < 2) {
                var path = document.getElementById("trianglePath");
                var color = "#4D4ADF";
                return Bodies.fromVertices(
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
            } else if (num > 2 && num < 3) {
                var path = document.getElementById("circlePath");
                var color = "#E2A30D";
                return Bodies.fromVertices(
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
            } else if (num > 3) {
                var path = document.getElementById("diamondPath");
                var color = "#3BD7FF";
                return Bodies.fromVertices(
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
            }
        }
    );

    var stack1 = Composites.stack(
        positionEnd,
        0,
        10,
        1,
        0,
        0,
        function (x, y) {
            var color = "#FFFFFF";
            var path = document.getElementById("squarePath");
            var cls = path.getAttribute("class");
            if (cls) {
                if (cls.includes("square")) {
                    color = "#F05A67";
                }
                if (cls.includes("triangle")) {
                    color = "#4D4ADF";
                }
                if (cls.includes("circle")) {
                    color = "#E2A30D";
                }
                if (cls.includes("diamond")) {
                    color = "#3BD7FF";
                }
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
            //array1.forEach(path => {
            //    var color = "#FFFFFF";
            //    var cls = path.getAttribute("class");
            //    if (cls) {
            //        if (cls.includes("square")) {
            //            color = "#F05A67";
            //        }
            //        if (cls.includes("triangle")) {
            //            color = "#4D4ADF";
            //        }
            //        if (cls.includes("circle")) {
            //            color = "#E2A30D";
            //        }
            //        if (cls.includes("diamond")) {
            //            color = "#3BD7FF";
            //        }
            //    }

            //    var body = Bodies.fromVertices(
            //        x,
            //        y,
            //        Svg.pathToVertices(path, 1),
            //        {
            //            render: {
            //                fillStyle: color,
            //                strokeStyle: color,
            //            }
            //        },
            //        true
            //    );
            //    return body;
            //});
        }
    );

    var stack2 = Composites.stack(
        positionEnd,
        0,
        10,
        1,
        0,
        0,
        function (x, y) {
            var color = "#FFFFFF";
            var path = document.getElementById("trianglePath");
            var cls = path.getAttribute("class");
            if (cls) {
                if (cls.includes("square")) {
                    color = "#F05A67";
                }
                if (cls.includes("triangle")) {
                    color = "#4D4ADF";
                }
                if (cls.includes("circle")) {
                    color = "#E2A30D";
                }
                if (cls.includes("diamond")) {
                    color = "#3BD7FF";
                }
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

    //        array2.forEach(path => {
    //            var color = "#FFFFFF";
    //            var cls = path.getAttribute("class");
    //            if (cls) {
    //                if (cls.includes("square")) {
    //                    color = "#F05A67";
    //                }
    //                if (cls.includes("triangle")) {
    //                    color = "#4D4ADF";
    //                }
    //                if (cls.includes("circle")) {
    //                    color = "#E2A30D";
    //                }
    //                if (cls.includes("diamond")) {
    //                    color = "#3BD7FF";
    //                }
    //            }

    //            var body = Bodies.fromVertices(
    //                x,
    //                y,
    //                Svg.pathToVertices(path, 1),
    //                {
    //                    render: {
    //                        fillStyle: color,
    //                        strokeStyle: color,
    //                    }
    //                },
    //                true
    //            );
    //            return body;
    //        });
        }
    );

    var stack3 = Composites.stack(
        positionEnd,
        0,
        10,
        1,
        0,
        0,
        function (x, y) {
            var color = "#FFFFFF";
            var path = document.getElementById("circlePath");
            var cls = path.getAttribute("class");
            if (cls) {
                if (cls.includes("square")) {
                    color = "#F05A67";
                }
                if (cls.includes("triangle")) {
                    color = "#4D4ADF";
                }
                if (cls.includes("circle")) {
                    color = "#E2A30D";
                }
                if (cls.includes("diamond")) {
                    color = "#3BD7FF";
                }
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

    //        array3.forEach(path => {
    //            var color = "#FFFFFF";
    //            var cls = path.getAttribute("class");
    //            if (cls) {
    //                if (cls.includes("square")) {
    //                    color = "#F05A67";
    //                }
    //                if (cls.includes("triangle")) {
    //                    color = "#4D4ADF";
    //                }
    //                if (cls.includes("circle")) {
    //                    color = "#E2A30D";
    //                }
    //                if (cls.includes("diamond")) {
    //                    color = "#3BD7FF";
    //                }
    //            }

    //            var body = Bodies.fromVertices(
    //                x,
    //                y,
    //                Svg.pathToVertices(path, 1),
    //                {
    //                    render: {
    //                        fillStyle: color,
    //                        strokeStyle: color,
    //                    }
    //                },
    //                true
    //            );
    //            return body;
    //        });
        }
    );

    var stack4 = Composites.stack(
        positionEnd,
        0,
        10,
        1,
        0,
        0,
        function (x, y) {
            var color = "#FFFFFF";
            var path = document.getElementById("diamondPath");
            var cls = path.getAttribute("class");
            if (cls) {
                if (cls.includes("square")) {
                    color = "#F05A67";
                }
                if (cls.includes("triangle")) {
                    color = "#4D4ADF";
                }
                if (cls.includes("circle")) {
                    color = "#E2A30D";
                }
                if (cls.includes("diamond")) {
                    color = "#3BD7FF";
                }
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

    //        array4.forEach(path => {
    //            var color = "#FFFFFF";
    //            var cls = path.getAttribute("class");
    //            if (cls) {
    //                if (cls.includes("square")) {
    //                    color = "#F05A67";
    //                }
    //                if (cls.includes("triangle")) {
    //                    color = "#4D4ADF";
    //                }
    //                if (cls.includes("circle")) {
    //                    color = "#E2A30D";
    //                }
    //                if (cls.includes("diamond")) {
    //                    color = "#3BD7FF";
    //                }
    //            }

    //            var body = Bodies.fromVertices(
    //                x,
    //                y,
    //                Svg.pathToVertices(path, 1),
    //                {
    //                    render: {
    //                        fillStyle: color,
    //                        strokeStyle: color,
    //                    }
    //                },
    //                true
    //            );
    //            return body;
    //        });
        }
    );

    //Composite.add(world, stack1);
    //Composite.add(world, stack2);
    //Composite.add(world, stack3);
    //Composite.add(world, stack4);
    Composite.add(world, [myStack, ceiling, floor, leftWall, rightWall]);

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