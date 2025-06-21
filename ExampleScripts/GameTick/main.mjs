function WorldLoaded(world) {
    Console.WriteLine(`This runs when the world: "${world.Name}" is loaded`);
}

function GameTick(world) {
    Console.WriteLine(`This runs every tick in the world: "${world.Name}". And here is a random number from 0 to 99: ${Math.floor(Math.random() * 100)}`);
}

// The WorldFocused Event will be fired when the world loads
FrooxEngine.WorldManager.WorldFocused.connect((world) => {

    // RunInUpdates makes sure the thread is synced
    world.RunInUpdates(0, new Action(() => 
        WorldLoaded(world)
    ));

    // On onEngineTick is a custom singleton, this will change later 
    OnEngineTick.AddListener(new Action(() => 

        // RunInUpdates makes sure the thread is synced
        world.RunInUpdates(0, new Action(() => 
            GameTick(world)
        ))
    ));
});