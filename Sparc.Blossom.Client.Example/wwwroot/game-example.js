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
        Vertices = Matter.Vertices,
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

    const square = document.getElementById("square"),
        triangle = document.getElementById("triangle"),
        circle = document.getElementById("circle"),
        diamond = document.getElementById("diamond");

    var squareV,
        triangleV,
        circleV,
        diamondV;

    const squarePath = document.getElementById("squarePath");
    squareV = Bodies.fromVertices(
        134,
        134,
        Svg.pathToVertices(squarePath),
        {
            render: {
                fillStyle: "#F05A67",
                strokeStyle: "#F05A67",
            }
        },
        true
    );

    const trianglePath = document.getElementById("trianglePath");
    triangleV = Bodies.fromVertices(
        402,
        134,
        Svg.pathToVertices(trianglePath),
        {
            render: {
                fillStyle: "#4D4ADF",
                strokeStyle: "#4D4ADF",
                lineWidth: 1
            }
        },
        true
    );

    const circlePath = document.getElementById("circlePath");
    circleV = Bodies.fromVertices(
        900,
        134,
        Svg.pathToVertices(circlePath),
        {
            render: {
                fillStyle: "#E2A30D",
                strokeStyle: "#E2A30D",
                lineWidth: 1
            }
        },
        true
    );

    const diamondPath = document.getElementById("diamondPath");
    diamondV = Bodies.fromVertices(
        536,
        134,
        Svg.pathToVertices(diamondPath),
        {
            render: {
                fillStyle: "#3BD7FF",
                strokeStyle: "#3BD7FF",
                lineWidth: 1
            }
        },
        true,
        removeDuplicatePoints = 2
    );

    bodies.push(squareV, triangleV, circleV, diamondV);
    World.add(world, bodies);

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