using Dawnsbury.Modding;

namespace Dawnsbury.Mods.IncreaseLevelCapTo20;

public class IncreaseLevelCapTo20Mod
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        Constants.CharacterLevelCap = 20;
    }
}