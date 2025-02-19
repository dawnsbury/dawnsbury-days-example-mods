using System.Collections.Generic;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Display;
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
                    self.AddQEffect(new QEffect("Devastating attacks (two dice)", "Your Strikes deals two damage dice instead of one.")
                    {
                        IncreaseItemDamageDieCount = (qfSelf, item) =>
                        {
                            if (item.WeaponProperties?.DamageDieCount == 1)
                            {
                                return true;
                            }

                            return false;
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
        foreach (var skill in Skills.RelevantSkills)
        {
            var feat = new Feat(ModManager.RegisterFeatName("Skill Potency: " + skill), null, "You gain a +1 item bonus to " + skill + ".", [skillPotencyTrait], null)
                .WithOnCreature((sheet, cr) =>
                {
                    cr.AddQEffect(new QEffect($"Skill potency ({skill.HumanizeTitleCase2()})", "You have a +1 item bonus to " + skill + ".")
                    {
                        BonusToSkills = (bonusToWhatSkill) =>
                        {
                            if (bonusToWhatSkill == skill)
                            {
                                return new Bonus(1, BonusType.Item, "Automatic Bonus Progression");
                            }

                            return null;
                        }
                    });
                });
            ModManager.AddFeat(feat);
        }

        ModManager.RegisterActionOnEachCharacterSheet(self =>
        {
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection", "Skill potency", 3, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection6", "Skill potency", 6, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection13", "Skill potency", 13, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection15", "Skill potency", 15, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection17", "Skill potency", 17, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection20", "Skill potency", 20, ft => ft.HasTrait(skillPotencyTrait)).WithIsOptional());
        });
    }
}