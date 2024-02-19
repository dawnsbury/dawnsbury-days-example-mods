using System.Collections.Generic;
using System.Linq;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Feats.General.ABetterFleet;

public class ABetterFleetLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        // This sample demonstrates how to replace an existing feat with a new one:
        AllFeats.All.RemoveAll(feat => feat.FeatName == FeatName.Fleet);
        ModManager.AddFeat(new TrueFeat(FeatName.Fleet, 1,
                "You {b}really{/b} move more quickly on foot.",
                "Your Speed increases by {b}10 feet{/b}.",
                new[] { Trait.General })
            .WithOnCreature(cr => cr.BaseSpeed += 2));

    }
}