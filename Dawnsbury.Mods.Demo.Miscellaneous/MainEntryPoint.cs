using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Demo.Miscellaneous;

public class MainEntryPoint
{
    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        ABetterBurningHands.Apply();
        ANewWeapon.RegisterAWeapon();
        ANewWeapon.RegisterAWondrousItem();
    }
}