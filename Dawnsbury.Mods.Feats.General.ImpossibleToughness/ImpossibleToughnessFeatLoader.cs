using Origin.Core.CharacterBuilder.Feats;
using Origin.Core.CharacterBuilder.FeatsDb;
using Origin.Core.Mechanics.Enumerations;
using Origin.Modding;

namespace Dawnsbury.Mods.Feats.General.ImpossibleToughness;

public class ImpossibleToughnessFeatLoader
{
    [Origin.Modding.DawnsburyDaysModMainMethodAttribute]
    public static void LoadMod()
    {
        // This is the "Hello, world!" mod that you can try compiling and loading to check if your local environment is set up correctly.
        ModManager.AddFeat(
            new TrueFeat(FeatName.CustomFeat,  // All new custom feats should have the "CustomFeat" FeatName and you instead set the feat's name using .WithCustomName.
                1, 
                "You were touched by the power of divinity and have become indestructible.",
                "You have 20,000 extra Hit Points.",
                new[] { Trait.General })
            .WithCustomName("Impossible Toughness")
            .WithOnCreature(cr => cr.MaxHP += 20000));

    }
}