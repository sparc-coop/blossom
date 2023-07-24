var canvas = document.getElementById("game-container");
canvas.width = window.innerWidth;
canvas.height = window.innerHeight;

window.addEventListener("resize", function () {
    document.getElementById("animation").width = window.innerWidth;
    document.getElementById("animation").height = window.innerHeight;
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
    element: document.body,
    engine: engine,
    options: {
        wireframes: false,
        showInternalEdges: false,
        width: percentX(100),
        height: percentY(100),
        background: "transparent"
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
    Svg.pathToVertices(squarePath)
);

const trianglePath = document.getElementById("trianglePath");
triangleV = Bodies.fromVertices(
    134,
    134,
    Svg.pathToVertices(trianglePath)
);

const circlePath = document.getElementById("circlePath");
circleV = Bodies.fromVertices(
    134,
    134,
    Svg.pathToVertices(circlePath)
);

const diamondPath = document.getElementById("diamondPath");
diamondV = Bodies.fromVertices(
    134,
    134,
    Svg.pathToVertices(diamondPath)
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