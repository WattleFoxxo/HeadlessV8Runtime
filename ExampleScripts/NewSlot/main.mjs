function WorldLoaded(world) {
    var newSlot = world.AddSlot("NewSlot");
    newSlot.Name = `NewSlot, Whats 9+10? ${9 + 10}`;
}

// The WorldFocused Event will be fired when the world loads
FrooxEngine.WorldManager.WorldFocused.connect((world) => {

    // RunInUpdates makes sure the thread is synced
    world.RunInUpdates(0, new Action(() => 
        WorldLoaded(world)
    ));
});