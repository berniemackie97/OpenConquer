# OpenConquer 

**A Faithful Conquer Online Emulator (Currently only for patch 5517/5518)**

---

## Project Goals

1. **Exact Live-Server Parity Where It Matters**
   The goal of this project is to try first and foremost recreate the game we all loved in the ways that it counts. This is something that is going to mean something different person to person. So beyond my definition of "where it matters" I strong goal of mine is to make this project adaptable so that whoever you are using this can easily adjust to fit what matters to you most.
    
3. **Modern .NET 8 Architecture**
   As I got into this world of conquer server emulation and was playing around with some community projects, I felt bogged down, and encumbered by old methodologies, and tech limitations that these older sources had. This project aims to modernize the approach available in a lot of public sources out there for either others to use or learn from. This project will focus on clean code practices modern solutions and clear separation of concern to try to achieve the most scalability and maintainability as the project grows.
   
5. **Foundation for Derivatives**
   The idea for this project came to me when I was updating Albetros1.1 source code to create a "singleplayer" version of conquer online when I decided I really wanted to go all in on this I knew I had to do a bit more than edit an old source. If I wanted to do what I had planned here we would need to write this from the ground up. (That said much if not most of the implementation, protocols, etc here come from either Comet, or Albetros in one way or another as these are the sources I used as learning material when I first got into conquer server emulation) 

## Repository Structure

```
OpenConquer.sln
├── OpenConquer.Protocol        # Packet definitions, crypto, serialization
├── OpenConquer.Domain          # Core models, enums, service interfaces
├── OpenConquer.Infrastructure  # EF Core, networking hosts
├── OpenConquer.AccountServer   # Login/auth host
├── OpenConquer.GameServer      # World/logic host
└── OpenConquer.Tests           # unit, and integration tests
```

### Project Responsibilities

* **Protocol**

  * Defines packet schemas, reading/writing, encryption

* **Domain**

  * Business types
  * Service contracts
  * Enums and value objects

* **Infrastructure**

  * EF Core DbContexts, migrations, entity configurations
  * Mapster mappings between EF entities and domain models
  * Implementations of domain contracts/services
  * Scripting host and telemetry integrations

* **AccountServer**

  * Entrypoint for authentication & character listing

* **GameServer**

  * Entrypoint for world simulation: movement, combat, NPCs, events, etc

* **Tests**

  * Tests lol

---

## Roadmap

* **Phase 1**: Core protocol & handshake (✅)
* **Phase 2**: Account & char CRUD (✅)
* **Phase 3**: Experience & stats (✅)
* **Phase 4**: Movement & map logic (✏️)
* **Phase 5**: Combat & skills (⏳)
* **Phase 6**: NPC scripting & quests (⏳)
* **Phase 7**: Economy, shops, items (⏳)
* **Phase 8**: Social systems (chat, guilds) (⏳)
* **Phase 9**: Observability & testing (⏳)

---

## Contributing

1. Fork the repository and create a feature branch.
2. Align changes with project responsibilities above.
3. Write/update tests for new logic.
4. Open a pull request—describe the problem and your solution.

**License**: MIT
