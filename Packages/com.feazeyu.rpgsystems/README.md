# RPG Systems

Graph-based **dialogue** and **quest** systems, a flexible **inventory** (grid, list, and shop), and a small **character/combat** layer for 2D RPGs in Unity. The dialogue and quest systems share a single graph execution framework, so custom nodes and blackboard variables work the same across both.

## Features

- **Unified graph framework** — node/edge/blackboard model with a coroutine-driven runner, parallel flow tokens, subgraphs, and a full graph editor window (canvas, blackboard, inspector) you can reuse for your own graph-based systems.
- **Dialogue** — branching lines, conditional choices, requirement gating, text interpolation from blackboard variables, and inline event/prefab actions.
- **Quests** — single quests (objectives, rewards, terminal outcomes) and quest chains (dependency graphs with gates and per-quest variables). Composable objectives (kill count, collect/accumulate/deliver items, reach location) with stackable gate, timer, and reset modifiers.
- **Inventory** — shape-aware grid inventory with drag-and-drop and stacking, a linear list inventory, and a shop layer (buy/sell against a runtime copy of the stock).
- **Character & combat** — entity resources (health/mana/stamina), a stat/effect scaling system, weapons (melee, projectile, combos), and an interaction system.

## Requirements

- Unity **6000.0** (developed against 6000.0.43f1)
- Dependencies (installed automatically): `com.unity.ugui`, `com.unity.inputsystem`

## Installation

Install via the Package Manager using the Git URL or a local path:

1. **Window → Package Manager → + → Add package from git URL…** (or *from disk…* and pick this folder's `package.json`).
2. Unity resolves the `com.unity.ugui` and `com.unity.inputsystem` dependencies automatically.

## Samples

Import the demos from the package's **Samples** tab in the Package Manager:

- **Dialogue Demo** — an interactive NPC dialogue scene.
- **Quest Demo** — a quest with objectives, enemies, and rewards.
- **Inventory Demo** — a player grid inventory and chests with list inventories.

> Samples use TextMeshPro. If prompted, import the TMP Essentials (Window → TextMeshPro → Import TMP Essential Resources).

## Getting started

- **Dialogue:** *Assets → Create → Dialogue Graph*, author the graph, add a `DialogueRunner` to an NPC, and start it (e.g. through the provided `Interactable`/`Interactor`).
- **Quests:** *Assets → Create → RPGFramework → Quest Graph*. Use a `QuestRunner` for a single quest or a `QuestChainRunner` for a chain.

See the in-code XML documentation for API details.

## License

MIT — see [LICENSE.md](LICENSE.md).
