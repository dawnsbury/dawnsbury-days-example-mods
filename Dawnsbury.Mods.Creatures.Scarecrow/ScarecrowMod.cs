using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Creatures.Scarecrow;

public class ScarecrowMod
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        var rolledAgainstLeerThisTurn = ModManager.RegisterEnumMember<QEffectId>("RolledAgainstLeerThisTurn");
        var immuneToLeer = ModManager.RegisterEnumMember<QEffectId>("ImmuneToLeer");
        ModManager.RegisterNewCreature("Scarecrow", (encounter) =>
        {
            void AffectWithLeer(Creature scarecrow, Creature target)
            {
                if (target.IsImmuneTo(Trait.Mental)) return;
                var leerAction = CombatAction.CreateSimple(scarecrow, "Scarecrow's Leer");
                leerAction.Traits.Add(Trait.Fear);
                leerAction.Traits.Add(Trait.Mental);
                leerAction.Traits.Add(Trait.Emotion);
                leerAction.Traits.Add(Trait.Visual);
                var result = CommonSpellEffects.RollSavingThrow(target, leerAction, Defense.Will, _ => 18);
                switch (result)
                {
                    case CheckResult.CriticalSuccess:
                        target.AddQEffect(new QEffect() { Id = immuneToLeer });
                        break;
                    case CheckResult.Success:
                        target.AddQEffect(QEffect.Frightened(1));
                        break;
                    case CheckResult.Failure:
                        target.AddQEffect(QEffect.Frightened(2));
                        break;
                    case CheckResult.CriticalFailure:
                        target.AddQEffect(QEffect.Frightened(3));
                        break;
                }
                target.AddQEffect(new QEffect()
                {
                    ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn,
                    Id = rolledAgainstLeerThisTurn
                });
            }
            
            var qfScarecrowLeer = new QEffect("Scarecrow's Leer", "Creatures within 40 feet of the scarecrow are affected by its aura of fear and must make a DC 18 Will save when they first enter the aura and at the beginning of each turn.")
            {
                StateCheckWithVisibleChanges = async (leer) =>
                {
                    foreach (var creature in leer.Owner.Battle.AllCreatures.Where(cr => cr.DistanceTo(leer.Owner) <= 8))
                    {
                        if (!creature.HasEffect(rolledAgainstLeerThisTurn) && !creature.HasEffect(immuneToLeer))
                        {
                            AffectWithLeer(leer.Owner, creature);
                        }

                        if (!creature.HasEffect(immuneToLeer))
                        {
                            creature.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            {
                                Id = QEffectId.DirgeOfDoomFrightenedSustainer
                            });
                        }
                    }
                }
            };
            
            return new Creature(new ModdedIllustration("Scarecrow.png"),
                    "Scarecrow",
                    new List<Trait> { Trait.Construct, Trait.Evil },
                    4, 11, 4,
                    new Defenses(19, 13, 8, 11),
                    60,
                    new Abilities(5, 2, 3, -4, 3, -2),
                    new Skills(athletics: 12))
                .AddQEffect(QEffect.DamageWeakness(DamageKind.Fire, 5))
                .AddQEffect(QEffect.DamageResistance(DamageKind.Bludgeoning, 5))
                .AddQEffect(QEffect.DamageResistance(DamageKind.Piercing, 5))
                .AddQEffect(QEffect.TraitImmunity(Trait.Mental))
                .AddQEffect(QEffect.TraitImmunity(Trait.Necromancy))
                .AddQEffect(QEffect.TraitImmunity(Trait.Nonlethal))
                .AddQEffect(QEffect.DamageImmunity(DamageKind.Bleed))
                .AddQEffect(QEffect.DamageImmunity(DamageKind.Mental))
                .AddQEffect(QEffect.DamageImmunity(DamageKind.Poison))
                .AddQEffect(QEffect.ImmunityToCondition(QEffectId.Sickened))
                .WithProficiency(Trait.Weapon, Proficiency.Trained)
                .WithUnarmedStrike(CommonItems.CreateNaturalWeapon(IllustrationName.DragonClaws, "claw of fear", "2d6", DamageKind.Bludgeoning))
                .AddQEffect(new QEffect("Baleful Glow", "On the first round of combat, creatures that haven't acted yet are flat-footed to you.", ExpirationCondition.Never, null, IllustrationName.None)
                {
                    Innate = true,
                    StateCheck = (qfBalefulGlow) =>
                    {
                        if (qfBalefulGlow.Owner.Battle.RoundNumber == 1)
                        {
                            foreach (var creatureWhoHasNotActed in qfBalefulGlow.Owner.Battle.AllCreatures.Where(unactor => !unactor.Actions.ActedThisEncounter))
                            {
                                creatureWhoHasNotActed.AddQEffect(new QEffect("Baleful Glow", QEffect.NoDescription, ExpirationCondition.Ephemeral, qfBalefulGlow.Owner, IllustrationName.None)
                                {
                                    IsFlatFootedTo = (qfOther, attacker, combatAction) => attacker == qfBalefulGlow.Owner ? "Baleful Glow" : null
                                });
                            }
                        }
                    }
                })
                .AddQEffect(new QEffect("Clawing Fear", "The scarecrow's Strike deal an additional 1d6 mental damage to frightened creatures.")
                {
                    AddExtraStrikeDamage = (action, defender) =>
                    {
                        if (defender.HasEffect(QEffectId.Frightened))
                        {
                            return (DiceFormula.FromText("1d6", "Clawing Fear"), DamageKind.Mental);
                        }

                        return null;
                    }
                })
                .AddQEffect(qfScarecrowLeer);
        });
    }
}