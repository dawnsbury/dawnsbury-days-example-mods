using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Coroutines.Requests;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.StatBlocks;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.Classes.Portalist;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
public static class PortalistClassLoader
{
    private const string CREATE_A_PORTAL = "Create a Portal";

    // Static initializers run first and register all our custom enum members that we use from multiple places:
    
    private static Trait TPortalist = ModManager.RegisterTrait("Portalist", new TraitProperties("Portalist", true));
    
    private static QEffectId QAllyPortal = ModManager.RegisterEnumMember<QEffectId>("QAllyPortal");
    private static QEffectId QBoomerangPortal = ModManager.RegisterEnumMember<QEffectId>("QBoomerangPortal");
    private static QEffectId QAttackPortal = ModManager.RegisterEnumMember<QEffectId>("QAttackPortal");
    private static QEffectId QCoveringPortal = ModManager.RegisterEnumMember<QEffectId>("QCoveringPortal");
    private static QEffectId QDoubleHopPortal = ModManager.RegisterEnumMember<QEffectId>("QDoubleHopPortal");
    private static QEffectId QElementalBlastPortal = ModManager.RegisterEnumMember<QEffectId>("QElementalBlastPortal");
    private static QEffectId QShieldingPortal = ModManager.RegisterEnumMember<QEffectId>("QShieldingPortal");
    private static QEffectId QSummoningPortal = ModManager.RegisterEnumMember<QEffectId>("QSummoningPortal");

    // We're adding one custom illustration, the rest of the pictures come from Dawnsbury Days core game so we can refer to them with IllustrationName:
    private static ModdedIllustration IllPortal = new ModdedIllustration("PortalistAssets/CreatePortal.png");
    
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        foreach (var feat in CreateFeats())
        {
            ModManager.AddFeat(feat);
        }
    }

    private static IEnumerable<Feat> CreateFeats()
    {
        // Almost everything in Dawnsbury Days is a feat. Class definitions are feats. Here we declare the portalist as a new class:
        yield return new ClassSelectionFeat(ModManager.RegisterFeatName("FeatPortalist", "Portalist"),
                "The portalist is the ultimate mobile combatant. Portalists open dimensional fissures and leap through them on battlefield to attack foes. Portalists are small scale teleporters, perfect for setting a flank or being in the right place at the right time. The portalist is not a wizard, but rather a lightly armored warrior who excels at speed and perfect positioning.", TPortalist,
                new LimitedAbilityBoost(Ability.Strength, Ability.Dexterity),
                8, [Trait.Fortitude, Trait.Will, Trait.Simple, Trait.Rapier, Trait.Shortsword, Trait.Kukri, Trait.Unarmed, Trait.UnarmoredDefense, Trait.LightArmor], [Trait.Perception, Trait.Reflex], 3,
                @"{b}1. Standard portal.{/b} You can spend an action to teleport up to your speed to a square you can see. This is a flourish action that doesn't provoke attacks of opportunity.

{b}2. Quick.{/b} You gain a +1 status bonus to initiative rolls, if you're wearing no armor or light armor only.

{b}3. Portalist feat.{/b} You choose and get a portalist feat. Portalist feats often allow you to create more powerful or specialized portals instead of your standard portal. 

{b}At higher levels:{/b}
{b}Level 2:{/b} Portalist feat
{b}Level 3:{/b} General feat, skill increase, fast movement {i}(you gain a +10-foot status bonus to your Speed while you're wearing no armor or only light armor){/i}
{b}Level 4:{/b} Portalist feat", null)
            .WithOnSheet(sheet =>
            {
                sheet.AddSelectionOption(new SingleFeatSelectionOption("PortalistFeat1", "Portalist feat", 1, (ft) => ft.HasTrait(TPortalist)));
            })
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect()
                {
                    ProvideMainAction = qf =>
                    {
                        return Wrap(CreateNormalPortal(qf.Owner, IllPortal, "Standard Portal", "Teleport to a square you can see within range.")
                            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                            {
                                PortalistTeleport(caster, targets.ChosenTile!);
                            }));
                    }
                });
                creature.AddQEffect(new QEffect("Quick", "You have +1 to initiative.")
                {
                    BonusToInitiative = (qf) => new Bonus(1, BonusType.Status, "Quick")
                });
                if (creature.Level >= 3)
                {
                    creature.AddQEffect(new QEffect("Fast movement", "You have +10 to Speed if you're not wearing armor or are wearing only light armor.")
                    {
                        BonusToAllSpeeds = qf =>
                        {
                            if (!creature.Armor.WearsArmor || (creature.Armor.Item?.HasTrait(Trait.LightArmor) ?? false))
                            {
                                return new Bonus(2, BonusType.Status, "Fast movement");
                            }
                            return null;
                        }
                    });
                }
            });
        // And here we define all the portalist class feats, its portal tricks. Generally, most of these tricks will use CreateNormalPortal to create the basics of
        // the action and then modify it:
        yield return new TrueFeat(ModManager.RegisterFeatName("Ally Portal"), 1, "You pull your friend alongside you.",
                "Choose a target square, then choose an adjacent ally.\n\nYou teleport as normal, then you pull your ally through the portal to an adjacent square of your choice.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(IllPortal)
            .WithPermanentQEffect("You can bring an ally with you through a portal.", qf =>
            {
                qf.Id = QAllyPortal;
                qf.ProvideMainAction = qff =>
                {
                    var action = CreateNormalPortal(qff.Owner, IllPortal, "Ally Portal", "Choose a target square, then choose an adjacent ally.\n\nYou teleport as normal, then you pull your ally through the portal to an adjacent square of your choice.");
                    ((TileTarget)action.Target).AdditionalTargetingRequirement = (creature, tile) => creature.Occupies != null && creature.Occupies.Neighbours.Creatures.Any(cr => cr.FriendOf(creature)) ? Usability.Usable : Usability.NotUsable("You don't have an adjacent ally.");
                    action.WithEffectOnChosenTargets((async (spell, caster, targets) =>
                    {
                        var ally = await caster.Battle.AskToChooseACreature(caster, caster.Occupies.Neighbours.Creatures.Where(cr => cr.FriendOf(caster)), IllPortal, "Choose an ally to teleport alongside you.", "Teleport this ally.", "Cancel");
                        if (ally == null)
                        {
                            spell.RevertRequested = true;
                            return;
                        }

                        PortalistTeleport(caster, targets.ChosenTile!);
                        Tile? chosenTarget = null;
                        var response = await caster.Battle.SendRequest(
                            new AdvancedRequest(caster,
                                "Choose a square to teleport your ally to.",
                                caster.Occupies.Neighbours
                                    .Select(edge => edge.Tile)
                                    .Where(tile => tile.IsTrulyGenuinelyFreeTo(ally))
                                    .Select(tile => new TileOption(tile, "Place this ally here.", async () => { chosenTarget = tile; }, AIConstants.NEVER, true))
                                    .Concat(new Option[] { new PassViaButtonOption("Do not teleport ally") })
                                    .ToList())
                            {
                                TopBarText = "Choose a square to teleport your ally to.",
                                TopBarIcon = IllPortal
                            });
                        await response.ChosenOption.Action();
                        if (chosenTarget != null)
                        {
                            PortalistTeleport(ally, chosenTarget);
                        }
                    }));
                    return Wrap(action);
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Boomerang Portal"), 1, "You swiftly move in and out of combat.",
                "Teleport as normal, then make a single melee Strike, then teleport back to the square where you started.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(IllustrationName.AerialBoomerang256)
            .WithPermanentQEffect("You can teleport, make a melee Strike, then teleport back.", qf =>
            {
                qf.Id = QBoomerangPortal;
                qf.ProvideMainAction = qff =>
                {
                    var action = CreateNormalPortal(qff.Owner, IllustrationName.AerialBoomerang256, "Boomerang Portal", "Teleport to a square within range, then make a melee Strike, then teleport back.")
                        .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                        {
                            var originalPlace = caster.Occupies;
                            PortalistTeleport(caster, targets.ChosenTile!);
                            await CommonCombatActions.StrikeAdjacentCreature(caster);
                            PortalistTeleport(caster, originalPlace);
                        });
                    return Wrap(action);
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Attack Portal"), 1, "You come out of the portal with your weapons imbued with extradimensional energy.",
                "Teleport as normal, then you gain a +2 status bonus to your next attack this turn.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(IllustrationName.Swords)
            .WithPermanentQEffect("Teleport, then you gain a +2 status bonus to your next attack this turn.", qf =>
            {
                qf.Id = QAttackPortal;
                qf.ProvideMainAction = qff =>
                {
                    var action = CreateNormalPortal(qff.Owner, new SideBySideIllustration(IllPortal, IllustrationName.Swords), "Attack Portal", "Teleport to a square within range and gain a +2 status bonus to your next attack this turn.")
                        .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                        {
                            PortalistTeleport(caster, targets.ChosenTile!);
                            caster.AddQEffect(new QEffect("Attack Portal", "You have +2 to your next attack.", ExpirationCondition.ExpiresAtStartOfSourcesTurn, caster, IllPortal)
                            {
                                BonusToAttackRolls = (effect, combatAction, defender) => combatAction.HasTrait(Trait.Attack) ? new Bonus(2, BonusType.Status, "Attack Portal") : null,
                                AfterYouTakeAction = (async (effect, combatAction) =>
                                {
                                    if (combatAction.HasTrait(Trait.Attack))
                                    {
                                        effect.ExpiresAt = ExpirationCondition.Immediately;
                                    }
                                })
                            });
                        });
                    return Wrap(action);
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Covering Portal"), 1, "You summon a portal in such a way that it serves as cover from attacks.",
                "You may choose to teleport as normal. Whether or not you teleport, you gain a +2 circumstance bonus to your AC until you move or until the beginning of your next turn.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(IllustrationName.SteelShield)
            .WithPermanentQEffect("You may choose to teleport as normal. Whether or not you teleport, you gain a +2 circumstance bonus to your AC until you move or until the beginning of your next turn.", qf =>
            {
                qf.Id = QCoveringPortal;
                qf.ProvideMainAction = qff =>
                {
                    void AddShieldBonus(Creature caster)
                    {
                        caster.AddQEffect(new QEffect("Covering Portal", "You have +2 to AC until you move.", ExpirationCondition.ExpiresAtStartOfYourTurn, caster, IllustrationName.SteelShield)
                        {
                            BonusToDefenses = (effect, action, defense) => defense == Defense.AC ? new Bonus(2, BonusType.Circumstance, "Shielding Portal") : null,
                            AfterYouTakeAction = (async (effect, action) =>
                            {
                                if (action.HasTrait(Trait.Move) && !action.HasTrait(Trait.Flourish))
                                {
                                    effect.ExpiresAt = ExpirationCondition.Immediately;
                                }
                            })
                        });
                    }
                    return new SubmenuPossibility(IllustrationName.SteelShield, "Covering Portal")
                    {
                        Subsections =
                        [
                            new PossibilitySection("Covering Portal")
                            {
                                Possibilities =
                                [
                                    new ActionPossibility(new CombatAction(qff.Owner, IllustrationName.SteelShield, "Shield only", [TPortalist, Trait.Flourish, Trait.Basic],
                                        "You gain a +2 circumstance bonus to your AC until you move or until the beginning of your next turn.", Target.Self())
                                        .WithSoundEffect(SfxName.RaiseShield)
                                        .WithEffectOnEachTarget(async (spell, caster, target, result) => { AddShieldBonus(caster); })),
                                    new ActionPossibility(CreateNormalPortal(qff.Owner, new SideBySideIllustration(IllustrationName.SteelShield, IllPortal), "Teleport and shield", 
                                        "Teleport as normal, then you gain a +2 circumstance bonus to your AC until you move or until the beginning of your next turn.")
                                        .WithEffectOnChosenTargets((async (spell, caster, targets) =>
                                        {
                                            PortalistTeleport(caster, targets.ChosenTile!);
                                            AddShieldBonus(caster);
                                        }))
                                    )
                                ]
                            }
                        ]

                    }.WithPossibilityGroup(CREATE_A_PORTAL);
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Double-Hop Portal"), 1, "You create two portals, jump into the first one, then out the first one into the second.",
                "Teleport as normal. Then do it again.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(new SideBySideIllustration(IllPortal, IllPortal))
            .WithPermanentQEffect("Teleport as normal. Then do it again.", qf =>
            {
                qf.Id = QDoubleHopPortal;
                qf.ProvideMainAction = qff =>
                {
                    return Wrap(CreateNormalPortal(qff.Owner, new SideBySideIllustration(IllPortal, IllPortal), "Double-Hop Portal", "Teleport as normal. Then do it again.")
                        .WithActionCost(2)
                        .WithEffectOnChosenTargets((async (spell, caster, targets) =>
                        {
                            PortalistTeleport(caster, targets.ChosenTile!);
                            var portal = CreateNormalPortal(caster, IllPortal, "Second Portal", "Teleport again.").WithActionCost(0).WithEffectOnChosenTargets((async (action, creature, chosenTargets) =>
                            {
                                PortalistTeleport(creature, chosenTargets.ChosenTile!);
                            }));
                            await caster.Battle.GameLoop.FullCast(portal);
                        })));
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Elemental Blast Portal"), 2, "You cause the extradimensional matter of a portal to explode in an unstable vortex of elemental energy.",
                "Create a portal as normal, and deal 1d6 acid, electricity, fire, cold or sonic damage to each creature in the target square or adjacent to it (basic Reflex save against your class DC mitigates). Afterwards, you may choose to teleport there as normal.\n\nThe damage increases by 1d6 on level 3, and every two levels afterwards.", [TPortalist, Trait.Flourish])
            .WithActionCost(2)
            .WithIllustration(IllustrationName.EnergyEmanation)
            .WithPermanentQEffect("You can open a portal explosively to deal elemental damage.", qf =>
            {
                qf.Id = QElementalBlastPortal;
                qf.ProvideMainAction = qff =>
                {
                    return Wrap(CreateNormalPortal(qff.Owner, IllustrationName.EnergyEmanation, "Elemental Blast Portal", $"Create a portal as normal, and deal {S.HeightenedVariable((qff.Owner.Level + 1) / 2, 1)}d6 acid, electricity, fire, cold or sonic damage to each creature in the target square or adjacent to it (basic Reflex save against your class DC mitigates). Afterwards, you may choose to teleport there as normal.")
                        .WithActionCost(2)
                        .WithVariants(new[]
                        {
                            new SpellVariant("Acid", "Acid", IllustrationName.ResistAcid),
                            new SpellVariant("Cold", "Cold", IllustrationName.ResistCold),
                            new SpellVariant("Electricity", "Electricity", IllustrationName.ResistElectricity),
                            new SpellVariant("Fire", "Fire", IllustrationName.ResistFire),
                            new SpellVariant("Sonic", "Sonic", IllustrationName.ResistSonic)
                        })
                        .WithEffectOnChosenTargets((async (spell, caster, targets) =>
                        {
                            var damageKind = spell.ChosenVariant!.ToEnergyDamageKind();
                            var targetTile = targets.ChosenTile!;
                            await CommonAnimations.CreateConeAnimation(caster.Battle, targetTile.ToCenterVector(), new Tile[] { targetTile }.Concat(targetTile.Neighbours.Select(e => e.Tile)).ToList(), 20, ProjectileKind.Cone, IllustrationName.EnergyEmanation);
                            int dcClass = caster.PersistentCharacterSheet!.Class != null ? caster.Proficiencies.Get(caster.PersistentCharacterSheet.Class.ClassTrait).ToNumber(caster.Level)
                                                                                           + caster.Abilities.Get(caster.Abilities.KeyAbility) + 10 : 10;
                            int dcSpell = caster.Spellcasting != null ? caster.Proficiencies.Get(Trait.Spell).ToNumber(caster.Level) + 10 + caster.Spellcasting.Sources.Max(source => source.SpellcastingAbilityModifier) : 10;
                            int dc = Math.Max(dcClass, dcSpell);
                            foreach (var target in targetTile.Neighbours.CreaturesPlusCreatureOnSelf)
                            {
                                var save = CommonSpellEffects.RollSavingThrow(target, spell, Defense.Reflex, caster2 => dc);
                                await CommonSpellEffects.DealBasicDamage(spell, caster, target, save, ((caster.Level + 1) / 2) + "d6", damageKind);
                            }

                            if (await caster.Battle.AskForConfirmation(caster, IllPortal, "Teleport into the area of the elemental blast?", "Teleport"))
                            {
                                PortalistTeleport(caster, targetTile);
                            }
                        }))
                    );
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Shielding Portal"), 2, "You prepare to summon a portal at a moment's notice that would shunt away incoming projectiles.",
                "Until your next turn, if you'd be the target of a ranged attack (including a ranged spell attack, but not a spell that requires a save), you can spend {icon:Reaction} a reaction. If you do, the attack automatically misses as the projectile is deflected into a portal.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(IllustrationName.ForbiddingWard)
            .WithPermanentQEffect("You can prepare a reaction to deflect an incoming projectile.", qf =>
            {
                qf.Id = QShieldingPortal;
                qf.ProvideMainAction = qff =>
                {
                    return Wrap(new CombatAction(qff.Owner, IllustrationName.ForbiddingWard, "Shielding Portal", [TPortalist, Trait.Flourish, Trait.Basic], "Until your next turn, if you'd be the target of a ranged attack (including a ranged spell attack, but not a spell that requires a save), you can spend {icon:Reaction} a reaction. If you do, the attack automatically misses as the projectile is deflected into a portal.", Target.Self())
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            caster.AddQEffect(new QEffect("Shielding Portal", "Until your next turn, if you'd be the target of a ranged attack (including a ranged spell attack, but not a spell that requires a save), you can spend {icon:Reaction} a reaction. If you do, the attack automatically misses as the projectile is deflected into a portal.", ExpirationCondition.ExpiresAtStartOfYourTurn, caster, IllustrationName.ForbiddingWard)
                            {
                                FizzleIncomingActions = (async (effect, action, sb) =>
                                {
                                    if (action.HasTrait(Trait.Ranged) && action.HasTrait(Trait.Attack))
                                    {
                                        if (await effect.Owner.Battle.AskToUseReaction(effect.Owner, "You're targeted by " + action.Name + ". Use Shielding Portal to deflect this?"))
                                        {
                                            effect.Owner.Occupies.Overhead("deflected", Color.White, effect.Owner + " deflected the projectile with Shielding Portal.");
                                            sb.AppendLine("Projectile deflected by Shielding Portal.");
                                            return true;
                                        }
                                    }

                                    return false;
                                })
                            });
                        }));
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Summoning Portal"), 4, "You don't cross your portal and instead use it to call in creatures of energy.",
                "You summon an elemental creature whose level is 1 or lower.\n\nImmediately when you open this portal and then once each turn when you Sustain the portal, you can take two actions as the summoned creature. If you don't Sustain the portal during a turn, the summoned creature will go away.", [TPortalist, Trait.Flourish])
            .WithActionCost(3)
            .WithIllustration(IllustrationName.SummonElemental)
            .WithPermanentQEffect("You can summon elementals as per the {i}summon elemental{/i} spell.", qf =>
            {
                qf.Id = QSummoningPortal;
                qf.ProvideMainAction = qff =>
                {
                    return Wrap(new CombatAction(qff.Owner, IllustrationName.SummonElemental, "Summoning Portal", new[] { Trait.Conjuration, Trait.Arcane, Trait.Primal, TPortalist, Trait.Basic }, "You summon an elemental creature whose level is 1 or lower.\n\nImmediately when you open this portal and then once each turn when you Sustain the portal, you can take two actions as the summoned creature. If you don't Sustain the portal during a turn, the summoned creature will go away.", Target.RangedEmptyTileForSummoning(6))
                        .WithActionCost(3)
                        .WithSoundEffect(SfxName.Summoning)
                        .WithVariants(MonsterStatBlocks.MonsterExemplars.Where(animal => animal.HasTrait(Trait.Elemental) && animal.Level <= 1).Select(animal => new SpellVariant(animal.Name, animal.Name, animal.Illustration)).ToArray())
                        .WithCreateVariantDescription((_, variant) => RulesBlock.CreateCreatureDescription(MonsterStatBlocks.MonsterExemplarsByName[variant!.Id]))
                        .WithEffectOnChosenTargets((async (spell, caster, targets) => { await CommonSpellEffects.SummonMonster(spell, caster, targets.ChosenTile!); })));
                };
            });
    }

    /// <summary>
    /// Creates a no-effect combat action that serves as a base for the "create a portal" ability. Individual actions, such as the base action, and all the feats,
    /// can then use this and build on top of it.
    /// </summary>
    /// <param name="portalist">The portalist owning this action.</param>
    /// <param name="illustration">Illustration of this action.</param>
    /// <param name="name">Name of this action.</param>
    /// <param name="description">Full description of this action.</param>
    /// <returns></returns>
    private static CombatAction CreateNormalPortal(Creature portalist, Illustration illustration, string name, string description)
    {   
        var range = portalist.Speed; // In Dawnsbury Days, ranges are indicated in squares, not feet
        var target = new TileTarget((caster, tile) =>
            tile.IsFree
            && caster.Occupies?.DistanceTo(tile) <= range 
            && caster.Occupies.HasLineOfEffectToIgnoreLesser(tile) < CoverKind.Blocked, null)
        {
            OverriddenTargetLine = "{b}Range{/b} " + (range*5) + " feet" // TileTarget normally doesn't create target lines automatically, so we have to write one ourselves
        };
        List<Trait> traits = [TPortalist, Trait.Move, Trait.Flourish];
        if (name != "Standard Portal")
        { 
            // The Basic trait prevents the action from being shown in the OFFENSE section of the rules block, which is what we want because the 
            // action is already describes under the ABILITIES section:
            traits.Add(Trait.Basic);
        }
        return new CombatAction(portalist, illustration, name, traits.ToArray(), description, target)
            .WithSoundEffect(SfxName.PhaseBolt);
    }

    /// <summary>
    /// Teleports the PORTALIST onto the TARGET.
    /// </summary>
    private static void PortalistTeleport(Creature portalist, Tile target)
    {
        if (!target.IsTrulyGenuinelyFreeTo(portalist))
        {
            target = target.GetShuntoffTile(portalist);
        }
        portalist.TranslateTo(target);
        portalist.AnimationData.ActualPosition = new Vector2(target.X, target.Y);
        portalist.AnimationData.ColorBlink(Color.White);
        portalist.Battle.SmartCenterAlways(target);
    }

    /// <summary>
    /// All "Create a Portal" actions are wrapped with these modifications before being returned to the core game. This wrapping makes them all part
    /// of the same possibility group so that they get "merged together" if action combining is enabled in Settings (it is by default). It also causes
    /// Variants to work, which is needed because normally Variants only work for spells. 
    /// </summary>
    private static Possibility Wrap(CombatAction portalAction)
    {
        if (portalAction.Variants != null)
        {
            ChooseVariantThenActionPossibility CreateVariantPossibility(SpellVariant variant)
            {
                return new ChooseVariantThenActionPossibility(portalAction, variant.Illustration, variant.Name, variant, portalAction.Target.CanBeginToUse(portalAction.Owner), PossibilitySize.Full);
            }

            return new SubmenuPossibility(portalAction.Illustration, portalAction.Name, PossibilitySize.Full)
            {
                SpellIfAny = portalAction,
                Subsections =
                {
                    new PossibilitySection(portalAction.Name)
                    {
                        Possibilities = portalAction.Variants.Select(CreateVariantPossibility).Cast<Possibility>().ToList()
                    }
                },
               PossibilityGroup = CREATE_A_PORTAL
            };
        }
        return new ActionPossibility(portalAction).WithPossibilityGroup(CREATE_A_PORTAL);
    }
}