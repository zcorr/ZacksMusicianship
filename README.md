# Zack's Musicianship

`Zack's Musicianship` is a Terraria / tModLoader mod project centered on turning music theory into moment-to-moment gameplay.

At the time of writing, the project is built around a playable prototype weapon, `Woodcord`, and a companion `Songbook` system that tracks harmonic discoveries such as cadences and progression shapes. The mod is intentionally early-stage, system-heavy, and experimental. It is not trying to be a content dump yet. It is trying to build a strong foundation for a music-driven combat sandbox inside Terraria where weapons can be customized through chord structures and progressions that also sound good in-game.

This repository is also something more specific than "just a mod": it is an ongoing project to build AI software engineering skill and push it to its limits. That means using a real codebase, real constraints, real bugs, real refactors, real art and UI work, and real design iteration to stress-test what AI-assisted engineering can actually do when the task is messy and multidisciplinary.

## What This Project Is

This mod asks a simple question:

What would a Terraria content mod look like if music theory was not just flavor text, but the actual logic layer behind weapons, progression, UI, and player discovery?

The current answer is:

- a melee instrument weapon that changes behavior based on the chord it is using
- a progression editor that lets the player build a short harmonic loop
- cadence-aware combat logic
- a discoverable in-game journal that logs musical structures the player has performed
- custom UI built specifically for those systems instead of relying only on vanilla item behavior
- a long-term progression model where later weapons gain more musical capability, more expressive range, and more customization than early ones

The goal is not to bolt a few notes onto a sword and call it done. The goal is to make harmony, progression, tension, release, and musical memory part of the actual game structure.

## Why It Exists

There are two parallel goals behind this repository.

### 1. Build a real music-centered Terraria mod

The mod side of the project is trying to create an identity that feels distinct from conventional content mods. The long-term vision is a set of instruments, musical combat verbs, progression systems, and discovery mechanics that feel coherent rather than gimmicky.

### 2. Push AI software engineering under real pressure

This repository is explicitly a proving ground for AI-assisted development.

It is meant to answer practical questions such as:

- Can AI help design systems, not just isolated files?
- Can AI survive repeated refactors as the design changes?
- Can AI work across gameplay logic, UI, save data, networking, pixel art, and documentation in one project?
- Can AI produce useful code when the requirements are underspecified, contradictory, or evolving?
- Can AI be treated like a serious software engineering tool instead of just a code snippet generator?

This means the repo is intentionally used for tasks that are awkward, stateful, and cross-cutting:

- custom tModLoader UI
- progression and save-state logic
- multiplayer sync paths
- pixel-art asset creation
- sound and interaction design
- design iteration based on in-game feel
- documentation and internal workflows

If this project succeeds, it will not be because AI wrote a lot of code. It will be because AI was pushed into the kind of iterative engineering work where correctness, structure, and adaptation actually matter.

## Current State

The mod is currently a prototype with one main weapon ecosystem and one supporting discovery item.

### Woodcord

`Woodcord` is the first flagship item in the mod.

It is a guitar-like melee weapon made from early-game materials and built around chord-based behavior. Instead of behaving like a normal sword with a single fixed profile, it changes how it attacks depending on the active chord quality.

At a high level, the weapon currently supports:

- chord-based behavior changes
- a saved chord progression system
- custom projectile behaviors for different harmonic qualities
- phrase-based cadence charging for empowered attacks
- a custom UI for editing a progression

The intent is to make the weapon feel like an instrument first and a weapon second, while still remaining playable inside Terraria's combat rhythm.

### Songbook

The `Songbook` is the second major system item currently implemented.

It functions as an in-game journal for harmonic discoveries. As the player performs chord phrases and cadence patterns with `Woodcord`, the book unlocks entries and reveals descriptions, formulas, and examples.

Right now the Songbook is:

- a real item
- craftable with `1 Wood`
- granted to new characters on creation
- backed by persistent player save data
- synchronized for multiplayer state
- equipped with a custom full-screen UI inspired by bestiary-style navigation

The purpose of the Songbook is not just lore. It is the beginning of a progression-memory system for the whole mod.

## Core Design Direction

The project is moving toward a few specific principles.

### Music theory should drive mechanics

Names like `major`, `minor`, `sus4`, `ii-V-I`, or `authentic cadence` are not just labels. They should correspond to actual playable structures, actual combat behavior, and actual discovery rules.

### Systems should teach through play

The player should be able to discover musical ideas through combat and experimentation, not only through reading external documentation. A player who spends time with the mod should gradually absorb real ideas about chords, progressions, resolution, cadence, and harmonic function simply by using the weapons.

### Custom UI is part of the mod identity

If a mechanic needs a real interface, the mod should build one instead of forcing everything through vanilla conventions.

### Terraria readability matters

Even when the systems are complex, the mod should still look and feel like it belongs in Terraria:

- readable sprites
- compact item silhouettes
- understandable tooltips
- strong combat feedback
- low-friction controls

### Weapon depth should grow with game progression

Early-game instruments should be simpler and more readable. As the player moves deeper into Terraria, the instruments should gain broader musical vocabulary, richer customization, and more expressive progression control.

That means power progression should not just be numerical. It should also be musical:

- more chord control
- more progression control
- more expressive attack routing
- more advanced harmony systems
- more rewarding sound design

### AI work should be held to engineering standards

This is not a repository for dumping AI output into version control. The goal is to make AI-produced work survive:

- compile checks
- design changes
- refactors
- runtime debugging
- cross-file consistency
- real player feedback

## Where It Is Going

The current codebase is a foundation, not the final shape of the mod.

The likely direction from here includes:

### More instruments

`Woodcord` is only the first weapon. The larger vision is a family of instruments that express different harmonic or rhythmic ideas through Terraria combat.

The most important part of that future is not just "more items." It is a ladder of musical complexity across the game. Early weapons should introduce core ideas cleanly. Later weapons should open up more customization and more interesting harmonic space without losing usability.

Possible future directions include:

- percussion-based weapons
- wind or resonance-based ranged instruments
- support or summoner-adjacent instruments
- midgame and late-game instrument upgrades
- weapons with broader chord palettes and deeper progression editing
- weapons whose musical complexity scales alongside Terraria progression

### Deeper harmony systems

The current system focuses on a manageable subset of chord qualities and cadence patterns. Over time, the mod should expand into richer harmonic vocabulary without making the interface unusable.

That could include:

- more chord qualities
- inversions or voicing identity
- longer progressions
- key-aware systems
- more nuanced cadence recognition
- better-sounding in-game musical results as customization gets deeper

### A broader Songbook

The Songbook should become a true in-game record of musical knowledge and player experimentation.

Long-term, that could mean:

- more cadence families
- progression archetypes beyond cadences
- unlocking lore or system hints through discovery
- progression-based rewards
- stat or combat interactions tied to musical mastery

### Better presentation

The current project already includes custom UI and custom pixel assets, but presentation is still prototype-grade in many places.

Future work should improve:

- sprite consistency
- slash and projectile readability
- audio feel
- menu layout polish
- localization coverage
- iconography and visual identity

### Stronger engineering infrastructure

Because this project doubles as an AI engineering testbed, it also needs stronger repo discipline over time.

That likely includes:

- better top-level documentation
- cleaner code organization
- more internal authoring guides
- build/reload workflows
- regression checks
- versioned design notes

## Why This Repo Matters as an AI Engineering Project

There are plenty of toy examples where AI can produce a nice result in one file. That is not what this repository is trying to measure.

This project matters because it forces AI-assisted development into uncomfortable territory:

- requirements change after the first implementation
- gameplay feel invalidates technically correct code
- UI and art problems are intertwined with systems problems
- save data and multiplayer sync matter
- naming and structure accumulate across many files
- the project needs documentation that explains intent, not just implementation

That is where software engineering starts to get real.

The repository is therefore useful even when features are rough, because each rough edge exposes a concrete question about what AI can or cannot yet do well:

- Can it preserve design intent across refactors?
- Can it recover from wrong first attempts?
- Can it maintain consistency as the surface area grows?
- Can it shift from implementation to explanation to art direction and back again?
- Can it participate in iterative creative engineering rather than only deterministic tasks?

The aspiration is not to prove that AI replaces engineers.

The aspiration is to force AI into an environment where shallow competence is not enough.

## Repository Structure

This repository currently follows a conventional tModLoader mod layout with custom systems layered on top.

Top-level structure:

- `Content/`
  - gameplay-facing items, projectiles, buffs, and textures
- `Common/`
  - shared systems such as players, UI, cadence logic, chord logic, and mod systems
- `Localization/`
  - localizable content strings
- `Docs/`
  - internal working documentation such as item authoring notes

Some notable files and folders:

- `Content/Items/Woodcord.cs`
  - the main prototype weapon
- `Content/Items/Songbook.cs`
  - the cadence journal item
- `Common/Cadences/CadenceLibrary.cs`
  - cadence definitions and matching rules
- `Common/Players/GuitarSwordPlayer.cs`
  - progression and cadence combat state
- `Common/Players/SongbookPlayer.cs`
  - discovery persistence and starter inventory
- `Common/UI/ChordComposerUIState.cs`
  - progression editor UI
- `Common/UI/SongbookUIState.cs`
  - cadence journal UI
- `Docs/ITEM_AUTHORING.md`
  - working notes for adding future items to the mod

## Development Notes

This is an active prototype codebase. Expect design churn.

That means:

- systems may be refactored aggressively
- item behavior may change to match feel, not just theory
- UI layout may be redesigned multiple times
- cadence rules may evolve as the combat model evolves

That is intentional.

The point of the project is not to lock too early. The point is to keep pushing until the design, code, and tooling can survive complexity.

## Building / Compiling

The project is built as a tModLoader mod source project.

Current compile command used in local iteration:

```bash
dotnet msbuild '/Users/zackcorr/Library/Application Support/Terraria/tModLoader/ModSources/ZacksMusicianship/ZacksMusicianship.csproj' /t:Compile /p:BuildMod=false /p:TargetFramework=net8.0 /p:LangVersion=12.0
```

Typical workflow:

1. edit code or assets in the mod source folder
2. compile the project
3. reload the mod in tModLoader
4. test the feature in-game
5. refine behavior based on actual feel

## What Success Looks Like

A successful version of this repository would become all of the following at once:

- a genuinely interesting Terraria mod
- a coherent music-driven combat sandbox
- a maintainable experimental codebase
- a record of iterative AI-assisted engineering under real constraints
- a game system that teaches real chord and music theory concepts through use rather than through lectures

In other words, success is not just "more content."

Success is:

- better systems
- better readability
- better design language
- better engineering discipline
- weapons that become more musically capable as the player advances
- progression editing that is both expressive and satisfying to hear in real gameplay
- a mod that feels genuinely novel inside Terraria instead of feeling like a themed reskin
- better evidence for what AI-assisted software development can actually accomplish

## Current Disclaimer

This project is early.

Some parts are already real and playable. Some parts are still rough. Some parts will likely be reworked entirely. That is normal for the current stage of the mod and for the AI-engineering mission behind it.

If you are here because you care about Terraria modding, music systems, experimental UI, or AI-assisted game development, this repository is meant to be a serious sandbox for all of those things.

## Short Version

`Zack's Musicianship` is a Terraria mod prototype about turning music theory into combat systems, progression, customization, and discovery.

It is also a deliberate attempt to use a real game project to train, pressure-test, and push AI software engineering beyond easy demo tasks and into the kind of work where iteration, structure, debugging, and design judgment actually matter.
