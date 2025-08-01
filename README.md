# OpenConquer

**A Faithful Conquer Online 5518 Emulator & Community Platform**

---

## Background

OpenConquer is designed to be the **ultimate community base** for running a private Conquer Online server at patch **5518**, with **exact 1:1 parity** to the official live servers. By combining modern .NET 8 architecture with a meticulously reconstructed feature set, OpenConquer provides a turnkey foundation that hobbyists and developers can clone, fork, and extend—whether for multiplayer emulation or creative variants (e.g., a single-player–focused Conquer experience).

## Project Goals

1. **Exact Live-Server Parity (Patch 5517/5518)**
   Implement every system, mechanic, and UI behavior as on the official servers—including mounted combat, rebirth quests, guild events, and more.
2. **Modern .NET 8 Architecture**
   Modular projects leveraging `IHostedService`, `System.IO.Pipelines`, EF Core, DI, Roslyn scripting, and OpenTelemetry.
3. **Scalability & Maintainability**
   Async-first packet processing, bounded worker pools (or actor-style), hot-reloadable C# scripting for NPCs/quests, and clear separation of concerns.
4. **Local-Host Deployability**
   Zero-config or minimal dependencies (SQLite + Redis), single-machine launcher or Docker Compose, and simple configuration.
5. **Observability & Resilience**
   Prometheus-compatible metrics, structured logging, graceful shutdown, unhandled-exception handlers, and comprehensive test coverage.
6. **Foundation for Derivatives**
   Serve as a stable base for community forks—such as a single-player variant—without rearchitecting the core engine.

## Full Feature Parity

Implement all official Conquer Online features from patch 5517/5518, including:

* **Account & Characters**: login/authentication, char create/select/delete
* **Class & Skills**: Warrior, Ninja, Trojan, Archer, Fire/Water Taoist; skill trees & rebirth skills
* **World & Maps**: Wind Plain, Love Canyon, Mystic Castle, Frozen Grotto, etc.
* **NPCs & Quests**: shopkeepers, job center, rebirth & challenge events, scripted dialogs
* **Monsters & AI**: full spawn tables, patrol/pathfinding, aggro/respawn logic
* **Combat & PvP**: PvE/PvP formulas, buffs/debuffs, PK rules, jail system, team PK, wars
* **Items & Economy**: equipment, socketing, enchanting, NPC/CP shops, auction, trade, warehouses
* **Guild & Social**: clans, guild wars, siege bombs, guild beasts/statues, friends, mentor/apprentice, marriage, chat channels
* **Advanced Systems**: offline training, housing, daily quests, alchemy challenge, VIP services, broadcast, flowers ranking

## High-Level Solution Structure

```text
OpenConquer.sln
├── OpenConquer.Protocol        # Packet definitions & crypto
├── OpenConquer.Domain          # Core entities, value objects, enums & repository interfaces
├── OpenConquer.Infrastructure  # Networking, EF Core data access, scripting, metrics implementations
├── OpenConquer.AccountServer   # Worker host registering AccountHostedService
├── OpenConquer.GameServer      # Worker host registering GameHostedService, Chat, Shop, Clan…
└── OpenConquer.Tests           # xUnit protocol, unit & integration tests
```

* **Protocol** remains standalone for packet specs and crypto.
* **Domain** holds core models, value objects, enums, and repository/service interfaces.
* **Infrastructure** implements networking (IHostedService + IO.Pipelines), EF Core persistence, scripting host, and metrics.
* **AccountServer** and **GameServer** are separate host projects for login/auth and world simulation respectively.
* **Tests** covers both Protocol and shared libraries.

## Roadmap & Checklist

> *This checklist is ever-expanding—mark \[✅] when complete, \[✏️] while in progress, and \[ ] if not started.*

### Phase 0 – Preparation

* \[✅] Clone/reference upstream sources (Redux, AlbetrosCO, Comet) for protocol & schemas
* \[✅] Gather 5518 client binaries, map files, and protocol docs (GitLab Wiki)
* \[✅] Scaffold `OpenConquer.sln` and initial project folders

### Phase 1 – Protocol & Networking

* \[✅] **OpenConquer.Protocol**: packet definitions, crypto (seed/XOR/RSA)
* \[✅] **OpenConquer.Infrastructure**: TCP listener, pipelines, seed handshake → `Channel<Packet>`
* \[✅] Protocol compliance tests (seed → response, login handshake)

### Phase 2 – Account & Character Services

* \[✅] **Domain**: define `IAccountService`, `ICharacterService`, domain events
* \[✏️] **Infrastructure**: implement account auth, session management, char CRUD
* \[✏️] EF Core migrations: `Account` & `Character` entities

### Phase 3 – World Simulation

* [ ] **Domain**: models for Map, Player, NPC, Monster, collision rules
* [ ] **Infrastructure**: map loading, entity spawning, movement handling
* [ ] Worker-pool/actor pattern for per-map processing

### Phase 4 – Combat & Skills

* [ ] **Domain**: damage & buff formulas, skill definitions
* [ ] **Infrastructure**: damage application, HP/MP updates, buff timers
* [ ] Validation tests vs. live-server formulas

### Phase 5 – NPCs, Quests & AI

* [ ] **Infrastructure**: Roslyn scripting host for NPC/quest scripts
* [ ] **Domain**: quest state models & reward triggers
* [ ] AI loops: pathfinding, aggro, respawn timing

### Phase 6 – Economy & Items

* [ ] **Domain**: `Item`, `Inventory`, `Shop` models & interfaces
* [ ] **Infrastructure**: item CRUD, socketing/enchanting, shop & marketplace

### Phase 7 – Social Systems

* [ ] **Infrastructure**: chat (Redis pub/sub), clan & guild services
* [ ] **Domain**: social models (party, friends, mentor/apprentice, marriage)

### Phase 8 – Advanced & QoL Features

* [ ] Housing, furniture, offline training, daily/alchemy events
* [ ] Rebirth promotions, prestige tracking, battle power ranking

### Phase 9 – Observability & Testing

* [ ] Instrument services with OpenTelemetry counters & gauges
* [ ] Expose Prometheus endpoint + Grafana dashboard example
* [ ] Structured logging (Serilog) & unhandled-exception handlers
* [ ] Extend unit/integration tests for all packet flows & mechanics

### Phase 10 – Final Polish & Launch

* [ ] Single-machine launcher script / Docker Compose
* [ ] Sample `appsettings.json` & EF Core migration scripts
* [ ] Admin commands (reload scripts, kick/ban, event triggers)
* [ ] User guide: configuration, startup, gameplay

## License & Contribution

Feel free to fork, submit issues, or propose pull requests. Let’s build the definitive Conquer Online emulator together!
