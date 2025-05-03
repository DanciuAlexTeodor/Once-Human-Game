## 🛠️ Technical Overview

**Once Human** was developed over a period of 6 months using Unity, with a strong emphasis on performance, realism, and immersion. Below is a breakdown of the technical aspects and systems integrated into the game:

----------

### 🔧 Engine & Codebase

-   Built entirely in **Unity**, leveraging the **URP pipeline** for performance and visual fidelity.
    
-   **15,000+ lines** of optimized C# code, cleanly structured across modular systems.
    
-   Compressed assets (**textures, 3D models, audio**) to maintain disk usage under **~150 MB** while delivering high-quality visuals and audio.
    

----------
### 🧠 AI & Gameplay Systems
- **AI Script:** [`MutantScript.cs`](https://github.com/DanciuAlexTeodor/Once-Human-Game/blob/main/SomeScripts/MutantScript.cs)

-   Custom **Mutant AI** using **NavMesh** and **state machine logic** with:
    
    -   Patrolling, chasing, investigating, and attacking states.
        
    -   Dynamic sound/vision detection.
        
    -   Realistic pursuit behavior with path re-evaluation and escape logic.
        
-   Item dependency system: players must collect:
    
    -   **Keys, ID cards, batteries, and screwdrivers** to:
        
        -   Activate power systems.
            
        -   Open restricted/locked areas.
            
        -   Access hidden rooms containing **lore-rich documents**.
            
    -   Monsters can dynamically open doors and adapt to player hiding behaviors.
        
    -   Interaction with music (e.g., radios) affects mutant behavior including dancing and distraction mechanics.
        

----------

### 👁️ Immersive Player Mechanics
  - **Script:** [`FirstPersonController.cs`](https://github.com/DanciuAlexTeodor/Once-Human-Game/blob/main/SomeScripts/FirstPersonController.cs)


-   Smooth **headbob** and **camera sway** effects based on movement state.
    
-   Full range of player motion:
    
    -   Walking, Sprinting, Crouching, Prone (stomach crawl).
        
-   Dynamic **footstep audio** that adapts to surface type (tiles, metal, concrete).
    
-   Integrated **stamina** and **exhaustion system**.
    
-   Toggleable **flashlight** and **radio** mechanics.
    
-   **Customizable controls** through a built-in keybinding manager.
    

----------
### 😱 Jumpscares System
- **Script:** [`Jumpscares.cs`](https://github.com/DanciuAlexTeodor/Once-Human-Game/blob/main/SomeScripts/Jumpscares.cs)

-   Jumpscares are dynamically triggered when the player interacts with specific zones.
    
-   Controlled through a modular **Jumpscares** script:
    
    -   Spawns a monster near the player with calculated **position and rotation offsets**.
        
    -   Locks player movement and rotates the camera towards the monster’s head.
        
    -   Plays synced **jumpscare animations** and **audio stingers**.
        
    -   After the scare, the player is **pushed back** with camera tilt to simulate impact.
        
    -   Other jumpscares in the same area are **disabled to avoid repetition**.
        
-   Supports two modes:
    
    -   **Immediate** jumpscare when entering a trigger.
        
    -   **Remote** call (script-triggered) for cinematic sequences.
        
-   Integrated with:
    
    -   **Health reduction**, fall sound, post-processing.
        
    -   **Walkie-talkie triggers** after first encounter.
----------



### 🔊 Audio & Feedback

-   Spatial **3D audio** design with **audio zones** and **reverb areas**.
    
-   Custom soundtrack **timed to scenes** and **jumpscare triggers**.
    
-   Use of **walkie-talkie** for in-game scripted dialogue with a helper character.
    
-   Carefully layered ambient sounds (drips, whispers, electrical hums).
    
-   **Heartbeat and exhaustion** audio feedback for immersive player stress states.
    

----------

### 🖼️ UI/UX & Animations

-   UI designed with **tweened transitions** and **responsive menus**.
    
-   Animated interface elements for:
    
    -   Menu open/close
        
    -   Scene transitions
        
    -   Pause and settings menu
        
-   **In-world object animations** include:
    
    -   Doors, lockers, drawers, switches
        
    -   Synced with animation curves and sound effects
        
-   **Interactable highlight system** for objects in the environment
    
-   Realistic **camera shake** and **object interaction feedback**.
    

----------

> This technical structure delivers not only performance and polish but also a deeper immersion into the horror experience Once Human offers.
