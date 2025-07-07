using System;
using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;
using Dawnsbury.Core;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;

namespace Dawnsbury.Mods.Ancestries.Kobold;

public static class KoboldAncestryLoader
{
    public static Trait KoboldTrait;
    public static FeatName KoboldBreathFeat = ModManager.RegisterFeatName("Kobold Breath");
    public static FeatName DragonBreathFeat = ModManager.RegisterFeatName("KoboldDragon'sBreath", "Dragon's Breath");

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
#if V3
        ModManager.AssertV3();
#else
        ModManager.AssertV2();
#endif
        
        KoboldTrait = ModManager.RegisterTrait(
            "Kobold",
            new TraitProperties("Kobold", true)
            {
                IsAncestryTrait = true
            });
        AddFeats(CreateDraconicExemplars());
        AddFeats(CreateKoboldAncestryFeats());

        ModManager.AddFeat(new AncestrySelectionFeat(
                ModManager.RegisterFeatName("ModKobold", "Kobold"), // We can't use the name "Kobold" because that's already that name of our trait, and the feat technical name and the trait technical name would conflict.
                "Every kobold knows that their slight frame belies true, mighty draconic power. They are ingenious crafters and devoted allies within their warrens, but those who trespass into their territory find them to be inspired skirmishers, especially when they have the backing of a draconic sorcerer or true dragon overlord. However, these reptilian opportunists prove happy to cooperate with other humanoids when it's to their benefit, combining caution and cunning to make their fortunes in the wider world.",
                [Trait.Humanoid, KoboldTrait],
                6,
                5,
                [
                    new EnforcedAbilityBoost(Ability.Dexterity),
                    new EnforcedAbilityBoost(Ability.Charisma),
                    new FreeAbilityBoost()
                ],
                CreateKoboldHeritages().ToList())
            .WithAbilityFlaw(Ability.Constitution)
            .WithOnSheet(sheet =>
            {
                sheet.AddSelectionOption(new SingleFeatSelectionOption("DraconicExemplar", "Draconic exemplar", 1,
                    (ft) => ft.Name.StartsWith("Draconic exemplar: ")));
            })
        );
    }

    private static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (var feat in feats)
        {
            ModManager.AddFeat(feat);
        }
    }

    private static IEnumerable<Feat> CreateKoboldAncestryFeats()
    {
        yield return new KoboldAncestryFeat(
                "Draconic Sycophant",
                "You have an affect that dragonkind find unusually pleasing—and when that fails, you know when to duck.",
                "You gain a +2 circumstance bonus to saving throws against dragons.", 1)
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect("Draconic Sycophant", "You have +2 to saves against dragons.")
                {
                    BonusToDefenses = (qfSelf, incomingAttack, targetedDefense) =>
                    {
                        if (targetedDefense == Defense.Fortitude || targetedDefense == Defense.Reflex || targetedDefense == Defense.Will)
                        {
                            if (incomingAttack?.Owner.HasTrait(Trait.Dragon) ?? false)
                            {
                                return new Bonus(2, BonusType.Circumstance, "Draconic Sycophant");
                            }
                        }

                        return null;
                    }
                });
            });
        yield return new KoboldAncestryFeat("Dragon's Presence",
                "As a member of dragonkind, you project unflappable confidence.",
                "When you attempt to Demoralize a foe of your level or lower, you gain a +1 circumstance bonus to the Intimidation check.", 1)
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect("Dragon's Presence", "You have a +1 circumstance bonus to Demoralize.")
                {
                    BonusToAttackRolls = (qfSelf, combatAction, defender) =>
                    {
                        if (combatAction.ActionId == ActionId.Demoralize) return new Bonus(1, BonusType.Circumstance, "Dragon's Presence");
                        return null;
                    }
                });
            });
        yield return new KoboldAncestryFeat("Kobold Weapon Familiarity",
                "You've trained with weapons ideal for subterranean efficiency.",
                "You are trained with the crossbow, light pick, pick, and spear. For the purpose of determining your proficiency, martial kobold weapons are simple weapons, and advanced kobold weapons are martial weapons.",
                1)
            .WithOnSheet(values =>
            {
                values.SetProficiency(Trait.SimpleCrossbow, Proficiency.Trained);
                values.SetProficiency(Trait.Pick, Proficiency.Trained);
                values.SetProficiency(Trait.LightPick, Proficiency.Trained);
                values.SetProficiency(Trait.Spear, Proficiency.Trained);
                values.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(Trait.Kobold) && traits.Contains(Trait.Martial), Trait.Simple);
                values.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(Trait.Kobold) && traits.Contains(Trait.Advanced), Trait.Martial);
            });
        yield return new TrueFeat(KoboldBreathFeat, 1,
                "You channel your draconic exemplar's power into a gout of energy.",
                "You gain a breath weapon attack that manifests as a 30-foot line or a 15-foot cone, dealing 1d4 damage. Each creature in the area must attempt a basic Reflex saving throw against the higher of your class DC. You can't use this ability again for 1d4 rounds.\n\nAt 3rd level and every 2 levels thereafter, the damage increases by 1d4. The shape of the breath and the damage type match those of your draconic exemplar.",
                [KoboldTrait], null)
            .WithActionCost(2)
            .WithOnCreature((sheet, creature) =>
            {
                var exemplarFeat = sheet.AllFeats.FirstOrDefault(ft => ft.Name.StartsWith("Draconic exemplar:"));
                if (exemplarFeat != null)
                {
                    var draconicExemplar = DraconicExemplarDescription.DraconicExemplarDescriptions[exemplarFeat.Name];
                    creature.AddQEffect(new QEffect("Kobold breath", "You have a breath weapon.")
                    {
                        ProvideMainAction = (qfSelf) =>
                        {
                            var kobold = qfSelf.Owner;
                            if (kobold.QEffects.Any(qf => qf.Key == "Dragon Breath")) return null;
                            int dc = kobold.ClassOrSpellDC();

                            if (kobold.HasFeat(DragonBreathFeat))
                            {
                                var menu = new SubmenuPossibility(IllustrationName.BreathWeapon, "Breath Weapon");
                                menu.PossibilityGroup = Constants.POSSIBILITY_GROUP_ADDITIONAL_NATURAL_STRIKE;
                                var section = new PossibilitySection("Breath Weapon");
                                menu.Subsections.Add(section);
                                section.AddPossibility(KoboldBreath("Kobold breath"));
                                section.AddPossibility(KoboldBreath("Dragon breath", true));

                                return menu;
                            }

                            return KoboldBreath("Breath weapon").WithPossibilityGroup(Constants.POSSIBILITY_GROUP_ADDITIONAL_NATURAL_STRIKE);

                            ActionPossibility KoboldBreath(string name, bool dragonBreath=false)
                            {
                                return new ActionPossibility(new CombatAction(kobold, IllustrationName.BreathWeapon, name, [Trait.Basic],
                                        "{b}Area{/b} " + (draconicExemplar.IsCone ? $"{(dragonBreath ? 30 : 15)}-foot cone" : $"{(dragonBreath ? 60 : 30)}-foot line") + "\n{b}Saving throw{/b} basic Reflex\n\nDeal " +
                                        S.HeightenedVariable((kobold.Level + 1) / 2, 1) + "d4 " + draconicExemplar.DamageKind.HumanizeTitleCase2().ToLower() +
                                        " damage (basic DC " + dc + " Reflex save mitigates).\n\n" + (dragonBreath ? "Then you can't use Breath weapon again for the rest of the encounter." : "Then you can't use Breath weapon again for 1d4 rounds."),
                                        draconicExemplar.IsCone ? Target.Cone(dragonBreath ? 6 : 3) : Target.Line(dragonBreath ? 12 : 6))
                                    .WithActionCost(2)
                                    .WithProjectileCone(IllustrationName.BreathWeapon, 15, ProjectileKind.Cone)
                                    .WithSoundEffect(SfxName.FireRay)
                                    .WithSavingThrow(new SavingThrow(draconicExemplar.SavingThrow, dc))
                                    .WithEffectOnEachTarget(async (spell, caster, target, result) => { await CommonSpellEffects.DealBasicDamage(spell, caster, target, result, (caster.Level + 1) / 2 + (dragonBreath ? "d8" : "d4"), draconicExemplar.DamageKind); })
                                    .WithEffectOnChosenTargets(async (spell, caster, targets) =>
                                    {
                                        if (dragonBreath)
                                            caster.AddQEffect(new QEffect() { Key = "Dragon Breath" });
                                        else
                                            caster.AddQEffect(new QEffect("Recharging Breath weapon", "This creature can't use Breath weapon until the value counts down to zero.", ExpirationCondition.CountsDownAtStartOfSourcesTurn, caster, IllustrationName.Recharging)
                                            {
                                                PreventTakingAction = (ca) => ca.Name == "Kobold breath" || ca.Name == "Breath weapon" || ca.Name == "Dragon breath" ? "This ability is recharging." : null,
                                                Value = R.Next(2, 5),
                                            });
                                    })
                                );
                            }
                        }
                    });
                }
            });
#if V3
        yield return new KoboldAncestryFeat("Winglets", "You're among the few kobolds who grow a set of draconic wings later in life. The wings are initially small and weak; while not enough for full flight, a strong flap allows you to jump further.",
                "You gain Powerful Leap as an extra feat. {i}(You can jump 5 feet farther with the Leap action.){/i}", 5)
            .WithOnSheet(values => values.GrantFeat(FeatName.PowerfulLeap));
        yield return new KoboldAncestryFeat("Kobold Weapon Innovator",
                "You've learned devious tactics with your kobold weapons.",
                "Whenever you critically hit with a crossbow, light pick, pick or spear, you trigger the weapon's {tooltip:criteffect}critical specialization effect.{/}", 5)
            .WithPermanentQEffect("Your crossbows, picks and spears trigger {tooltip:criteffect}critical specialization effects.{/}", qf =>
            {
                qf.YouHaveCriticalSpecialization = (effect, item, action, defender) 
                    => action.HasTrait(Trait.Crossbow) || action.HasTrait(Trait.Pick) || action.HasTrait(Trait.LightPick) || action.HasTrait(Trait.Spear);
            });
        yield return new KoboldAncestryFeat("Ally's Shelter",
                "In stressful circumstances, you find strength in your allies' example.",
                "When you're about to make a saving throw while adjacent to an ally, you may spend {icon:Reaction}a reaction. If you do, roll the save using your ally's base saving throw bonus instead of your own.", 5)
            .WithActionCost(Constants.ACTION_COST_REACTION)
            .WithPermanentQEffect("When you're about to make a saving throw while adjacent to an ally, as a reaction, you can roll the save using your ally's saving throw bonus instead of your own.", qf =>
            {
                qf.BeforeYourSavingThrow = async (effect, action, you) =>
                {
                    if (action.SavingThrow == null) return;
                    var defense = action.SavingThrow.Defense;
                    int bestBonus = 0;
                    Creature? bestAlly = null;
                    int yourBaseBonus = you.Defenses.GetBaseValue(defense);
                    foreach (var ally in you.Occupies.Neighbours.Creatures.Where(cr => cr.FriendOf(you) && cr.Alive))
                    {
                        int allyBaseBonus = ally.Defenses.GetBaseValue(defense);
                        int thisAllyOverbonus = allyBaseBonus - yourBaseBonus;
                        if (thisAllyOverbonus > bestBonus)
                        {
                            bestBonus = thisAllyOverbonus;
                            bestAlly = ally;
                        }
                    }

                    if (bestBonus > 0)
                    {
                        if (await you.AskToUseReaction("You're about to roll a save against " + action + ". Use Ally's Shelter to use " + bestAlly + "'s base saving throw bonus instead of yours, for an effective " + bestBonus.WithPlus() + " bonus?"))
                        {
                            you.AddQEffect(new QEffect(ExpirationCondition.EphemeralAtEndOfImmediateAction)
                            {
                                BonusToDefenses = (qEffect, combatAction, def) => def == defense && combatAction == action ? new Bonus(bestBonus, BonusType.Untyped, "Ally's Shelter") : null
                            });
                        }
                    }
                };
            });

        // Level 9
        yield return new KoboldAncestryFeat("Between the Scales",
            "Underestimating you is a grave mistake, but it's one others keep making.",
            "When you Strike a flat-footed creature using a melee weapon or unarmed attack that has the agile and finesse traits, it gains the backstabber trait.", 9)
        .WithPermanentQEffect("When you Strike a flat-footed creature using a melee weapon or unarmed attack that has the agile and finesse traits, it gains the backstabber trait.", qf =>
        {
            qf.BeforeYourActiveRoll = async (self, strike, target) =>
            {
                if (!(strike != null && strike.Item != null && !strike.HasTrait(Trait.Backstabber) && strike.HasTrait(Trait.Strike) && strike.HasTrait(Trait.Finesse) && strike.HasTrait(Trait.Agile) && target.IsFlatFootedTo(self.Owner, strike))) return;

                strike.Item.Traits.Add(Trait.Backstabber);

                self.Owner.AddQEffect(new QEffect()
                {
                    ExpiresAt = ExpirationCondition.EphemeralAtEndOfImmediateAction,
                    WhenExpires = self => strike.Item.Traits.Remove(Trait.Backstabber),
                });
            };
        });

        yield return new TrueFeat(DragonBreathFeat, 9,
            "You can put more effort into your Kobold Breath to channel greater draconic power, though it takes more out of you.",
            "When you use Kobold Breath, you can increase the damage dice to d8s and increase the area to 60 feet for a line breath weapon or 30 feet for a cone. If you do, you can't use your Breath weapon again for the rest of the encounter.", [KoboldTrait], null)
        .WithPermanentQEffect("Once per encounter you can boost the power of your Breath weapon, at the cost of losing the ability to use it again for the rest of the encounter.", qf => { })
        .WithPrerequisite(KoboldBreathFeat, "Kobold breath");

        yield return new KoboldAncestryFeat("Arcane Caster",
            "Your inborn arcane power begins to manifest.",
            "Choose one 1st-level spell and one 2nd-level spell from the arcane spell list. You can cast each of these as an arcane innate spells once per day.", 9)
        .WithOnSheet(values =>
        {
            values.SetProficiency(Trait.Spell, Proficiency.Trained);
            values.InnateSpells.GetOrCreate(KoboldTrait, () => new InnateSpells(Trait.Arcane));
            values.AddSelectionOptionRightNow(new AddInnateSpellOption("KoboldArcaneCaster2ndLevelSpell", "Level 2 Arcane Spell", -1, KoboldTrait, 2, spell => spell.HasTrait(Trait.Arcane) && !spell.HasTrait(Trait.Cantrip)));
            values.AddSelectionOptionRightNow(new AddInnateSpellOption("KoboldArcaneCaster1stLevelSpell", "Level 1 Arcane Spell", -1, KoboldTrait, 1, spell => spell.HasTrait(Trait.Arcane) && !spell.HasTrait(Trait.Cantrip)));
        });
#endif
    }

    private static IEnumerable<Feat> CreateDraconicExemplars()
    {
        yield return CreateAndRegisterDraconicExemplar(
            "You venerate, serve or descend from a wild fire dragon.",
            "For the purposes of some heritages and feats, you draconic exemplar is a {b}fire dragon{/b} whose breath weapon deals fire damage in a cone.",
            "Fire dragon",
            new DraconicExemplarDescription(DamageKind.Fire, true, Defense.Reflex));
        yield return CreateAndRegisterDraconicExemplar(
            "You venerate, serve or descend from a stoic cold dragon.",
            "For the purposes of some heritages and feats, you draconic exemplar is a {b}cold dragon{/b} whose breath weapon deals cold damage in a cone.",
            "Cold dragon",
            new DraconicExemplarDescription(DamageKind.Cold, true, Defense.Reflex));
        yield return CreateAndRegisterDraconicExemplar(
            "You venerate, serve or descend from an enigmatic electricity dragon.",
            "For the purposes of some heritages and feats, you draconic exemplar is an {b}electricity dragon{/b} whose breath weapon deals electricity damage in a line.",
            "Electricity dragon",
            new DraconicExemplarDescription(DamageKind.Electricity, false, Defense.Reflex));
        yield return CreateAndRegisterDraconicExemplar(
            "You venerate, serve or descend from a southern sonic dragon.",
            "For the purposes of some heritages and feats, you draconic exemplar is a {b}sonic dragon{/b} whose breath weapon deals sonic damage in a cone (Fortitude mitigates).",
            "Sonic dragon",
            new DraconicExemplarDescription(DamageKind.Sonic, true, Defense.Fortitude));
        yield return CreateAndRegisterDraconicExemplar(
            "You venerate, serve or descend from an underwater acid dragon.",
            "For the purposes of some heritages and feats, you draconic exemplar is an {b}acid dragon{/b} whose breath weapon deals acid damage in a line.",
            "Acid dragon",
            new DraconicExemplarDescription(DamageKind.Acid, false, Defense.Reflex));
    }

    private static Feat CreateAndRegisterDraconicExemplar(string flavorText, string rulesText, string name,
        DraconicExemplarDescription draconicExemplarDescription)
    {
        var featName = "Draconic exemplar: " + name;
        DraconicExemplarDescription.DraconicExemplarDescriptions.Add(featName, draconicExemplarDescription);
        return new Feat(ModManager.RegisterFeatName(featName), flavorText, rulesText, [], null);
    }

    private static IEnumerable<Feat> CreateKoboldHeritages()
    {
        yield return new HeritageSelectionFeat(ModManager.RegisterFeatName("Unusual Kobold"),
                "You're not like most other kobolds and don't share their fragile builds.",
                "You have two free ability boosts instead of a kobold's normal ability boosts and flaw.")
            .WithOnSheet((sheet) =>
            {
                sheet.AbilityBoostsFabric.AbilityFlaw = null;
                sheet.AbilityBoostsFabric.AncestryBoosts =
                [
                    new FreeAbilityBoost(),
                    new FreeAbilityBoost()
                ];
            });
        yield return new HeritageSelectionFeat(ModManager.RegisterFeatName("Dragonscaled Kobold"),
                "Your scales are especially colorful, possessing some of the same resistance a dragon possesses.",
                "You gain resistance equal to half your level (rounded up) to the damage type associated with your draconic exemplar. Double this resistance against dragons' Breath Weapons.")
            .WithOnCreature((sheet, creature) =>
            {
                var exemplarFeat = sheet.AllFeats.FirstOrDefault(ft => ft.Name.StartsWith("Draconic exemplar:"));
                if (exemplarFeat != null)
                {
                    var draconicExemplar = DraconicExemplarDescription.DraconicExemplarDescriptions[exemplarFeat.Name];
                    var resistanceValue = (creature.Level + 1) / 2;
                    creature.AddQEffect(new QEffect("Dragonscaled",
                        "You have " + draconicExemplar.DamageKind.HumanizeTitleCase2().ToLower() + " resistance " +
                        resistanceValue + ".")
                    {
                        StateCheck = (qfSelf) =>
                        {
                            var kobold = qfSelf.Owner;
                            kobold.WeaknessAndResistance.AddResistance(draconicExemplar.DamageKind, resistanceValue);
                        },
                        YouAreTargeted = async (qfSelf, incomingAttack) =>
                        {
                            if (incomingAttack.Name.Contains("Breath Weapon") &&
                                incomingAttack.Owner.HasTrait(Trait.Dragon))
                            {
                                qfSelf.Owner.WeaknessAndResistance.AddResistance(draconicExemplar.DamageKind,
                                    resistanceValue * 2);
                            }
                        }
                    });
                }
            });
        yield return new HeritageSelectionFeat(ModManager.RegisterFeatName("Strongjaw Kobold"),
                "Your bloodline is noted for their powerful jaws and sharp teeth.",
                "You gain a jaws unarmed attack that deals 1d6 piercing damage. Your jaws have the finesse and unarmed traits.")
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect("Strongjaw", "You have a jaws attack.")
                {
                    AdditionalUnarmedStrike = new Item(IllustrationName.Jaws, "jaws",
                            new[] { Trait.Finesse, Trait.Unarmed, Trait.Melee, Trait.Weapon })
                        .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                });
            });
        yield return new HeritageSelectionFeat(ModManager.RegisterFeatName("Venomtail Kobold"),
                "A vestigial spur in your tail secretes one dose of deadly venom each day.",
                "You gain the Tail Toxin action which allows you to apply your tail's venom to a piercing or slashing weapon once per day. If your next Strike with that weapon before the end of your next turn hits and deals damage, you deal persistent poison damage equal to your level to the target.")
            .WithOnCreature(creature =>
            {
                if (creature.PersistentUsedUpResources.UsedUpActions.Contains("Tail Toxin")) return;
                creature.AddQEffect(new QEffect("Tail Toxin", "You can apply your tail's venom to your weapon.")
                {
                    ProvideMainAction = (qfSelf) =>
                    {
                        var kobold = qfSelf.Owner;
                        return new ActionPossibility(
                            new CombatAction(kobold,
                                    IllustrationName.AcidSplash,
                                    "Tail Toxin",
                                    [Trait.Kobold, Trait.Poison, Trait.Basic],
                                    "You apply your tail's venom to a piercing or slashing weapon. If your next Strike with that weapon before the end of your next turn hits and deals damage, you deal persistent poison damage equal to your level to the target.\n\nYou can only take this action once per day.",
                                    Target.Self()
                                        .WithAdditionalRestriction(self =>
                                        {
                                            if (!self.HeldItems.Any(item => item.WeaponProperties?.DamageKind == DamageKind.Piercing || item.WeaponProperties?.DamageKind == DamageKind.Slashing))
                                            {
                                                return "You must wield a piercing or slashing weapon.";
                                            }

                                            return null;
                                        })
                                )
                                .WithActionCost(1)
                                .WithEffectOnSelf(self =>
                                {
                                    // You can no longer use this this encounter:
                                    self.RemoveAllQEffects(qf => qf.Name == "Tail Toxin");
                                    
                                    // You can no longer use it until the end of the day:
                                    self.PersistentUsedUpResources.UsedUpActions.Add("Tail Toxin");

                                    // Set up the actual effect:
                                    self.AddQEffect(new QEffect("Poisoned weapon", "Your next Strike with a piercing or slashing weapon deals extra persistent poison damage.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, self, IllustrationName.AcidSplash)
                                    {
                                        CountsAsBeneficialToSource = true,
                                        AfterYouDealDamage = async (attacker, action, defender) =>
                                        {
                                            if (action.Item?.WeaponProperties?.DamageKind == DamageKind.Piercing || action.Item?.WeaponProperties?.DamageKind == DamageKind.Slashing)
                                            {
                                                defender.AddQEffect(QEffect.PersistentDamage(attacker.Level.ToString(), DamageKind.Poison));
                                            }
                                        }
                                    });
                                })
                        ).WithPossibilityGroup(Constants.POSSIBILITY_GROUP_ADDITIONAL_NATURAL_STRIKE);
                    }
                });
            });
    }
}