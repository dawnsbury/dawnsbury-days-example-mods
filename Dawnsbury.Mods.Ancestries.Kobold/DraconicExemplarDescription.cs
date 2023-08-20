using System.Collections.Generic;
using Origin.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.Ancestries.Kobold;

public class DraconicExemplarDescription
{
    public static readonly Dictionary<string, DraconicExemplarDescription> DraconicExemplarDescriptions = new Dictionary<string, DraconicExemplarDescription>(); 
    
    public DamageKind DamageKind { get; }
    public bool IsCone { get; }
    public Defense SavingThrow { get; }

    public DraconicExemplarDescription(DamageKind damageKind, bool isCone, Defense savingThrow)
    {
        DamageKind = damageKind;
        IsCone = isCone;
        SavingThrow = savingThrow;
    }
}