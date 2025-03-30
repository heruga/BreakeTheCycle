# Hades-Like Dungeon Generator for Unity

A procedural dungeon generator for Unity that follows the Hades/roguelike room progression pattern, where players move forward through doors and can never backtrack to previous rooms.

## Features

- **One-Way Progression**: Players can only move forward through doors, never backward.
- **Dynamic Room Loading**: Rooms are randomly selected from a pool and loaded only when the player enters a door.
- **Room Pool Management**: Configure different room types (combat, shop, boss, treasure, etc.) with different probabilities.
- **Visual Door Selection**: Multiple doors in each room lead to different room types, giving players meaningful choices.
- **Editor Integration**: Complete editor tools for easy setup and configuration.
- **Independent Room Instances**: Rooms are isolated instances for better performance.
- **Memory Optimization**: Only keeps necessary rooms loaded, unloading distant rooms.
- **Room Transitions**: Customizable transitions between rooms.
- **Reward System**: Different room types offer different rewards based on configuration.

## Quick Start

1. Open the Dungeon Generator setup window: `Tools > Dungeon Generator > Setup Window`
2. Create a new Dungeon Configuration
3. Create room types for your game (combat, treasure, shop, boss, etc.)
4. Create room data assets and assign room prefabs
5. Create room pools for each floor
6. Create a door prefab using the editor tools
7. Click "Create Demo Scene" to test your dungeon

## Core Components

### Scriptable Objects

- **DungeonConfiguration**: Main configuration asset that defines global settings for the dungeon generator.
- **RoomType**: Defines a type of room (combat, reward, shop, boss, etc.) with visual indicators.
- **RoomData**: Defines a specific room prefab and its properties.
- **RoomPool**: Defines a collection of rooms for a specific floor/depth.

### MonoBehaviour Components

- **DungeonManager**: Main controller that manages dungeon generation and progression.
- **Room**: Component attached to each room that handles room-specific logic.
- **Door**: Component that handles transitions between rooms.

## Setting Up Room Types

1. In the Dungeon Generator window, go to the "Room Types" tab.
2. Click "Create New Room Type" to create a new room type.
3. Set the following properties:
   - Name and description
   - Door icon and color for visual indication
   - Whether doors to this room should be locked until the room is cleared
   - Floor depth range where this room type can appear
   - Spawn probability

Common room types include:
- Combat rooms (default, where enemies are fought)
- Shop rooms (with purchasable items)
- Treasure/Reward rooms (free rewards)
- Boss rooms (end of floor encounters)
- Special/Challenge rooms (unique encounters)
- Entry/Exit rooms (beginning and end of floors)

## Creating Rooms

1. In the Dungeon Generator window, go to the "Rooms" tab.
2. Click "Create New Room Data" to create a room data asset.
3. Create a room prefab in your project with the necessary components:
   - The room geometry (walls, floor, etc.)
   - Door placeholders where doors will be spawned
   - Enemy spawn points (for combat rooms)
   - Reward spawn points (for reward rooms)
4. Assign your room prefab to the room data asset.
5. Configure the room properties:
   - Room type
   - Difficulty level
   - Minimum/maximum number of doors
   - Possible next room types that doors can lead to

## Door Configuration

1. In the Dungeon Generator window, go to the "Doors" tab.
2. Assign a base door model/prefab.
3. Click "Create Door Prefab" to generate a door prefab with the Door component.
4. Customize the door visuals and properties as needed.
5. Assign the door prefab to your DungeonConfiguration.

## Floor Setup

1. In the Dungeon Generator window, go to the "Floors" tab.
2. Click "Create New Floor Pool" to create a new floor.
3. Assign room data assets to the floor's room pools:
   - Entry rooms (first room of the floor)
   - Combat rooms (regular encounters)
   - Reward rooms (treasure, etc.)
   - Shop rooms
   - Special rooms
   - Boss rooms (end of floor)
   - Exit rooms (transition to next floor)
4. Configure progression settings:
   - Minimum rooms before boss room becomes available
   - Maximum rooms in the floor
   - Limited room type availability (e.g., only 2 shops per floor)

## Runtime Behavior

During gameplay:
1. The DungeonManager initializes with the first room.
2. When a player approaches a door, the door displays its destination room type.
3. When the player interacts with a door, the system:
   - Randomizes a room from the appropriate pool
   - Loads the selected room
   - Transitions the player to the new room
   - Optionally unloads distant rooms to save memory
4. Combat rooms lock doors until all enemies are defeated.
5. Completing a floor's boss room allows progression to the next floor.

## Extending the System

### Custom Room Types

1. Create a new RoomType asset with unique properties.
2. Create room prefabs that work with this room type.
3. Add the room type to your room pools.

### Custom Rewards

1. Create a reward prefab with the Reward component.
2. Configure which room types it can appear in.
3. Add to your reward prefabs list in the DungeonManager.

### Custom Transitions

1. Modify the transition type in your DungeonConfiguration.
2. Extend the TransitionType enum and implement your custom transition in the DungeonManager.

## Tips and Best Practices

- Keep room prefabs lightweight for faster loading.
- Use direct prefab references for rooms for simplicity.
- Create varied room layouts for each room type.
- Balance the distribution of room types for a good gameplay experience.
- Add variation to room pools for each floor to create a sense of progression.
- Test your dungeon with different seeds to ensure good randomization.

## Example Workflow

1. Design room templates for different room types (combat, shop, boss, etc.)
2. Create prefabs for each room template
3. Set up room types in the Dungeon Generator window
4. Create room data assets for each room prefab
5. Configure room pools for each floor
6. Create a door prefab
7. Test your dungeon and adjust as needed

## Troubleshooting

- **Rooms not loading**: Check that room prefabs are correctly assigned to room data assets.
- **Doors not working**: Ensure door components have correct references and colliders.
- **Always getting the same rooms**: Check that room pools have sufficient variety and randomization settings.
- **Performance issues**: Consider optimizing room prefab assets and adjusting the room unloading settings.

Happy dungeon building! 