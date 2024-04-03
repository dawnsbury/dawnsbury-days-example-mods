using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Feats.General.ImpossibleToughness;

public class ImpossibleToughnessFeatLoader
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        // This is the "Hello, world!" mod that you can try compiling and loading to check if your local environment is set up correctly.
        ModManager.AddFeat(
            new TrueFeat(ModManager.RegisterFeatName("Impossible Toughness"), // This name will both be serialized into save files, and display to the user
                1, 
                "You were touched by the power of divinity and have become indestructible.",
                "You have 20,000 extra Hit Points.",
                new[] { Trait.General })
            .WithOnCreature(cr => cr.MaxHP += 20000));
    }
}