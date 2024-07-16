using System.Collections.Generic;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
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
                    self.AddQEffect(new QEffect("Attack Potency (Lv.2)", "You have a +1 item bonus to attack rolls.")
                    {
                        BonusToAttackRolls = (qfSelf, combatAction, defender) =>
                        {
                            if (combatAction.Item != null && (combatAction.Item.HasTrait(Trait.Weapon) || combatAction.Item.HasTrait(Trait.Unarmed)))
                            {
                                return new Bonus(1, BonusType.Item, "Automatic Bonus Progression");
                            }

                            return null;
                        }
                    });
                }
                
                // Devastating attacks
                if (self.Level >= 4)
                {
                    self.AddQEffect(new QEffect("Devastating Attacks (Lv.4)", "Your Strikes deals two damage dice instead of one.")
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
            }
        });

        // Skill potency
        var skillPotencyTrait = ModManager.RegisterTrait(
            "Skill Potency", 
            new TraitProperties("Skill Potency", false) // explicitly set to 'false' so that the trait isn't displayed in the interface
            );
        foreach (var skill in Skills.RelevantSkills)
        {
            var feat = new Feat(ModManager.RegisterFeatName("Skill Potency: " + skill), null, "You gain a +1 item bonus to " + skill + ".", new List<Trait>() { skillPotencyTrait }, null)
                .WithOnCreature((sheet, cr) =>
                {
                    cr.AddQEffect(new QEffect("Skill Potency (Lv.3)", "You have a +1 item bonus to " + skill + ".")
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
            self.Calculated.AddSelectionOption(new SingleFeatSelectionOption("SkillPotencySelection", "Skill Potency", 3, (ft)=>ft.HasTrait(skillPotencyTrait))
                .WithIsOptional());
        });
    }
}