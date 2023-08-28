using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.Ancestries.Kobold;

public class KoboldAncestryFeat : TrueFeat
{
    public KoboldAncestryFeat(string name, string flavorText, string rulesText)
        : base(FeatName.CustomFeat, 1, flavorText, rulesText, new[] { Trait.Ancestry, Trait.Kobold })
    {
        this
            .WithCustomName(name)
            .WithPrerequisite(sheet => sheet.Ancestries.Contains(Trait.Kobold), "You must be a Kobold.");
    }
}