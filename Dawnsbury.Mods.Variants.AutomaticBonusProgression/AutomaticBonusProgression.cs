using System;
using System.Collections.Generic;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Variants.AutomaticBonusProgression;

public class AutomaticBonusProgression
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModManager.RegisterActionOnEachCreature(self =>
        {
            if (self.PersistentCharacterSheet != null) // Applies only to player characters, not to monsters.
            {
                // Attack potency
                if (self.Level >= 2)
                {
                    int attackPotency = self.Level switch
                    {
                        <= 9 => 1,
                        <= 15 => 2,
                        _ => 3
                    };
                    self.AddQEffect(new QEffect($"Attack potency (+{attackPotency})", $"You have a +{attackPotency} item bonus to weapon attack rolls.")
                    {
                        BonusToAttackRolls = (qfSelf, combatAction, defender) =>
                        {
                            if (combatAction.HasTrait(Trait.Attack) && combatAction.Item != null && (combatAction.Item.HasTrait(Trait.Weapon) || combatAction.Item.HasTrait(Trait.Unarmed)))
                            {
                                return new Bonus(attackPotency, BonusType.Item, "Automatic Bonus Progression");
                            }

                            return null;
                        }
                    });
                }

                // Devastating attacks
                if (self.Level >= 4)
                {
                    int targetNumberOfDice = self.Level >= 19 ? 4 : self.Level >= 12 ? 3 : 2;
                    self.AddQEffect(new QEffect($"Devastating attacks ({S.EnglishNumber(targetNumberOfDice)} dice)", $"Your Strikes deal {S.EnglishNumber(targetNumberOfDice)} damage dice instead of one.")
                    {
                        StateCheck = sc =>
                        {
                            foreach (var weapon in sc.Owner.Weapons)
                            {
                                if (weapon.WeaponProperties == null) continue;
                                int originalDice = weapon.WeaponProperties.DamageDieCount;
                                if (targetNumberOfDice > originalDice)
                                {
                                    weapon.WeaponProperties.DamageDieCount = targetNumberOfDice;
                                    if (!weapon.HasTrait(Trait.Unarmed))
                                    {
                                        var originalExit = weapon.OnExitFromHand;
                                        weapon.OnExitFromHand += exitedItem =>
                                        {
                                            var passthrough = originalExit == null ? exitedItem : originalExit(exitedItem);
                                            passthrough.WeaponProperties?.DamageDieCount = originalDice;
                                            return passthrough;
                                        };
                                    }
                                }
                            }
                        }
                    });
                }

                // Defense potency
                if (self.Level >= 5)
                {
                    int defensePotency = self.Level switch
                    {
                        <= 10 => 1,
                        <= 17 => 2,
                        _ => 3
                    };
                    self.AddQEffect(new QEffect($"Defense potency (+{defensePotency})", $"All armor you wears count as if it had a +{defensePotency} armor potency bonus to AC.")
                    {
                        BonusToDefenses = (effect, action, defense) =>
                        {
                            if (defense == Defense.AC)
                            {
                                var itemBonus = effect.Owner.Armor.Item?.ArmorProperties?.ItemBonus ?? 0;
                                if (defensePotency > itemBonus)
                                {
                                    return new Bonus(defensePotency - itemBonus, BonusType.Untyped, "Automatic Bonus Progression");
                                }
                            }

                            return null;
                        }
                    });
                }

                if (self.Level >= 7)
                {
                    int perceptionPotency = self.Level switch
                    {
                        <= 12 => 1,
                        <= 18 => 2,
                        _ => 3
                    };
                    self.AddQEffect(new QEffect($"Perception potency (+{perceptionPotency})", $"You have a +{perceptionPotency} item bonus to Perception.")
                    {
                        BonusToPerception = _ => new Bonus(perceptionPotency, BonusType.Item, "Automatic Bonus Progression")
                    });
                }

                if (self.Level >= 8)
                {
                    int savingThrowPotency = self.Level switch
                    {
                        <= 13 => 1,
                        <= 19 => 2,
                        _ => 3
                    };
                    self.AddQEffect(new QEffect($"Saving throw potency (+{savingThrowPotency})", $"You have a +{savingThrowPotency} item bonus to all saving throws.")
                    {
                        BonusToDefenses = (_, _, defense) => defense.IsSavingThrow() ? new Bonus(savingThrowPotency, BonusType.Item, "Automatic Bonus Progression") : null
                    });
                }
            }
        });

        // Skill potency
        var skillPotencyTrait = ModManager.RegisterTrait(
            "Skill Potency",
            new TraitProperties("Skill Potency", false) // explicitly set to 'false' so that the trait isn't displayed in the interface
        );
        var skillPotencyImprovementTrait = ModManager.RegisterTrait("SkillPotencyImprovementPlusTwo", new TraitProperties("Skill Potency Improvement +2", false));
        var skillPotencyImprovementPlusThreeTrait = ModManager.RegisterTrait("SkillPotencyImprovementPlusThree", new TraitProperties("Skill Potency Improvement +3", false));

        Dictionary<Skill, FeatName> initialFeats = new Dictionary<Skill, FeatName>();
        Dictionary<Skill, FeatName> plusTwoFeats = new Dictionary<Skill, FeatName>();
        
        for (int itemBonus = 1; itemBonus <= 3; itemBonus++)
        {
            int capturedItemBonus = itemBonus;
            var trait = capturedItemBonus switch
            {
                1 => skillPotencyTrait,
                2 => skillPotencyImprovementTrait,
                3 => skillPotencyImprovementPlusThreeTrait,
                _ => throw new Exception("This can't happen")
            };

            foreach (var skill in Skills.AllSkills)
            {
                var feat = new Feat(ModManager.RegisterFeatName(
                        trait.ToStringOrTechnical() + ": " + skill.ToStringOrTechnical(), $"Skill Potency (+{capturedItemBonus}): {skill.HumanizeTitleCase2()}"),
                        null, $"You gain a +{capturedItemBonus} item bonus to " + skill + ".", [trait], null)
                    .WithOnCreature((sheet, cr) =>
                    {
                        cr.AddQEffect(new QEffect()
                        {
                            BonusToSkills = (bonusToWhatSkill) =>
                            {
                                if (bonusToWhatSkill == skill)
                                {
                                    return new Bonus(capturedItemBonus, BonusType.Item, "Automatic Bonus Progression");
                                }

                                return null;
                            }
                        });
                    });

                switch (capturedItemBonus)
                {
                    case 1:
                        initialFeats.Add(skill, feat.FeatName);
                        break;
                    case 2:
                        plusTwoFeats.Add(skill, feat.FeatName);
                        feat.WithPrerequisite(values => values.HasFeat(initialFeats[skill]), "You must first increase this skill to +1 with Skill Potency.");
                        break;
                    case 3:
                        feat.WithPrerequisite(values => values.HasFeat(plusTwoFeats[skill]), "You must first increase this skill to +2 with Skill Potency.");
                        break;
                }
                ModManager.AddFeat(feat);
            }
        }

        ModManager.RegisterActionOnEachCharacterSheet(self =>
        {
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection", "Skill potency +1", 3, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
            
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection6", "Skill potency +1", 6, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
            
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencyImprovement9", "Skill potency +2", 9, ft => ft.HasTrait(skillPotencyImprovementTrait)).WithIsOptional());
            
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencyImprovement13", "Skill potency +2", 13, ft => ft.HasTrait(skillPotencyImprovementTrait)).WithIsOptional());
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection13", "Skill potency +1", 13, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
            
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencyImprovement15", "Skill potency +2", 15, ft => ft.HasTrait(skillPotencyImprovementTrait)).WithIsOptional());
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection15", "Skill potency +1", 15, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
            
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencyImprovement17-3", "Skill potency +3", 17, ft => ft.HasTrait(skillPotencyImprovementPlusThreeTrait)).WithIsOptional());
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection17", "Skill potency +1", 17, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
            
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencyImprovement20-3", "Skill potency +3", 20, ft => ft.HasTrait(skillPotencyImprovementPlusThreeTrait)).WithIsOptional());
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencyImprovement20", "Skill potency +2", 20, ft => ft.HasTrait(skillPotencyImprovementTrait)).WithIsOptional());
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection20", "Skill potency +1", 20, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
        });
        
        // Ability apex
        var abilityApexTrait = ModManager.RegisterTrait("AbilityApex", new TraitProperties("AbilityApex", false));
        foreach (var ability in Abilities.AllAbilities)
        {
            ModManager.AddFeat(new Feat(ModManager.RegisterFeatName($"AbilityApex{ability.ToStringOrTechnical()}", ability.HumanizeTitleCase2()), null,
                @$"Increase your {ability.HumanizeTitleCase2()} by +2, or to 18, whichever grants you the higher score.", [abilityApexTrait], null)
                .WithZOrder((int)ability)
                .WithOnSheet(values =>
                {
                    int intelligenceBefore = values.FinalAbilityScores.TotalModifier(Ability.Intelligence);
                    var currentAbility = values.FinalAbilityScores.TotalScore(ability);
                    values.FinalAbilityScores.AddViaAdditionalAbilityBoost(ability, (currentAbility >= 17) ? 2 : (18 - currentAbility));

                    int intelligenceAfter = values.FinalAbilityScores.TotalModifier(Ability.Intelligence);
                    int difference = intelligenceAfter - intelligenceBefore;
                    if (difference > 0)
                    {
                        for (int i = 0; i < difference; i++)
                        {
                            values.AddSelectionOptionRightNow(new SingleFeatSelectionOption($"ApexIntelligenceSkill{i}", "Skill from intelligence increase", -1, (ft) => ft is SkillSelectionFeat).WithIsOptional());
                        }

                        values.Alchemist?.RecalculateMaximumBatches(values);
                    }  
                })
            );
        }
        ModManager.RegisterActionOnEachCharacterSheet(self =>
        {
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("AbilityApex17", "Ability apex", 17, ft => ft.HasTrait(abilityApexTrait)).WithIsOptional());
        });
    }
}