var Engine = Matter.Engine,
    Render = Matter.Render,
    Runner = Matter.Runner,
    Bodies = Matter.Bodies,
    Body = Matter.Body,
    Composite = Matter.Composite;

let el = {};
let render = {};
let engine = {};

function initialize(id) {
    el = document.getElementById(id);
    let rect = el.getBoundingClientRect();

    engine = Engine.create();
    render = Render.create({
        element: el,
        engine: engine,
        options: {
            width: rect.width,
            height: rect.height,
            wireframes: false,
            background: 'transparent'
        }
    });

    return {
        Id: id,
        Height: rect.height,
        Width: rect.width
    };
}

function add(body) {
    let b;
    let options = {
        isStatic: true,
        fillStyle: body.fill,
        strokeStyle: body.stroke,
        label: body.name
    };

    switch (body.type) {
        case "rectangle": 
            b = Bodies.rectangle(body.x, body.y, body.width, body.height, options);
    }

    console.log('adding', b);
    Composite.add(engine.world, b);
}

function animate() {
    Render.run(render);
    Runner.run(Runner.create(), engine);
}

function animatebody(body) {
    const allBodies = Matter.Composite.allBodies(engine.world);
    console.log('animating', body);
    console.log('all bodies are', allBodies);

    allBodies.forEach((b) => {
        if (b.label === body.name) {
            console.log('found body', b);
            Body.setStatic(b, false);
        }
    });
}

export { initialize, add, animate, animatebody };