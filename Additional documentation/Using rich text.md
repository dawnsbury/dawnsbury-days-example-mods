# Using rich text

Dawnsbury Days uses a custom rich text language for formatting text. Within any text shown on screen, you can use the following tags:

* **Bold:** `{b}Bold{/b}`
* **Italics:** `{i}Italics{/i}`
* **Strike:** `{strike}This text will be crossed-out{/strike}`
* **Color:** `{Blue}Blue text{/}` where "Blue" is an X11 name of a color. You can review the full list of colors and their names at https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.colors
* **Icon:** Shows an inline picture
  * `{icon:YellowWarning}` displays a built-in image
  * `{icon:modded:MyAssets\Portal.png}` displays a modded illustration
  * `{icon:customportrait:MyPortrait.png}` displays a custom portrait loaded by the user
* **Tooltip:** Changes text color, bolds the text, and displays a tooltip when the player hovers over the text
  * `{tooltip:StarNight}Night of the Shooting Stars{/}` where "StarNight" is a keyword you registered on mod load using `ModManager.RegisterInlineTooltip`
  * `{r}flying{/r}` where "flying" is a rules term you or the base game registered using `ModManager.RegisterInlineTooltip`
  * `{r:Alignment damage}evil damage{/r}` where "Alignment damage" is a rules term you or the base game registered using `ModManager.RegisterInlineTooltip`
* **Link:** Shows a blue text that does something when you hover over it or click it
  * External webpage link: `{link:https://dawnsburydays.com}Homepage{/}`
  * Spell (only works in character editor screen): `{link:SpellTechnicalName}fireball{/}`
  * Spell with the context of a class: `{link:Fireball:Wizard}fireball{/}`
  * Spell at a specific spell level: `{link:Fireball:Wizard:4}fireball{/}`
    * If you don't specify a spell level, it's shown at the lowest possible level, or at your character's maximum spell level if it's a cantrip or focus spell.
  * Feat: `{link:FeatTechnicalName}`, e.g. `{link:DoubleSlice}Double Slice{/}`
  * Item: `{link:ItemTechnicalName}`, e.g. `{link:MinorHealingPotion}a potion{/}`