using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
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
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

namespace Dawnsbury.Mods.Classes.Portalist;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
public static class PortalistClassLoader
{
    private const string CREATE_A_PORTAL = "Create a Portal";

    // private static string SUsedPoint = "UsedPortalPoint";
    // private static QEffectId QUsedPoint = ModManager.RegisterEnumMember<QEffectId>("UsedPortalistPoint");
    // private static QEffectId QAbundantPortalling = ModManager.RegisterEnumMember<QEffectId>("QAbundantPortalling");
    // private static QEffectId QAbundantPortalling2 = ModManager.RegisterEnumMember<QEffectId>("QAbundantPortalling2");
    private static Trait TPortalist = ModManager.RegisterTrait("Portalist", new TraitProperties("Portalist", true));
    
    private static QEffectId QAllyPortal = ModManager.RegisterEnumMember<QEffectId>("QAllyPortal");
    private static QEffectId QBoomerangPortal = ModManager.RegisterEnumMember<QEffectId>("QBoomerangPortal");
    private static QEffectId QAttackPortal = ModManager.RegisterEnumMember<QEffectId>("QAttackPortal");
    private static QEffectId QCoveringPortal = ModManager.RegisterEnumMember<QEffectId>("QCoveringPortal");
    private static QEffectId QDoubleHopPortal = ModManager.RegisterEnumMember<QEffectId>("QDoubleHopPortal");
    private static QEffectId QElementalBlastPortal = ModManager.RegisterEnumMember<QEffectId>("QElementalBlastPortal");
    private static QEffectId QShieldingPortal = ModManager.RegisterEnumMember<QEffectId>("QShieldingPortal");
    private static QEffectId QSummoningPortal = ModManager.RegisterEnumMember<QEffectId>("QSummoningPortal");

    private static ModdedIllustration IllPortal = new ModdedIllustration("PortalistAssets/CreatePortal.png");
    // Boomerang Portal [2-action][2 points]
    // Attack Portal [1-action][1 point] Get a +1 bonus to attack.
    // Covering Portal [1-action][2 points] Gain a +2 circumstance bonus to AC.
    // Double-Hop Portal [2-action][1 point] Teleport twice.
    // Elemental Blast Portal [2-action][2 points] 
    // Shielding Portal [1-action + reaction][1 point by reaction]
    // Summoning Portal [3-action + 2 points] as summon elemental
    // Transposition Portal [1-action + 1 point]
    
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
       AddFeats(CreateFeats());
    }

    private static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (var feat in feats)
        {
            ModManager.AddFeat(feat);
        }
    }

    private static IEnumerable<Feat> CreateFeats()
    {
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
                        return Wrap(CreateNormalPortal(qf, IllPortal, "Standard Portal", "Teleport to a square you can see within range.")
                            .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                            {
                                // caster.PersistentUsedUpResources.UsedUpActions.Add(SUsedPoint);
                                PortalistTeleport(caster, targets.ChosenTile!);
                            }));
                    },
                    // EndOfCombat = async (qf, w) =>
                    // {
                    //     if (qf.Owner.HasEffect(QUsedPoint))
                    //     {
                    //         qf.Owner.PersistentUsedUpResources.UsedUpActions.RemoveFirst(str => str == SUsedPoint);
                    //     }
                    // },
                    // StateCheck = qfPortalist =>
                    // {
                    //     int portalPointCount = GetPortalPoints(qfPortalist.Owner);
                    //     qfPortalist.Owner.AddQEffect(new QEffect("Portal points: " + portalPointCount, "", ExpirationCondition.Ephemeral, qfPortalist.Owner, null)
                    //     {
                    //         Innate = true
                    //     });
                    // },
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
        // var abundantPortalling = ModManager.RegisterFeatName("Abundant Portalling");
        // yield return new TrueFeat(abundantPortalling, 1, "You can reach into the interdimensional void more than most portalists.", "You gain an extra portal point each day.", [TPortalist])
        //     .WithPermanentQEffect(qf =>
        //     {
        //         qf.Id = QAbundantPortalling;
        //     });
        // yield return new TrueFeat(ModManager.RegisterFeatName("Greater Abundant Portalling"), 2, "You can reach into the the deepest pockets of the interdimensional void to summon additional portals.", "You gain an additional extra portal point each day.", [TPortalist])
        //     .WithPermanentQEffect(qf => qf.Id = QAbundantPortalling2)
        //     .WithPrerequisite(values => values.HasFeat(abundantPortalling), "You must have Abundant Portalling.");
        yield return new TrueFeat(ModManager.RegisterFeatName("Ally Portal"), 1, "You pull your friend alongside you.",
                "Choose a target square, then choose an adjacent ally.\n\nYou teleport as normal, then you pull your ally through the portal to an adjacent square of your choice.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(IllPortal)
            .WithPermanentQEffect("You can bring an ally with you through a portal.", qf =>
            {
                qf.Id = QAllyPortal;
                qf.ProvideMainAction = qff =>
                {
                    var action = CreateNormalPortal(qff, IllPortal, "Ally Portal", "Choose a target square, then choose an adjacent ally.\n\nYou teleport as normal, then you pull your ally through the portal to an adjacent square of your choice.");
                    ((TileTarget)action.Target).AdditionalTargetingRequirement = (creature, tile) => creature.Occupies.Neighbours.Creatures.Any(cr => cr.FriendOf(creature)) ? Usability.Usable : Usability.NotUsable("You don't have an adjacent ally.");
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
            .WithActionCost(2)
            .WithIllustration(IllustrationName.AerialBoomerang256)
            .WithPermanentQEffect("You can teleport, make a melee Strike, then teleport back.", qf =>
            {
                qf.Id = QBoomerangPortal;
                qf.ProvideMainAction = qff =>
                {
                    var action = CreateNormalPortal(qff, IllustrationName.AerialBoomerang256, "Boomerang Portal", "Teleport to a square within range, then make a melee Strike, then teleport back.")
                        .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                        {
                            var originalPlace = caster.Occupies;
                            // caster.PersistentUsedUpResources.Used/UpActions.Add(SUsedPoint);
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
                    var action = CreateNormalPortal(qff, new SideBySideIllustration(IllPortal, IllustrationName.Swords), "Attack Portal", "Teleport to a square within range and gain a +2 status bonus to your next attack this turn.")
                        .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                        {
                            // caster.PersistentUsedUpResources.UsedUpActions.Add(SUsedPoint);
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
                                    new ActionPossibility(new CombatAction(qff.Owner, IllustrationName.SteelShield, "Shield only", [TPortalist, Trait.Flourish],
                                        "You gain a +2 circumstance bonus to your AC until you move or until the beginning of your next turn.", Target.Self())
                                        .WithSoundEffect(SfxName.RaiseShield)
                                        .WithEffectOnEachTarget(async (spell, caster, target, result) => { AddShieldBonus(caster); })),
                                    new ActionPossibility(CreateNormalPortal(qff, new SideBySideIllustration(IllustrationName.SteelShield, IllPortal), "Teleport and shield", 
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
            .WithActionCost(2)
            .WithIllustration(new SideBySideIllustration(IllPortal, IllPortal))
            .WithPermanentQEffect("Teleport as normal. Then do it again.", qf =>
            {
                qf.Id = QDoubleHopPortal;
                qf.ProvideMainAction = qff =>
                {
                    return Wrap(CreateNormalPortal(qff, new SideBySideIllustration(IllPortal, IllPortal), "Double-Hop Portal", "Teleport as normal. Then do it again.")
                        .WithActionCost(2)
                        .WithEffectOnChosenTargets((async (spell, caster, targets) =>
                        {
                            PortalistTeleport(caster, targets.ChosenTile!);
                            var portal = CreateNormalPortal(qff, IllPortal, "Second Portal", "Teleport again.").WithActionCost(0).WithEffectOnChosenTargets((async (action, creature, chosenTargets) =>
                            {
                                PortalistTeleport(creature, chosenTargets.ChosenTile!);
                            }));
                            await caster.Battle.GameLoop.FullCast(portal);
                        })));
                }
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Elemental Blast Portal"), 2, "You cause the extradimensional matter of a portal to explode in an unstable vortex of elemental energy.",
                "Create a portal as normal, and deal 1d6 acid, electricity, fire, cold or sonic damage to each creature in the target square or adjacent to it (basic Reflex save against your class DC mitigates). Afterwards, you may choose to teleport there as normal.\n\nThe damage increases by 1d6 on level 3, and every two levels afterwards.", [TPortalist, Trait.Flourish])
            .WithActionCost(2)
            .WithIllustration(IllustrationName.EnergyEmanation)
            .WithPermanentQEffect("You can open a portal explosively to deal elemental damage.", qf =>
            {
                qf.Id = QElementalBlastPortal;
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Shielding Portal"), 2, "You prepare to summon a portal at a moment's notice that would shunt away incoming projectiles.",
                "Until your next turn, if you'd be the target of a ranged attack (including a ranged spell attack, but not a spell that requires a save), you can spend {icon:Reaction} a reaction. If you do, the attack automatically misses as the projectile is deflected into a portal.", [TPortalist, Trait.Flourish])
            .WithActionCost(1)
            .WithIllustration(IllustrationName.ForbiddingWard)
            .WithPermanentQEffect("You can prepare a reaction to deflect an incoming projectile.", qf =>
            {
                qf.Id = QShieldingPortal;
            });
        yield return new TrueFeat(ModManager.RegisterFeatName("Summoning Portal"), 4, "You don't cross your portal and instead use it to call in creatures of energy.",
                "You summon an elemental creature whose level is 1 or lower.\n\nImmediately when you open this portal and then once each turn when you Sustain the portal, you can take two actions as the summoned creature. If you don't Sustain the portal during a turn, the summoned creature will go away.", [TPortalist, Trait.Flourish])
            .WithActionCost(3)
            .WithIllustration(IllustrationName.SummonElemental)
            .WithPermanentQEffect("You can summon elementals as per the {i}summon elemental{/i} spell.", qf =>
            {
                qf.Id = QSummoningPortal;
            });
    }

    private static CombatAction CreateNormalPortal(QEffect qf, Illustration illustration, string name, string description)
    {   
        var portalist = qf.Owner;
        // var pointsLeft = GetPortalPoints(portalist);
        var range = portalist.Speed;
        var target = new TileTarget((caster, tile) =>
            //pointsLeft >= 1 &&
            tile.IsFree
            && caster.Occupies?.DistanceTo(tile) <= range 
            && caster.Occupies.HasLineOfEffectToIgnoreLesser(tile) < CoverKind.Blocked, null)
        {
            OverriddenTargetLine = "{b}Range{/b} " + (range*5) + " feet"
        };
        return new CombatAction(portalist, illustration, name, [TPortalist, Trait.Move, Trait.Flourish], description, target)
            .WithSoundEffect(SfxName.PhaseBolt);
    }

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

    private static Possibility Wrap(CombatAction portalAction)
    {
        //var pointsLeft = GetPortalPoints(portalAction.Owner);
        return new ActionPossibility(portalAction).WithPossibilityGroup(CREATE_A_PORTAL); //  [" + pointsLeft + " points]");
    }

    // private static int GetPortalPoints(Creature portalist)
    // {
    //     return portalist.Level 
    //         + portalist.Abilities.Intelligence
    //         + (portalist.HasEffect(QAbundantPortalling) ? 1 : 0)
    //         + (portalist.HasEffect(QAbundantPortalling2) ? 1 : 0)
    //         - portalist.PersistentUsedUpResources.UsedUpActions.Count(c => c == SUsedPoint);
    // }
}