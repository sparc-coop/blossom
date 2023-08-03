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

    // stacks
    const gap = 0;
    const positionEnd = canvas.width;
    var row = 1;
    var index = 0;

    var myStack = Composites.stack(
        positionEnd,
        0,
        6,
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
            index++;

            var path = document.getElementById(id);
            if (path) {
                var cls = path.getAttribute("class");
            }

            var color = "#FFFFFF";
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
    );

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