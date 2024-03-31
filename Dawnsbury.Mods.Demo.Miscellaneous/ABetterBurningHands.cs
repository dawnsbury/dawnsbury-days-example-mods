using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Demo.Miscellaneous;

/// <summary>
/// Updates an existing spell.
/// </summary>
public static class ABetterBurningHands
{
    public static void Apply()
    {
        ModManager.ReplaceExistingSpell(SpellId.BurningHands, 1, (spellcaster, level, inCombat, spellInformation) =>
        {
            // Code copy-pasted from normal Burning Hands code, except:
            // -> Area increased to 60-foot cone
            // -> Allies are excluded from the effect.
            
            return Spells.CreateModern(IllustrationName.BurningHands,
                    "Burning Hands",
                new[] { Trait.Evocation, Trait.Fire, Trait.Arcane, Trait.Primal },
                    "Gouts of flame rush from your hands IN A SIXTY-FOOT CONE!!!",
                    "Deal " + S.HeightenedVariable(2 * level, 2) + "d6 fire damage to enemy creatures in the area only (allies are safe!)." + S.HeightenedDamageIncrease(level, inCombat, "2d6"),
                    Target.Cone(12), // HA HA HA!! ALL WILL BURN!!!
                    level, SpellSavingThrow.Basic(Defense.Reflex))
                .WithSoundEffect(SfxName.Fireball)
                .WithNoSaveFor((spell, target)=> target.FriendOf(spell.Owner)) // Allies don't need to make a saving throw.
                .WithEffectOnEachTarget((async (spell, caster, target, checkResult) =>
                {
                    if (target.FriendOf(caster))
                    {
                        // Allies are excluded from the effect.
                        return;
                    }
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, checkResult, (2 * level) + "d6", DamageKind.Fire);
                }));
        });
        
        // This is an alternate way of adjusting existing spell: it offers you less power, since the spell is already created and you can only edit it, but on the plus side, you don't need to duplicate the entire
        // code of the original spell:
        ModManager.RegisterActionOnEachSpell(spell =>
        {
            if (spell.SpellId == SpellId.BurningHands)
            {
                ((ConeAreaTarget)spell.Target).ConeLength = 40; // I changed my mind, actually make the cone super big, ha ha!
                
                // Unfortunately, we do need to rewrite the description manually...
                spell.Description = Spells.CreateInitialDescriptionBlock(spell.Target, SpellSavingThrow.Basic(Defense.Reflex), "Gouts of flame rush from your hands IN A 200-FOOT CONE!!!!!!") +
                                    "Deal " + S.HeightenedVariable(2 * spell.SpellLevel, 2) + "d6 fire damage to enemy creatures in the area only (allies are safe!)." + S.HeightenedDamageIncrease(spell.SpellLevel, false, "2d6");
            }
        });

    }
}