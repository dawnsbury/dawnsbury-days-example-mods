## How to install mods
To enable one of the example mods, rename its DLL file from `.dll-disabled` to `.dll`.

To enable a mod you downloaded, add its DLL file to the CustomMods folder in your installation folder. 

Mods don't run in any sandbox. They're executable .NET code which has full privileges on your computer. You should download mods only from authors or platforms you trust.

## How to create a mod
1. Create a new .NET class library project, for example using Microsoft Visual Studio.
2. Set the platform to `x64`, platform target to `x64` and the target framework to `net6.0-windows`.
3. Reference the assembly `Data/Dawnsbury Days.dll` in your installation folder as an assembly reference. Make sure to reference the DLL file that's in your Data folder. The EXE file in the root of the installation folder is only a launcher which doesn't contain actual Dawnsbury Days code.
4. In any one class in your project, add a public static method annotated with the attribute `DawnsburyDaysModMainMethodAttribute`. Dawnsbury Days will invoke that method when it starts up.
5. Add any code you want your mod to execute on startup into that method. For example, you can use `ModManager.AddFeat(...)` to add custom feats, ancestries, etc. to the game.

You can then build your project and put the output assembly .dll file into the `CustomMods` folder in the installation folder, then start Dawnsbury Days. Your mod should be loaded.

It may be useful for you to set up a post-build setup that will automatically place your output assembly into the CustomMods folder for faster iteration. The file `Dawnsbury.Mod.targets` in this folder can help you with that.

Or, instead of creating a .NET class library project from scratch, you can copy the project `Dawnsbury.Mods.Feats.General.ImpossibleToughness` from this folder, which is a small "Hello, world"-style project, and start by modifying it. See the [Example mods](#example-mods) section just below.

## Example mods

You can use the example mods in this folder as inspiration, or you can decompile the `Dawnsbury Days.dll` file in Data folder to check how built-in classes, feats and spells are implemented.

The example mods are:

* **Dawnsbury.Mods.Feats.General.ImpossibleToughness.** This is a "Hello, world!" mod which only adds one simple general feat.
* **Dawnsbury.Mods.Feats.General.ABetterFleet.** This mod shows how to replace an existing feat.
* **Dawnsbury.Mods.Ancestries.Kobold.** This is a more advanced mod which adds an entire ancestry, its heritages and its ancestry feats and shows off some more advanced modding options.
* **Dawnsbury.Mods.Spellbook.AcidicBurst.** This mod shows how to add a new spell to the game.
* **Dawnsbury.Mods.Demo.Miscellaneous.** This mod shows miscellaneous ModManager endpoints, such how to replace an existing spell or how to add new items.
* **Dawnsbury.Mods.Variants.AutomaticBonusProgression.** This mod implements the [Automatic Bonus Progression variant rule](https://2e.aonprd.com/Rules.aspx?ID=1357) showing off some more advanced techniques.

## Dawnsbury Days solution architecture

**Assemblies.** Dawnsbury Days itself consists of two main assemblies, `Dawnsbury Days.dll` and `Common.dll`, both in the Data folder. The `Dawnsbury Days.exe` file in the Data folder is a native launcher for that DLL file and you can't reference it. The `Dawnsbury Days.exe` file in the main game folder is a .NET Framework launcher that does nothing except launch the Dawnsbury Days.exe file in the Data folder. You can't reference it either. The main assembly is `Dawnsbury Days.dll` and it contains xmldoc documentation for many important public classes and methods in the file `Dawnsbury Days.xml` right next to it. Your IDE's IntelliSense should pick it up automatically when you reference the assembly.

**Namespaces.** Dawnsbury Days is split into the following main namespaces:

* **Audio.** Contains the static class `Sfxs` that you can use to play music, voice lines and sound effects.
* **Auxiliary.** Contains the Auxiliary framework, which is built on top of Monogame and is responsible for input/output and drawing.
* **Campaign.Encounters.** Defines and loads adventure path encounters, the tutorial and random encounters.
* **Campaign.Path.** Defines and handles savegames, player progression through the adventure path and what happens during each "campaign stop" such as an encounter, a long rest or a level up.  
* **Core.** This is the main namespace mods may want to use. It contains the main rules subsystem, the definitions of feats, spells, monsters, combat actions and generally implements the PF2E ruleset. It contains many child namespaces.
  * **Animations.** Contains code for particles and creatures moving across screen, as well as code for cutscenes.
  * **CharacterBuilder.** Contains code for `CharacterSheet`, which represents a player character's character sheet and all the classes it needs. This system is further described in [Dawnsbury Days rules system architecture](#dawnsbury-days-rules-system-architecture).
  * **CharacterBuilder.FeatsDb.** The classes in this namespace implement ancestries, backgrounds, classes, class features, feats, impulses, and spells.<br><br>If you want to create a mod that adds a new feature like this, you can use the existing definitions here for inspiration and add the new feature using `ModManager.AddFeat`.<br><br>If you want to update an existing feature, such as by adding a new heritage to an existing ancestry or changing the rules or properties of a feat or feature, you can make those changes to the static property `AllFeats.All` as demonstrated by the example mod `Dawnsbury.Mods.Feats.General.ABetterFleet`.
  * **Coroutines.** This contains the fairly complex definition of the [async/await coroutine engine](#asyncawait-coroutine-engine). If you need to have a player make a choice during the resolution of a combat action in combat, and it can't be solved by targeting, you'll need to use the `await battle.SendRequest(...)` to ask the player to make a choice. If you need to take an action that might require a player choice—which is almost anything, because almost anything can be interrupted by a reaction which needs to ask the player whether to take it—then that action will also be _async_ and you will need to `await` it. You can find some of such common actions in `CommonSpellEffects`.
  * **Creatures.** This contains information about a creature in combat, as opposed to a player character's character sheet. Monsters don't have character sheets and during combat gameplay, even a player's character sheet is mostly irrelevant and in any case read-only, and the player characters are also represented as a `Creature`. Each character sheet is converted to a Creature as a combat begins.
  * **Intelligence.** This contains pathfinding code, as well as tactics used by the computer-controlled creatures. Each creature has its own instance of the `AI` class, which determines how it values each option it can take: Each time a creature is asked to make a choice, such as if it's that creature's turn and it can move into various spaces or take various combat actions, code in this namespace evaluates most possible choices and then causes the creature to select the option with the highest value (called "AI usefulness" in the code). The creature's particular values in its AI class instance determine how it values each option. For example, normal enemies value flanking, but mindless enemies don't value flanking at all.
  * **Mechanics.** This large namespace deals with making "main checks" (attack rolls and saving throws), dealing damage, targeting, traits, bonuses and penalties, and items. The class _CombatActionExecution_ wraps the evaluation of each combat action and uses many of these things. Determining the final check bonus and the final DC is done mostly by the static class _Checks_.
  * **Possibilities.** When it's your turn, the bottom bar of the in-game screen shows all the "possibilities" that you have. Two possibilities are the most common: the ActionPossibility, which enables you to take a combat action, and the SubmenuPossibility, which expands a secondary (or potentially tertiary) bottom bar. 
  * **Roller.** These classes deal with die rolling.
  * **StatBlocks.** These classes contain the definition of each monster, NPC or interactible item or obstacle.
  * **Tiles.** The code of Dawnsbury Days doesn't use feet, but instead uses "tiles" or "squares", and this namespace contains code for the battlemap and the tiles.
* **Display.** This namespace and its child namespaces contain code that draws data on screen, manipulates text, controls drag-and-drop and it also contains some user interface controls that are not part of Auxiliary, such as the ScrollPane.
* **IO.** Contains auxiliary classes and methods for save/load, serialization, settings, logging, telemetry and other interaction with the outside world.
* **Modding.** Contains helper classes meant to help with creating custom mods. All classes in this namespace are fully documented.
* **Phases.** Contains so-called "phases" which represent screens. For example, MainMenuPhase represents the main menu, SettingsPhase represents the settings window and BattlePhase represents the main in-game screen. The child namespace CampaignViews contains the sections of the adventure path, such as Retraining and Shop; and the child namespace CharacterBuilderPages contains the user interface controls of the character builder, such as feat selection, spell selection or daily preparations.

## Dawnsbury Days rules system architecture

All the code that pertains to combat rules, classes, feats, spells, monsters, maps and actions in combat is in the namespace `Dawnsbury.Core`. 

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

## Troubleshooting

**Missing async.** Some decompilers, such as the JetBrains decompiler, omit the keyword "async" in their decompiled async lambda methods. If you copy-paste such decompiled code into your project, it will either fail to compile (if it contains an `await` expression) or it will function incorrectly and produce a warning. To fix this, and as a general rule, if a method signature contains `Task` or `Task<something>` as the return value, you must declare that method as `async`. See [Async/await coroutine engine](#asyncawait-coroutine-engine).

**System.Drawing.Color vs. Microsoft.Xna.Color.** If your mod adds visual elements, such as a flyout overhead, it may need to specify a color using the type `Microsoft.Xna.Color`. To refer to that type, your project must either reference the NuGet package `MonoGame.Framework.WindowsDX` or must reference the assembly `MonoGame.Framework.dll` that you'll find in the `Data` folder of the game, in the same folder as `Dawnsbury Days.dll`. To fix this error, add the `Data/MonoGame.Framework.dll` assembly as a reference to your project.

**Missing IllustrationName.** If your mod adds something that needs an illustration, such as a new item or QEffect, you may want to use an existing illustration by referring to its `IllustrationName`. This enum is in the `Common.dll` library that you'll find in the `Data` folder of the game and must also reference. To fix this error, add the `Data/Common.dll` assembly as a reference to your project.

## Licensing

The MIT license applies only to the example mods in this folder. It doesn't apply to Dawnsbury Days as whole. It also doesn't apply to any images or sounds in this folder which are under a separate non-open-source license.

However, I give permission to decompile, use and modify the decompiled code specifically for the purpose of creating Dawnsbury Days mods. For example, if you want to adjust how some feat works, it is okay to copy its decompiled code into your own mod, or to use it as a base for your own feat. 

## Support

I'm `dawnsbury` on Discord and I'll be happy to answer any questions or help you with creating mods in the `#mod-support` channel on [the Dawnsbury Discord server](https://discord.gg/MnPp8z2epk).