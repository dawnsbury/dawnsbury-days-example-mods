using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Feats.Features;
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
using Dawnsbury.Core.Mechanics.Targeting.TargetingRequirements;
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

    private static Trait TPortalist = ModManager.RegisterTrait("Portalist", new TraitProperties("Portalist", true)
    {
        IsClassTrait = true
    });

    private static QEffectId QAllyPortal = ModManager.RegisterEnumMember<QEffectId>("QAllyPortal");
    private static QEffectId QBoomerangPortal = ModManager.RegisterEnumMember<QEffectId>("QBoomerangPortal");
    private static QEffectId QAttackPortal = ModManager.RegisterEnumMember<QEffectId>("QAttackPortal");
    private static QEffectId QCoveringPortal = ModManager.RegisterEnumMember<QEffectId>("QCoveringPortal");
    private static QEffectId QDoubleHopPortal = ModManager.RegisterEnumMember<QEffectId>("QDoubleHopPortal");
    private static QEffectId QElementalBlastPortal = ModManager.RegisterEnumMember<QEffectId>("QElementalBlastPortal");
    private static QEffectId QShieldingPortal = ModManager.RegisterEnumMember<QEffectId>("QShieldingPortal");
    private static QEffectId QSummoningPortal = ModManager.RegisterEnumMember<QEffectId>("QSummoningPortal");
    private static QEffectId QUsedUpHealingPortal = ModManager.RegisterEnumMember<QEffectId>("QUsedUpHealingPortal");
    private static QEffectId QSwervingPortal = ModManager.RegisterEnumMember<QEffectId>("QSwervingPortal");

    // We're adding one custom illustration, the rest of the pictures come from Dawnsbury Days core game so we can refer to them with IllustrationName:
    private static ModdedIllustration IllPortal = new ModdedIllustration("PortalistAssets/CreatePortal.png");

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
#if V3
        ModManager.AssertV3();
#else
        ModManager.AssertV2();
#endif
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
                @"{b}1. Standard portal.{/b} You can spend an action to teleport up to your Speed to a square you can see. This is a flourish action that doesn’t provoke attacks of opportunity.

{b}2. Quick.{/b} You gain a +1 status bonus to initiative rolls, if you’re wearing no armor or light armor only.

{b}3. Portalist feat.{/b} You choose and get a portalist feat. Portalist feats often allow you to create more powerful or specialized portals instead of your standard portal.", null)
            .WithClassFeatures(features => features
                .AddFeature(3, "Fast movement +10 feet", "You gain a +10-foot status bonus to your Speed while you’re wearing no armor or only light armor.")
                .AddFeature(5, "Expert strikes", "You gain expert proficiency in simple weapons as well as the rapier, shortsword, kukri, and unarmed attacks. These weapons also trigger {tooltip:criteffect}critical specialization effects{/}.")
                .AddFeature(5, "Ingenious movement", "You ignore difficult terrain while wearing light or no armor.")
                .AddFeature(5, WellKnownClassFeature.MasterInPerception)
                .AddFeature(7, "Fast movement +15 feet")
                .AddFeature(7, WellKnownClassFeature.WeaponSpecialization)
                .AddFeature(7, WellKnownClassFeature.Evasion)
                .AddFeature(9, WellKnownClassFeature.ExpertInClassDC))
            .WithOnSheet(sheet =>
            {
                sheet.AddSelectionOption(new SingleFeatSelectionOption("PortalistFeat1", "Portalist feat", 1, (ft) => ft.HasTrait(TPortalist)));
                sheet.AddAtLevel(5, values =>
                {
                    values.SetProficiency(Trait.Simple, Proficiency.Expert);
                    values.SetProficiency(Trait.Rapier, Proficiency.Expert);
                    values.SetProficiency(Trait.Shortsword, Proficiency.Expert);
                    values.SetProficiency(Trait.Kukri, Proficiency.Expert);
                    values.SetProficiency(Trait.Unarmed, Proficiency.Expert);
                    values.SetProficiency(Trait.Perception, Proficiency.Master);
                });
                sheet.AddAtLevel(7, values => { values.SetProficiency(Trait.Reflex, Proficiency.Master); });
                sheet.IncreaseProficiency(9, TPortalist, Proficiency.Expert);
            })
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect()
                {
                    ProvideMainAction = qf =>
                    {
                        return Wrap(CreateNormalPortal(qf.Owner, IllPortal, "Standard Portal", "Teleport to a square you can see within range.")
                            .WithEffectOnChosenTargets(async (spell, caster, targets) => { PortalistTeleport(caster, targets.ChosenTile!); }));
                    }
                });
                creature.AddQEffect(new QEffect("Quick", "You have +1 to initiative.")
                {
                    BonusToInitiative = (qf) => new Bonus(1, BonusType.Status, "Quick")
                });
                if (creature.Level >= 3)
                {
                    var speedBonus = creature.Level >= 7 ? 3 : 2;
                    creature.AddQEffect(new QEffect($"Fast movement +{speedBonus * 5} feet", $"You have +{speedBonus * 5} to Speed if you’re not wearing armor or are wearing only light armor.")
                    {
                        BonusToAllSpeeds = qf =>
                        {
                            if (!creature.Armor.WearsArmor || (creature.Armor.Item?.HasTrait(Trait.LightArmor) ?? false))
                            {
                                return new Bonus(speedBonus, BonusType.Status, "Fast movement");
                            }

                            return null;
                        }
                    });
                }

                if (creature.Level >= 5)
                {
                    creature.AddQEffect(new QEffect("Expert strikes", $"Your unarmed attacks, simple weapons, and the rapier, kukri and shortsword trigger {{tooltip:criteffect}}critical specialization effects{{/}}.")
                    {
                        YouHaveCriticalSpecialization = (effect, item, action, defender) => item.HasTrait(Trait.Unarmed)
                                                                                            || item.HasTrait(Trait.Shortsword)
                                                                                            || item.HasTrait(Trait.Rapier)
                                                                                            || item.HasTrait(Trait.Kukri)
                                                                                            || item.HasTrait(Trait.Simple)
                    });
                    creature.AddQEffect(new QEffect("Ingenious movement", "You ignore difficult terrain while wearing no armor or only light armor.")
                    {
                        StateCheck = sc =>
                        {
                            if (!creature.Armor.WearsArmor || (creature.Armor.Item?.HasTrait(Trait.LightArmor) ?? false))
                            {
                                sc.Owner.AddQEffect(new QEffect(ExpirationCondition.Ephemeral) { Id = QEffectId.IgnoresDifficultTerrain });
                            }
                        }
                    });
                }

                if (creature.Level >= 7)
                {
                    creature.AddQEffect(QEffect.Evasion());
                    creature.AddQEffect(QEffect.WeaponSpecialization());
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
                    ((TileTarget)action.Target).AdditionalTargetingRequirement = (creature, tile) => creature.Neighbours.Creatures.Any(cr => cr.FriendOf(creature)) ? Usability.Usable : Usability.NotUsable("You don’t have an adjacent ally.");
                    action.WithEffectOnChosenTargets((async (spell, caster, targets) =>
                    {
                        var ally = await caster.Battle.AskToChooseACreature(caster, caster.Neighbours.Creatures.Where(cr => cr.FriendOf(caster)), IllPortal, "Choose an ally to teleport alongside you.", "Teleport this ally.", "Cancel");
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
                                caster.Neighbours.Tiles
                                    .Where(tile => tile.IsTrulyGenuinelyFreeTo(ally))
                                    .Select(tile => new TileOption(tile, "Place this ally here.", async () => { chosenTarget = tile; }, AIConstants.NEVER, true))
                                    .Concat(new Option[] { new PassViaButtonOption("Do not teleport ally") })
                                    .ToList())
                            {
                                TopBarText = "Choose a square to teleport your ally to.",
                                TopBarIcon = IllPortal,
                                DisplacedCreature = ally
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
                            var originalPlace = caster.Space.TopLeftTile;
                            PortalistTeleport(caster, targets.ChosenTile!);
                            await CommonCombatActions.StrikeAdjacentCreature(caster, null);
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
                            var portal = CreateNormalPortal(caster, IllPortal, "Second Portal", "Teleport again.").WithActionCost(0).WithEffectOnChosenTargets((async (action, creature, chosenTargets) => { PortalistTeleport(creature, chosenTargets.ChosenTile!); }));
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
                        .WithVariants([
                            new SpellVariant("Acid", "Acid", IllustrationName.ResistAcid).WithAdditionalTrait(Trait.Acid),
                            new SpellVariant("Cold", "Cold", IllustrationName.ResistCold).WithAdditionalTrait(Trait.Cold),
                            new SpellVariant("Electricity", "Electricity", IllustrationName.ResistElectricity).WithAdditionalTrait(Trait.Electricity),
                            new SpellVariant("Fire", "Fire", IllustrationName.ResistFire).WithAdditionalTrait(Trait.Fire),
                            new SpellVariant("Sonic", "Sonic", IllustrationName.ResistSonic).WithAdditionalTrait(Trait.Sonic)
                        ])
                        .WithEffectOnChosenTargets((async (spell, caster, targets) =>
                        {
                            var damageKind = spell.ChosenVariant!.ToEnergyDamageKind();
                            var targetTile = targets.ChosenTile!;
                            await CommonAnimations.CreateConeAnimation(caster.Battle, targetTile.ToCenterVector(), new Tile[] { targetTile }.Concat(targetTile.Neighbours.Select(e => e.Tile)).ToList(), 20, ProjectileKind.Cone, IllustrationName.EnergyEmanation);
                            int dc = caster.ClassDC(TPortalist);
                            foreach (var target in targetTile.Neighbours.CreaturesPlusCreatureOnSelf)
                            {
                                var save = CommonSpellEffects.RollSavingThrow(target, spell, Defense.Reflex, dc);
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
        yield return new TrueFeat(ModManager.RegisterFeatName("Shielding Portal"), 2, "You prepare to summon a portal at a moment’s notice that would shunt away incoming projectiles.",
                "Until your next turn, if you’d be the target of a ranged attack (including a ranged spell attack, but not a spell that requires a save), you can spend {icon:Reaction} a reaction. If you do, the attack automatically misses as the projectile is deflected into a portal.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(IllustrationName.ForbiddingWard)
            .WithPermanentQEffect("You can prepare a reaction to deflect an incoming projectile.", qf =>
            {
                qf.Id = QShieldingPortal;
                qf.ProvideMainAction = qff =>
                {
                    return Wrap(new CombatAction(qff.Owner, IllustrationName.ForbiddingWard, "Shielding Portal", [TPortalist, Trait.Flourish, Trait.Basic], "Until your next turn, if you’d be the target of a ranged attack (including a ranged spell attack, but not a spell that requires a save), you can spend {icon:Reaction} a reaction. If you do, the attack automatically misses as the projectile is deflected into a portal.", Target.Self())
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            caster.AddQEffect(new QEffect("Shielding Portal", "Until your next turn, if you’d be the target of a ranged attack (including a ranged spell attack, but not a spell that requires a save), you can spend {icon:Reaction} a reaction. If you do, the attack automatically misses as the projectile is deflected into a portal.", ExpirationCondition.ExpiresAtStartOfYourTurn, caster, IllustrationName.ForbiddingWard)
                            {
                                FizzleIncomingActions = (async (effect, action, sb) =>
                                {
                                    if (action.HasTrait(Trait.Ranged) && action.HasTrait(Trait.Attack))
                                    {
                                        if (await effect.Owner.AskToUseReaction("You’re targeted by " + action.Name + ". Use Shielding Portal to deflect this?"))
                                        {
                                            effect.Owner.Overhead("deflected", Color.White, effect.Owner + " deflected the projectile with Shielding Portal.");
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
        yield return new TrueFeat(ModManager.RegisterFeatName("Summoning Portal"), 4, "You don’t cross your portal and instead use it to call in creatures of energy.",
                "You summon an elemental creature whose level is 1 or lower.\n\nImmediately when you open this portal and then once each turn when you Sustain the portal, you can take two actions as the summoned creature. If you don’t Sustain the portal during a turn, the summoned creature will go away.\n\nAt level 5, the maximum level of the summoned creatures is 2. At level 7, the maximum level is 3.", [TPortalist, Trait.Flourish])
            .WithActionCost(3)
            .WithIllustration(IllustrationName.SummonElemental)
            .WithPermanentQEffect("You can summon elementals as per the {i}summon elemental{/i} spell.", qf =>
            {
                qf.Id = QSummoningPortal;
                qf.ProvideMainAction = qff =>
                {
                    var elementalLevel = CommonSpellEffects.GetMaximumSummonLevel((qff.Owner.Level + 1)/2);
                    return Wrap(new CombatAction(qff.Owner, IllustrationName.SummonElemental, "Summoning Portal", [Trait.Conjuration, Trait.Arcane, Trait.Primal, TPortalist, Trait.Basic], $"You summon an elemental creature whose level is {elementalLevel} or lower.\n\nImmediately when you open this portal and then once each turn when you Sustain the portal, you can take two actions as the summoned creature. If you don’t Sustain the portal during a turn, the summoned creature will go away.", Target.RangedEmptyTileForSummoning(6))
                        .WithActionCost(3)
                        .WithSoundEffect(SfxName.Summoning)
                        .WithVariants(MonsterStatBlocks.MonsterExemplars.Where(animal => animal.HasTrait(Trait.Elemental) && animal.Level <= elementalLevel).Select(animal => new SpellVariant(animal.Name, animal.Name, animal.Illustration)).ToArray())
                        .WithCreateVariantDescription((_, variant) => RulesBlock.CreateCreatureDescription(MonsterStatBlocks.MonsterExemplarsByName[variant!.Id]))
                        .WithEffectOnChosenTargets((async (spell, caster, targets) => { await CommonSpellEffects.SummonMonster(spell, caster, targets.ChosenTile!); })));
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Healing Portal"), 2, "You direct your portal so that it passes through the Plane of Positive Energy.",
                "Teleport as normal, except you also heal 1d8 HP per two character levels, rounded up. You can only use Healing Portal once per encounter.", [TPortalist, Trait.Flourish, Trait.Positive, Trait.Healing])
            .WithActionCost(2)
            .WithIllustration(IllustrationName.Heal)
            .WithPermanentQEffect("Teleport as normal, except you also heal 1d8 HP per two character levels, rounded up. You can only use Healing Portal once per encounter.", qf =>
            {
                qf.ProvideMainAction = qff =>
                {
                    if (qff.Owner.HasEffect(QUsedUpHealingPortal)) return null;
                    return Wrap(CreateNormalPortal(qff.Owner, new SideBySideIllustration(IllPortal, IllustrationName.Heal), "Healing Portal", $"{{i}}You direct your portal so that it passes through the Plane of Positive Energy.{{/i}}\n\nTeleport as normal, except you also heal {S.HeightenedVariable((qff.Owner.Level+1)/2, 1)}d8 HP. You can only use Healing Portal once per encounter.")
                        .WithAdditionalTraits(Trait.Positive, Trait.Healing)
                        .WithPortalTargetChanges(tt => tt.WithAdditionalSelfRequirement(
                            (portalist) => portalist.Damage > 0
                                ? Usability.Usable
                                : Usability.NotUsable("You’re already at full HP.")))
                        .WithActionCost(2)
                        .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                        {
                            Sfxs.Play(SfxName.Healing);
                            caster.AddQEffect(new QEffect() { Id = QUsedUpHealingPortal });
                            await caster.HealAsync((caster.Level + 1) / 2 + "d8", spell);
                            PortalistTeleport(caster, targets.ChosenTile!);
                        }));
                };
            });
        
        yield return new TrueFeat(ModManager.RegisterFeatName("Stealth Portal"), 6, "You camouflage both your portal and yourself, emerging from your portal unseen.",
                "Teleport as normal, except you also become invisible as per the spell {i}invisibility.{/i}", [TPortalist, Trait.Flourish, Trait.Illusion])
            .WithActionCost(2)
            .WithIllustration(IllustrationName.Invisibility)
            .WithPermanentQEffect("Teleport as normal, except you also become invisible as per the spell {i}invisibility.{/i}", qf =>
            {
                qf.ProvideMainAction = qff =>
                {
                    return Wrap(CreateNormalPortal(qff.Owner, new SideBySideIllustration(IllPortal, IllustrationName.Invisibility), "Stealth Portal", "Teleport as normal, except you also become invisible as per the spell {i}invisibility{/i}.")
                        .WithAdditionalTraits(Trait.Illusion)
                        .WithActionCost(2)
                        .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                        {
                            Sfxs.Play(SfxName.InvisibilityPoor);
                            var invisibility = QEffect.Invisibility(false);
                            caster.AddQEffect(invisibility);
                            PortalistTeleport(caster, targets.ChosenTile!);
                        }));
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Transposition Portal"), 1,
                "You create a bidirectional portal and pull a creature on the other hand back through.",
                @"Choose an ally or an enemy within the range of your Speed.
• If it’s an ally, you swap positions.
• If it’s an enemy, it makes a Reflex save against your class DC. If it fails, you swap positions. If it succeeds, the enemy stays in place but you can choose to teleport adjacent to that enemy anyway.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(IllustrationName.Shove)
            .WithPermanentQEffect("Create a bidirectional portal and pull a creature on the other hand back through.", qf =>
            {
                qf.ProvideMainAction = qff =>
                {
                    return Wrap(new CombatAction(qff.Owner, new SideBySideIllustration(IllPortal, IllustrationName.Shove), "Transposition Portal", [TPortalist, Trait.Teleportation, Trait.Move, Trait.Conjuration, Trait.Flourish, Trait.Basic], @"{i}You create a bidirectional portal and pull a creature on the other hand back through.{/i}

Choose an ally or an enemy within the range of your Speed.
• If it’s an ally, you swap positions.
• If it’s an enemy, it makes a Reflex save against your class DC. If it fails, you swap positions. If it succeeds, the enemy stays in place but you can choose to teleport adjacent to that enemy anyway.",
                            new CreatureTarget(RangeKind.Ranged, [
                                new MaximumRangeCreatureTargetingRequirement(qff.Owner.Speed),
                                new LegacyCreatureTargetingRequirement((a,d)=> DoesPortalHaveLineOfEffectTo(a, d) ? Usability.Usable : Usability.NotUsableOnThisCreature("line-of-effect"))
                            ], null))
                        .WithSavingThrow(new SavingThrow(Defense.Reflex, cr => cr?.ClassDC(TPortalist) ?? 10))
                        .WithNoSaveFor((combatAction, target) => combatAction.Owner.FriendOf(target))
                        .WithActionCost(1)
                        .WithSoundEffect(SfxName.PhaseBolt)
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            if (caster.FriendOf(target) || result <= CheckResult.Failure)
                            {
                                if (caster.Space.Tiles.All(tile => tile.PrimaryOccupant == caster))
                                {
                                    var originalTargetLocation = target.Space.CenterTile;
                                    foreach (var originalSpaceTile in caster.Space.Tiles.ToList())
                                    {
                                        originalSpaceTile.PrimaryOccupant = null;
                                    }
                                    PortalistTeleport(target, caster.Space.CenterTile);
                                    PortalistTeleport(caster, originalTargetLocation);
                                }
                            }
                            else
                            {
                                if (await caster.AskForConfirmation(new SideBySideIllustration(IllPortal, IllustrationName.Shove), target + " saved against Transposition Portal and will stay in its place. Teleport adjacent to the target?", "Teleport", "Stay in place"))
                                {
                                    Sfxs.Play(SfxName.PhaseBolt);
                                    PortalistTeleport(caster, target.Space.CenterTile);
                                }
                            }
                        }));
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Rising Portal"), 2,
                "Standing up is for mundane folk. You can stand up by creating a portal underneath yourself and fall into an upright position.",
                @"Stand up as {icon:FreeAction}a free action. This doesn’t provoke attacks of opportunity.", [TPortalist, Trait.Flourish])
            .WithActionCost(0)
            .WithIllustration(IllustrationName.StandUp)
            .WithPermanentQEffect("Stand up as a free action. This doesn’t provoke attacks of opportunity.", qf =>
            {
                qf.ProvideContextualAction = qff =>
                {
                    if (!qff.Owner.HasEffect(QEffectId.Prone)) return null;
                    return Wrap(new CombatAction(qff.Owner, new SideBySideIllustration(IllPortal, IllustrationName.Shove), "Rising Portal", [TPortalist, Trait.Teleportation, Trait.Move, Trait.Conjuration, Trait.Flourish, Trait.DoesNotProvoke, Trait.Basic], @"{i}Standing up is for mundane folk. You can stand up by creating a portal underneath yourself and fall into an upright position.{/i}

Stand up as {icon:FreeAction}a free action. This doesn’t provoke attacks of opportunity.", Target.Self())
                        .WithActionCost(0)
                        .WithSoundEffect(SfxName.PhaseBolt)
                        .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                        {
                            caster.StandUp();
                        }));
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Swerving Portal"), 4,
                "You can visualize a complex teleportation process in your mind’s eye without directly seeing your destination.",
                @"You don’t need line-of-effect or line-of-sight to the destination of your portals, as long as you know what the destination looks like {i}(you can teleport behind cover or walls, but not into fog-of-war).{/i}
", [TPortalist, Trait.Flourish])
            .WithPermanentQEffect("You don’t need line-of-effect or line-of-sight to the destination of your portals.", qf =>
            {
                qf.Id = QSwervingPortal;
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Chained Portals"), 6,
                "You hold on to the spark of interplanar travel for a big longer before it’s gone.",
                "Once per day, you may open up to two portals on the same turn, as though they weren’t flourish actions. {i}(You do this by choosing ‘Chain another portal’ after opening the first portal.){/i}", [TPortalist])
            .WithPermanentQEffect("Once per day, you may open up to two portals on the same turn, as though they weren’t flourish actions.", qf =>
            {
                qf.ProvideContextualAction = qff =>
                {
                    var portalist = qff.Owner;
                    if (portalist.PersistentUsedUpResources.UsedUpActions.Contains("ChainedPortals")) return null;
                    if (portalist.Actions.ActionHistoryThisTurn.Any(action => action.HasTrait(TPortalist) && action.HasTrait(Trait.Flourish)))
                    {
                        return new ActionPossibility(new CombatAction(portalist, IllPortal, "Chain another portal", [TPortalist, Trait.Basic],
                                "{b}Frequency{/b} once per day\n\nSuppress the flourish trait of the portal you opened previously this turn so that you can open one more portal this turn.",
                                Target.Self())
                            .WithActionCost(0)
                            .WithSoundEffect(SfxName.MinorHealing)
                            .WithEffectOnEachTarget(async (spell, caster, target, result) =>
                            {
                                caster.Actions.ActionHistoryThisTurn.ForEach(action =>
                                {
                                    if (action.HasTrait(TPortalist))
                                    {
                                        action.Traits.Remove(Trait.Flourish);
                                    }
                                });
                                caster.PersistentUsedUpResources.UsedUpActions.Add("ChainedPortals");
                            })
                        ).WithPossibilityGroup(Constants.POSSIBILITY_GROUP_CONTEXTUAL_GET_RID_OF_DEBUFF);
                    }
                    return null;
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Relentless Portal"), 8, "If your first portal strike doesn’t hit, your second will. You will not leave your enemy be.",
                "Teleport as normal, then make a melee attack against an adjacent enemy. If you miss but don’t critically miss, teleport again into another square adjacent to the same enemy, and make another attack. If you do, your multiple attack penalty increases twice, but only after you make the second attack.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(new SideBySideIllustration(IllPortal, IllustrationName.Swords))
            .WithPermanentQEffect("Teleport as normal, then make a melee attack against an adjacent enemy. If you miss but don’t critically miss, teleport again into another square adjacent to the same enemy, and make another attack. If you do, your multiple attack penalty increases twice, but only after you make the second attack.", qf =>
            {
                qf.ProvideMainAction = qff =>
                {
                    return Wrap(CreateNormalPortal(qff.Owner, new SideBySideIllustration(IllPortal, IllustrationName.Swords), "Relentless Portal", "Teleport as normal, then make a melee attack against an adjacent enemy. If you miss but don’t critically miss, teleport again into another square adjacent to the same enemy, and make another attack. If you do, your multiple attack penalty increases twice, but only after you make the second attack.")
                        .WithAdditionalTraits(Trait.Illusion)
                        .WithActionCost(1)
                        .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                        {
                            PortalistTeleport(caster, targets.ChosenTile!);
                            var lastStrikeBefore = caster.Actions.ActionHistoryThisTurn.LastOrDefault(act => act.HasTrait(Trait.Strike));
                            await CommonCombatActions.StrikeAdjacentCreature(caster);
                            var lastStrike = caster.Actions.ActionHistoryThisTurn.LastOrDefault(act => act.HasTrait(Trait.Strike));
                            var strikeTarget = lastStrike?.ChosenTargets.ChosenCreature;
                            if (lastStrike != null && lastStrike != lastStrikeBefore && lastStrike.CheckResult == CheckResult.Failure && strikeTarget != null)
                            {
                                var legalTiles = strikeTarget.Neighbours.Tiles.Where(tile => tile.IsTrulyGenuinelyFreeTo(caster) && !caster.Space.Tiles.Contains(tile)).ToList();
                                if (legalTiles.Count > 0)
                                {
                                    var options = legalTiles.Select(tl => new TileOption(tl, "Make your second attack from this tile.", async () =>
                                    {
                                        Sfxs.Play(SfxName.PhaseBolt);
                                        PortalistTeleport(caster, tl);
                                        caster.Actions.AttackedThisManyTimesThisTurn--;
                                        await CommonCombatActions.StrikeAdjacentCreature(caster, cr => cr == strikeTarget);
                                        caster.Actions.AttackedThisManyTimesThisTurn++;
                                    }, AIConstants.NEVER, true)).Cast<Option>().Concat([new PassViaButtonOption("Don’t teleport again")]).ToList();
                                    await caster.Battle.GameLoop.OfferOptions(caster, options, true);
                                }
                            }
                        }));
                };
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Retaliatory Portal"), 8, "If your first portal strike doesn’t hit, your second will. You will not leave your enemy be.",
                "After you take damage from an enemy melee attack, you can teleport to a square adjacent to that enemy as {icon:Reaction}a reaction, and make a melee Strike against that enemy.", [TPortalist])
            .WithActionCost(Constants.ACTION_COST_REACTION)
            .WithPermanentQEffect("After you take damage from an enemy melee attack, you can teleport to a square adjacent to that enemy as a reaction, and make a melee Strike against that enemy.", qf =>
            {
                qf.AfterYouTakeDamage = async (qeffect, amount, kind, action, critical) =>
                {
                    var caster = qeffect.Owner;
                    if (action != null && action.HasTrait(Trait.Melee) && await caster.AskToUseReaction(action.Owner + " dealt damage to you. Use Retaliatory Portal to Strike the enemy back?"))
                    {
                        var enemy = action.Owner;
                        var legalTiles = enemy.Neighbours.Tiles.Where(tile => tile.IsTrulyGenuinelyFreeTo(caster) && !caster.Space.Tiles.Contains(tile)).ToList();
                        if (legalTiles.Count > 0)
                        {
                            var options = legalTiles
                                .Select(tl => new TileOption(tl, "Make your retaliatory attack from this tile.", async () =>
                            {
                                Sfxs.Play(SfxName.PhaseBolt);
                                PortalistTeleport(caster, tl);
                                await CommonCombatActions.StrikeAdjacentCreature(caster, cr => cr == enemy);
                            }, AIConstants.NEVER, true))
                                .Cast<Option>()
                                .ToList();
                            await caster.Battle.GameLoop.OfferOptions(caster, options, true);
                        }
                    }
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
            tile.IsFree && caster.DistanceTo(tile) <= range && DoesPortalHaveLineOfEffectTo(caster, tile), null)
        {
            DisplacesCasterIntoTarget = true,
            OverriddenTargetLine = "{b}Range{/b} " + (range * 5) + " feet" // TileTarget normally doesn't create target lines automatically, so we have to write one ourselves
        };
        List<Trait> traits = [TPortalist, Trait.Move, Trait.Flourish, Trait.Conjuration, Trait.Teleportation];
        if (name != "Standard Portal")
        {
            // The Basic trait prevents the action from being shown in the OFFENSE section of the rules block, which is what we want because the 
            // action is already describes under the ABILITIES section:
            traits.Add(Trait.Basic);
        }

        return new CombatAction(portalist, illustration, name, traits.ToArray(), description, target)
            .WithSoundEffect(SfxName.PhaseBolt);
    }

    private static bool DoesPortalHaveLineOfEffectTo(Creature caster, Creature targetCreature)
    {
        return caster.HasLineOfEffectToIgnoreLesser(targetCreature) < CoverKind.Blocked
               || (caster.HasEffect(QSwervingPortal) && targetCreature.Space.TopLeftTile.FogOfWar == FogOfWar.Clear);
    }
    private static bool DoesPortalHaveLineOfEffectTo(Creature caster, Tile tile)
    {
        return caster.HasLineOfEffectToIgnoreLesser(tile) < CoverKind.Blocked
               || (caster.HasEffect(QSwervingPortal) && tile.FogOfWar == FogOfWar.Clear);
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
        portalist.AnimationData.ColorBlink(Color.White);
        portalist.Battle.SmartCenterTileAlways(target);
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

    public static CombatAction WithPortalTargetChanges(this CombatAction combatAction, Action<TileTarget> changes)
    {
        changes((TileTarget)combatAction.Target);
        return combatAction;
    }

    public static CombatAction WithAdditionalTraits(this CombatAction combatAction, params Trait[] traits)
    {
        combatAction.Traits.AddRange(traits);
        return combatAction;
    }
}