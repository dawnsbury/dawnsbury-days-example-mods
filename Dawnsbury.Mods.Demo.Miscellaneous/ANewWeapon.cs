using System.Linq;
using Dawnsbury.Core;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Modding;

namespace Dawnsbury.Mods.Demo.Miscellaneous;

public static class ANewWeapon
{
    public static void RegisterAWeapon()
    {
        ModManager.RegisterNewItemIntoTheShop("Megaaxe", itemName =>
            new Item(itemName, IllustrationName.BattleAxe, "megaaxe", 0, 10, Trait.Razing, Trait.Sweep, Trait.Backswing, Trait.Backstabber, Trait.Forceful, Trait.Agile, Trait.Simple, Trait.BattleAxe, Trait.Axe)
                .WithWeaponProperties(new WeaponProperties("1d12", DamageKind.Slashing)
                    .WithAdditionalDamage("6d12", DamageKind.Fire)
                    .WithAdditionalPersistentDamage("40", DamageKind.Fire)));
    }

    public static void RegisterAWondrousItem()
    {
        var appleOfPower = ModManager.RegisterNewItemIntoTheShop("AppleOfPower", itemName =>
            new Item(itemName, IllustrationName.Apple, "apple of power", 0, 14)
            {
                Description = "While you hold the {i}apple of power{/i}, you have a +42 to AC and all saving throws."
            });
        
        // The apple of power doesn't do anything on its own, but...
        ModManager.RegisterActionOnEachCreature(creature =>
        {
            // We add an effect to every single creature...
            creature.AddQEffect(
                new QEffect()
                {
                    StateCheck = (qfTechnicalEffectThatMakesAppleOfPowerDoSomething) =>
                    {
                        var appleOfPowerHolder = qfTechnicalEffectThatMakesAppleOfPowerDoSomething.Owner;
                        if (appleOfPowerHolder.HeldItems.Any(heldItem => heldItem.ItemName == appleOfPower))
                        {
                            // ...and that creature is currently holding an apple of power, we give it another ephemeral effect (which will expire at state-check so if the creature stops holding an apple of power,
                            // it will be lost during the next state-check):
                            appleOfPowerHolder.AddQEffect(new QEffect(ExpirationCondition.Ephemeral)
                            {
                                BonusToDefenses = ((effect, action, defense) => new Bonus(42, BonusType.Untyped, "Apple of Power"))
                            });
                        }
                    }
                });
        });
    }
}