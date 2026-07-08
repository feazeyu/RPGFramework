# Framework for the creation of 2D top-down rpg games in Unity

The software work is meant to help with the creation of 2D top-down rpg games in Unity by implementing elements common to the games in the genre in a modular and extensible way.

## Intended functionality

The functionality is intended to be split into several modules, that could be imported into Unity largely independent on each other. Some dependencies are obviously unavoidable.

However, in the event of multiple modules being imported at once, they should be able to seamlessly work together.

The modules should be both easily extendable by the user and functional out of the box.

### The modules

#### Shopkeep module

The idea is to streamline making dialogue menus with NPCs or self-talk when the player comes upon a defined point/something special happens

Generally, upon interacting with an NPC a configurable dialogue window should pop up, with these supported features:

- Conditional dialogue options
- Dialogue options that invoke events
- Accepting quests
- Opening shops
- Disabling character controls while open/Pausing the game
- Events invoked on certain items being purchased/sold

The shopkeeps' costs should be configurable to be virtually anything (Character Stats/Money/Other items)

#### Inventory module

Inventories - Chests, Player inventory, NPC inventories, drop tables.

Inventories should be able to support:

- Stackable/Non-stackable items
- Weight system (Some items are heavier)
- Multi-slot rotatable items
- Events based on the position of inventory items
- Configurable equipment slots

#### Item modules

Virtually anything and everything the player can put in his bag.

Should be closely coupled with the inventory module, but also easily usable with custom inventories.
Items should have a nice ui for configuring their weights, sizes, stackability.

Items should be very versatile, it should be configurable when they work.
Do they work when in the inventory? Or only when equipped? When you get hit? When you hit something?
And many more situations, all should raise specific events.

Items should also be able to modify the character that's weilding them - modifying abilities and stats.

#### Quest module

Quests, the core of every RPG!

Should be configurable to contain virtually anything.

Some examples that come to mind:

- Kill quests
- Fetch quests (Bring items back)
- Walk to place quests
- Interact with thing quests
- Escort quests
- Multi step quests
- Timed quests

Again, I cannot predict everything, so it should be extensible.

### Events

The modules mentioned above talk about invoking a lot of effects, from characters taking damage, to consuming items, to using abilities, events should be invoked.

The goal is to provide a good enough amount of events that the developer wouldn't have to implement them by themself.

Each module should add their own events, and a simple to understand way to add custom ones should be provided.

## Comparison to other works

As of 2025/04/01 I haven't found any free frameworks for making rpgs. What is available on the unity asset store is usually very costly and closed source, or incomplete.

This framework aims to remedy that.

The mentioned expensive frameworks:

- [ORK Framework](https://orkframework.com/)
- [2DRpgKit](https://assetstore.unity.com/packages/templates/packs/2d-rpg-kit-163910)
- [GameCreator2](https://assetstore.unity.com/publishers/7791)

The incomplete ones:

- [CoffeeVampir3](https://github.com/CoffeeVampir3/Unity-RPG-Framework)

There is however a product very close to what I'm trying to achieve:

- [Core-Rpg](https://github.com/delmarle/RPG-Core?tab=readme-ov-file)

However, it has virutally no documentation and was last updated 4 years ago.

## Language and environment specification

The resulting work should be in the form of a Unity asset, meaning it would run on any machine that runs Unity and would be written in C#.
