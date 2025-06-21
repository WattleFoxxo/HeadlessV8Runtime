function WorldLoaded(world) {
    world.Name = `This world name was changed by javascript!`;
}

// The WorldFocused Event will be fired when the world loads
FrooxEngine.WorldManager.WorldFocused.connect((world) => {

    // RunInUpdates makes sure the thread is synced
    world.RunInUpdates(0, new Action(() => 
        WorldLoaded(world)
    ));
});