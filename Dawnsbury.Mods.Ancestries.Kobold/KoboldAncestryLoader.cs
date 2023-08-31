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

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Dawnsbury.Mods.Ancestries.Kobold;

public static class KoboldAncestryLoader
{
    public static Trait KoboldTrait;
    
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        KoboldTrait = ModManager.RegisterTrait(
            "Kobold",
            new TraitProperties("Kobold", true)
            {
                IsAncestryTrait = true
            });
        AddFeats(CreateDraconicExemplars());
        AddFeats(CreateKoboldAncestryFeats());

        ModManager.AddFeat(new AncestrySelectionFeat(
                FeatName.CustomFeat,
                "Every kobold knows that their slight frame belies true, mighty draconic power. They are ingenious crafters and devoted allies within their warrens, but those who trespass into their territory find them to be inspired skirmishers, especially when they have the backing of a draconic sorcerer or true dragon overlord. However, these reptilian opportunists prove happy to cooperate with other humanoids when it's to their benefit, combining caution and cunning to make their fortunes in the wider world.",
                new List<Trait> { Trait.Humanoid, KoboldTrait },
                6,
                5,
                new List<AbilityBoost>()
                {
                    new EnforcedAbilityBoost(Ability.Dexterity),
                    new EnforcedAbilityBoost(Ability.Charisma),
                    new FreeAbilityBoost()
                },
                CreateKoboldHeritages().ToList())
            .WithAbilityFlaw(Ability.Constitution)
            .WithCustomName("Kobold")
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
                "You gain a +2 circumstance bonus to saving throws against dragons.")
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
                "When you attempt to Demoralize a foe of your level or lower, you gain a +1 circumstance bonus to the Intimidation check.")
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
                "You are trained with the crossbow and spear. For the purpose of determining your proficiency, martial kobold weapons are simple weapons, and advanced kobold weapons are martial weapons.")
            .WithOnSheet(sheet =>
            {
                sheet.SetProficiency(Trait.Crossbow, Proficiency.Trained);
                sheet.SetProficiency(Trait.Spear, Proficiency.Trained);
                sheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(Trait.Kobold) && traits.Contains(Trait.Martial), Trait.Simple);
                sheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(Trait.Kobold) && traits.Contains(Trait.Advanced), Trait.Martial);
            });
        yield return new KoboldAncestryFeat("Kobold Breath",
                "You channel your draconic exemplar's power into a gout of energy.",
                "You gain a breath weapon attack that manifests as a 30-foot line or a 15-foot cone, dealing 1d4 damage. Each creature in the area must attempt a basic Reflex saving throw against the higher of your class DC. You can't use this ability again for 1d4 rounds.\n\nAt 3rd level, the damage increases by 1d4. The shape of the breath and the damage type match those of your draconic exemplar.")
            .WithActionCost(2)
            .WithOnCreature((sheet, creature) =>
            {
                var exemplarFeat = sheet.AllFeats.FirstOrDefault(ft => ft.Name.StartsWith("Draconic exemplar:"));
                if (exemplarFeat != null)
                {
                    var draconicExemplar = DraconicExemplarDescription.DraconicExemplarDescriptions[exemplarFeat.Name];
                    creature.AddQEffect(new QEffect("Breath Weapon", "You have a breath weapon.")
                    {
                        ProvideMainAction = (qfSelf) =>
                        {
                            var kobold = qfSelf.Owner;
                            return new ActionPossibility(new CombatAction(kobold, IllustrationName.BreathWeapon, "Breath Weapon", new Trait[0],
                                    "You manifest as a 30-foot line or a 15-foot cone, dealing 1d4 damage. Each creature in the area must attempt a basic Reflex saving throw against your class DC. You can't use this ability again for 1d4 rounds.\n\nAt 3rd level, the damage increases by 1d4. The shape of the breath and the damage type match those of your draconic exemplar.",
                                    draconicExemplar.IsCone ? Target.Cone(3) : Target.Line(6))
                                .WithActionCost(2)
                                .WithProjectileCone(IllustrationName.BreathWeapon, 15, ProjectileKind.Cone)
                                .WithSoundEffect(SfxName.FireRay)
                                .WithSavingThrow(new SavingThrow(draconicExemplar.SavingThrow, (breathOwner) => 13 + breathOwner.Level + GetBestAbility(breathOwner)))
                                .WithEffectOnEachTarget((async (spell, caster, target, result) => { await CommonSpellEffects.DealBasicDamage(spell, caster, target, result, (caster.Level + 1) / 2 + "d4", draconicExemplar.DamageKind); }))
                                .WithEffectOnChosenTargets((async (spell, caster, targets) =>
                                {
                                    caster.AddQEffect(QEffect.CannotUseForXRound("Breath Weapon", caster, R.Next(2, 5)));
                                }))
                            );
                        }
                    });
                }
            });
    }

    private static int GetBestAbility(Creature creature)
    {
        int bestAbility = 0;
        if (creature.Abilities.Strength > bestAbility) bestAbility = creature.Abilities.Strength;
        if (creature.Abilities.Dexterity > bestAbility) bestAbility = creature.Abilities.Dexterity;
        if (creature.Abilities.Constitution > bestAbility) bestAbility = creature.Abilities.Constitution;
        if (creature.Abilities.Intelligence > bestAbility) bestAbility = creature.Abilities.Intelligence;
        if (creature.Abilities.Wisdom > bestAbility) bestAbility = creature.Abilities.Wisdom;
        if (creature.Abilities.Charisma > bestAbility) bestAbility = creature.Abilities.Charisma;
        return bestAbility;
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
        return new Feat(FeatName.CustomFeat, flavorText, rulesText, new List<Trait>(), null)
            .WithCustomName(featName);
    }

    private static IEnumerable<Feat> CreateKoboldHeritages()
    {
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                "You're not like most other kobolds and don't share their fragile builds.",
                "You have two free ability boosts instead of a kobold's normal ability boosts and flaw.")
            .WithCustomName("Unusual Kobold")
            .WithOnSheet((sheet) =>
            {
                sheet.AbilityBoostsFabric.AbilityFlaw = null;
                sheet.AbilityBoostsFabric.AncestryBoosts =
                    new List<AbilityBoost>
                    {
                        new FreeAbilityBoost(),
                        new FreeAbilityBoost()
                    };
            });
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                "Your scales are especially colorful, possessing some of the same resistance a dragon possesses.",
                "You gain resistance equal to half your level (rounded up) to the damage type associated with your draconic exemplar. Double this resistance against dragons' Breath Weapons.")
            .WithCustomName("Dragonscaled Kobold")
            .WithOnCreature((sheet, creature) =>
            {
                var exemplarFeat = sheet.AllFeats.FirstOrDefault(ft => ft.Name.StartsWith("Draconic exemplar:"));
                if (exemplarFeat != null)
                {
                    var draconicExemplar = DraconicExemplarDescription.DraconicExemplarDescriptions[exemplarFeat.Name];
                    var resistanceValue = (creature.Level + 1) / 2;
                    creature.AddQEffect(new QEffect("Dragonscaled",
                        "You have " + draconicExemplar.DamageKind.ToString().ToLower() + " resistance " +
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
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                "Your bloodline is noted for their powerful jaws and sharp teeth.",
                "You gain a jaws unarmed attack that deals 1d6 piercing damage. Your jaws have the finesse and unarmed traits.")
            .WithCustomName("Strongjaw Kobold")
            .WithOnCreature(creature =>
            {
                creature.AddQEffect(new QEffect("Strongjaw", "You have a jaws attack.")
                {
                    AdditionalUnarmedStrike = new Item(IllustrationName.Jaws, "jaws",
                            new[] { Trait.Finesse, Trait.Unarmed, Trait.Melee, Trait.Weapon })
                        .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                });
            });
        yield return new HeritageSelectionFeat(FeatName.CustomFeat,
                "A vestigial spur in your tail secretes one dose of deadly venom each day.",
                "You gain the Tail Toxin action which allows you to apply your tail's venom to a piercing or slashing weapon once per day. If your next Strike with that weapon before the end of your next turn hits and deals damage, you deal persistent poison damage equal to your level to the target.")
            .WithCustomName("Venomtail Kobold")
            .WithOnCreature(creature =>
            {
                if (creature.PersistentUsedUpResources?.UsedOrcFerocity ?? false) return;
                creature.AddQEffect(new QEffect("Tail Toxin", "You can apply your tail's venom to your weapon.")
                {
                    ProvideMainAction = (qfSelf) =>
                    {
                        var kobold = qfSelf.Owner;
                        return new ActionPossibility(
                            new CombatAction(kobold,
                                    IllustrationName.AcidSplash,
                                    "Tail Toxin",
                                    new[] { Trait.Kobold, Trait.Poison },
                                    "You apply your tail's venom to a piercing or slashing weapon. If your next Strike with that weapon before the end of your next turn hits and deals damage, you deal persistent poison damage equal to your level to the target.\n\nYou can only take this action once per day.",
                                    Target.Self()
                                        .WithAdditionalRestriction(self =>
                                        {
                                            return self.HeldItems.Any(item => item.WeaponProperties?.DamageKind == DamageKind.Piercing
                                                                              || item.WeaponProperties?.DamageKind == DamageKind.Slashing);
                                        })
                                )
                                .WithActionCost(1)
                                .WithEffectOnSelf(self =>
                                {
                                    // You can no longer use this this encounter:
                                    self.RemoveAllQEffects(qf => qf.Name == "Tail Toxin");
                                    
                                    // You can no longer use it until the end of the day:
                                    // (This is a hack, as we're reusing the "UsedOrcFerocity" flag so potentially a Venomtail Kobold with Adopted Ancestry (Orc) and Orc Ferocity would be affected,
                                    // but I hope for the purposes of a simple mod it's fine.)
                                    if (self.PersistentUsedUpResources != null) self.PersistentUsedUpResources.UsedOrcFerocity = true;
                                    
                                    // Set up the actual effect:
                                    self.AddQEffect(new QEffect("Poisoned weapon", "Your next Strike with a piercing or slashing weapon deals extra persistent poison damage.", ExpirationCondition.ExpiresAtEndOfSourcesTurn, self, IllustrationName.AcidSplash)
                                    {
                                        AfterYouDealDamage = async (attacker, action, defender) =>
                                        {
                                            if (action.Item?.WeaponProperties?.DamageKind == DamageKind.Piercing || action.Item?.WeaponProperties?.DamageKind == DamageKind.Slashing)
                                            {
                                                defender.AddQEffect(QEffect.PersistentDamage(attacker.Level.ToString(), DamageKind.Poison));
                                            }
                                        }
                                    });
                                })
                        );
                    }
                });
            });
    }
}