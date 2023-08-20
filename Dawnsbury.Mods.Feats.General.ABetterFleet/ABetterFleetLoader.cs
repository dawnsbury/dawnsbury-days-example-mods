using Origin.Core.CharacterBuilder.Feats;
using Origin.Core.CharacterBuilder.FeatsDb;
using Origin.Core.Mechanics.Enumerations;
using Origin.Modding;

namespace Dawnsbury.Mods.Feats.General.ABetterFleet;

public class ABetterFleetLoader
{
    [Origin.Modding.DawnsburyDaysModMainMethodAttribute]
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