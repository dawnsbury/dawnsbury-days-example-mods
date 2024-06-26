﻿using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Ancestries.Kobold;

public class KoboldAncestryFeat : TrueFeat
{
    public KoboldAncestryFeat(string name, string flavorText, string rulesText)
        : base(ModManager.RegisterFeatName(name), 1, flavorText, rulesText, new[]
        {
            KoboldAncestryLoader.KoboldTrait,
            // The following line is not needed -- because we registered the Kobold trait as an ancestry trait, the Ancestry trait is added automatically.
            // Trait.Ancestry
        })
    {
        // The following line is not needed -- because we registered the Kobold trait as an ancestry trait, the prerequisite is added automatically.
        // this.WithPrerequisite(sheet => sheet.Ancestries.Contains(KoboldAncestryLoader.KoboldTrait), "You must be a Kobold.");
    }
}