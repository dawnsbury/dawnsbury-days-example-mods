using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Spellbook.AcidicBurst;

public class AcidicBurst
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ModManager.RegisterNewSpell("AcidicBurst", 1, ((spellId, spellcaster, spellLevel, inCombat) =>
        {
            return Spells.CreateModern(new ModdedIllustration("AcidicBurstAssets/AcidicBurst.png"), "Acidic Burst", new[] { Trait.Acid, Trait.Evocation, Trait.Arcane, Trait.Primal },
                    "You create a shell of acid around yourself that immediately bursts outward.",
                    "Deal " + S.HeightenedVariable(spellLevel * 2, 2) + "d6 acid damage to each creature in the area." + S.HeightenedDamageIncrease(spellLevel, inCombat, "2d6"),
                    Target.SelfExcludingEmanation(1), spellLevel, SpellSavingThrow.Basic(Defense.Reflex))
                .WithSoundEffect(ModManager.RegisterNewSoundEffect("AcidicBurstAssets/AcidicBurstSfx.mp3"))
                .WithEffectOnEachTarget((async (spell, caster, target, result) =>
                {
                    await CommonSpellEffects.DealBasicDamage(spell, caster, target, result, (spellLevel * 2) + "d6", DamageKind.Acid);
                }));
        }));
    }
}