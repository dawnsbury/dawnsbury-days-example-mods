## How to install mods
To enable one of the example mods, renamed its DLL file from `.dll-disabled` to `.dll`.

To enable a mod you downloaded, add its DLL file to the CustomMods folder in your installation folder. 

Mods don't run in any sandbox. They're executable .NET code which has full privileges on your computer. You should download mods only from authors or platforms you trust.

## How to create a mod
1. Create a new .NET class library project, for example using Microsoft Visual Studio.
2. Set the platform to `x64`, platform target to `x64` and the target framework to `.net6.0-windows`.
3. Reference the assembly `Data/Dawnsbury Days.exe` in your installation folder as an assembly reference. Make sure to reference the file that's in your Data folder. The file in the root of the installation folder is only a launcher which doesn't contain actual Dawnsbury Days code.
4. In any one class in your project, add a public static method annotated with the attribute `DawnsburyDaysModMainMethodAttribute`. Dawnsbury Days will invoke that method when it starts up.
5. Add any code you want your mod to execute on startup into that method. For example, you can use `ModManager.AddFeat(...)` to add custom feats, ancestries, etc. to the game.

You can then build your project and put the output assembly .dll file into the `CustomMods` folder in the installation folder, then start Dawnsbury Days. Your mod should be loaded.

It may be useful for you to set up a post-build setup that will automatically place your output assembly into the CustomMods folder for faster iteration. The file `Dawnsbury.Mod.targets` in this folder can help you with that.

## Dawnsbury Days rules system architecture

All the code that pertains to combat rules, classes, feats, spells, monsters, maps and actions in combat is in the namespace `Origin.Core`. 

### Characters
A character that you can build is represented by a `CharacterSheet` in the campaign and in the random encounter mode menu, and as a `Creature` during an encounter. Enemies are represented as `Creature` and don't have character sheets.

Almost all choices you make during character creation, except for spells and choosing ability scores, are represented as a `Feat`. Your ancestry, heritage, background, class, subclass, deity, domain and bloodline are `Feat`, as are general feats, class feats and ancestry feats. These "true feats" are represented as `TrueFeat`.

The way a character sheet's values (`CharacterSheet.Calculated`) and choices are determined is called recalculation and is done by `CharacterSheet.Recalculate`. Essentially, a character sheet begins with three choices, "ancestry", "background" and "class". Each choice allows you to choose a feat (remember, everything is a feat), and each feat may have an `.OnSheet` action and an `.OnCreature` action. 
* The `.OnCreature` action is executed at the very end of recalculation and updates the creature's statistics and abilities during encounters, when it's represented as a Creature. 
* The `.OnSheet` action is executed at an appropriate point of recalculation and may cause more choices to be given to you. For example, choosing cleric as your class will give you a "doctrine" choice. Recalculation proceeds from ancestry, background and class through choices in the order they're generated, applying them as it goes, until all choices are applied or missing, at which point `.OnCreature` actions of all feats are executed and recalculation concludes.

When a character sheet is serialized into your save file (during the campaign) or into your character library (during random encounter mode), only the selections you make for each choice are serialized, not the calculated values themselves.

### Combat actions

Each action a creature can take is a `CombatAction`. A combat action represents a spell, feat or action in combat directly usable by a creature, whether it needs zero actions, one action or more. For example, 'Strike (+1 longsword)', 'Demoralize' and 'Magic Missile' are combat actions. Combat actions are usually wrapped in a `Possibility` object, which determines how they're displayed in the bottom bar.

A combat action contains a `Target` object which represents targeting requirements or area of effect of the action, traits and whether the main roll is an attack roll, a saving throw or none. It also includes the effect of the combat action.

Spells are a good way to learn about how different kinds of effects can be coded. Look for them in the types `Cantrips`, `Level1Spells` and `Level2Spells`, or you can look at fighter feats for some other kinds of actions  in `FighterFeatsDb`.

### Async/await coroutine engine

Dawnsbury Days use the `async`/`await` keywords to implement coroutines and allow animations and player choices to be made within the code that resolves an effect. Despite the name of the keywords, the rules system code of Dawnsbury Days is single-threaded. 

In Dawnsbury Days rules code, such as within the effect code of a combat action, `await` means that the rules code acknowledges that the called method may take a long time (e.g. if it's an animation or a request for player to take an action) and suspends further rules code execution until that animation completes or the player makes a decision. 

Some `async` methods don't run animations or request decisions but are async anyway because they _might_ play an animation or request a decision, for example, if they contain an extension point that a creature's ability might use to interrupt an action in progress.

As a rule, if a method returns a Task, always `await` it.

### QEffects and the state-check

Conditions, auras, passive abilities, temporary markers and other status that needs to be attached to a creature are represented as a `QEffect`. A QEffect has an owner, an expiration, and a large number of delegate properties (events) that generally tend to be empty. These properties are extension points that a QEffect can use to affect the game state.

For example, you may set a QEffect's `BonusToDefenses` property to add a bonus to its owner's saving throws or AC; or you may set a QEffect's `AfterYouDealDamage` property to code that will be executed each time its owner deals at least 1 damage to a creature.

One of the these property is called `StateCheck` and it's performed during each state-check.

State-check (the name comes from "state-based effects", a similar concept from Magic: the Gathering) is an event that happens each time a creature takes its turn, each time a combat action ends and at some other times, and it recalculates the current values of all properties of all creatures. 

A state-check proceeds like this:
1. For each creature, many calculated properties, such as weaknesses, resistances and all effects with the expiration time of `Ephemeral` are removed.
2. For each creature, the `StateCheck` code of each of its QEffects applies.
3. If at least one StateCheck event applied during step 2, the state-check repeats from step 2. This way, you can use a StateCheck event to create another Ephemeral QEffect on yourself or another creature.
4. When no more state-check events apply during step 3, the state-check ends.

## Licensing

The MIT license applies only to the examples mods in this folder. It doesn't apply to Dawnsbury Days as whole. 

However, I give permission to decompile, use and modify the decompiled code specifically for the purpose of creating Dawnsbury Days mods. For example, if you want to adjust how some feat works, it is okay to copy its decompiled code into your own mod, or to use it as a base for your own feat. 

## Support

I'm `dawnsbury` on Discord and I'll be happy to answer any questions or help you with creating mods in the `#mod-support` channel on [the Dawnsbury Discord server](https://discord.gg/MnPp8z2epk).