using System.Linq;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Feats.InfiniteFeatsAndSpells;

public class InfiniteFeatsAndSpellsModLoader
{
    [DawnsburyDaysModMainMethod]
    public static void Main()
    {
        ModManager.AddFeat(new TrueFeat(ModManager.RegisterFeatName("AdditionalGeneralFeats", "Additional General Feats"), 1,
            "You're more generally powerful than the average hero.",
            "Choose 2–10. You gain that number of additional general feats at the same level you picked this feat at.", [Trait.General],
            Enumerable.Range(2,10).Select(number => new Feat(ModManager.RegisterFeatName("AdditionalGeneralFeats" + number, number + " additional general feats"), null, "You gain " + number + " additional general feats at the level you picked this feat at.", [], null)
                .WithOnSheet(values =>
                {
                    values.AddSelectionOption(new SingleFeatSelectionOption("AdditionalGeneralFeats" + number, "Additional general feat", -1, ft => ft.HasTrait(Trait.General)));
                })
            ).ToList())
            .WithMultipleSelection());

        LoadOrder.WhenFeatsBecomeLoaded += () =>
        {
            foreach (var classSelectionFeat in AllFeats.All.OfType<ClassSelectionFeat>())
            {
                var technicalName = classSelectionFeat.FeatName.ToStringOrTechnical();
                ModManager.AddFeat(new TrueFeat(ModManager.RegisterFeatName($"Additional{technicalName}Feats", $"Additional {classSelectionFeat.Name} Feats"), 1,
                        "You're more generally powerful than the average hero.",
                        "Choose 2–10. You gain that number of additional class feats at the same level you picked this feat at.", [classSelectionFeat.ClassTrait],
                        Enumerable.Range(2, 10).Select(number => new Feat(ModManager.RegisterFeatName($"Additional{technicalName}Feats" + number, number + " additional class feats"), null, "You gain " + number + " additional class feats at the level you picked this feat at.", [], null)
                            .WithOnSheet(values => { values.AddSelectionOption(new SingleFeatSelectionOption($"Additional{technicalName}Feats" + number, "Additional class feat", -1, ft => ft.HasTrait(classSelectionFeat.ClassTrait))); })
                        ).ToList())
                    .WithMultipleSelection());
            }
            foreach (var ancestrySelectionFeat in AllFeats.All.OfType<AncestrySelectionFeat>())
            {
                var technicalName = ancestrySelectionFeat.FeatName.ToStringOrTechnical();
                ModManager.AddFeat(new TrueFeat(ModManager.RegisterFeatName($"Additional{technicalName}Feats", $"Additional {ancestrySelectionFeat.Name} Feats"), 1,
                        "You're more generally powerful than the average hero.",
                        "Choose 2–10. You gain that number of additional ancestry feats at the same level you picked this feat at.", ancestrySelectionFeat.Traits.ToArray(),
                        Enumerable.Range(2, 10).Select(number => new Feat(ModManager.RegisterFeatName($"Additional{technicalName}Feats" + number, number + " additional ancestry feats"), null, "You gain " + number + " additional ancestry feats at the level you picked this feat at.", [], null)
                            .WithOnSheet(values => { values.AddSelectionOption(new SingleFeatSelectionOption($"Additional{technicalName}Feats" + number, "Additional ancestry feat", -1, feat =>
                            {
                                if (!feat.HasTrait(Trait.Ancestry)) return false;
                                return values.Ancestries.ContainsOneOf(feat.Traits) || feat.Traits.Contains(Trait.AllAncestries);
                            })); })
                        ).ToList())
                    .WithMultipleSelection());
            }
        };
    }
}