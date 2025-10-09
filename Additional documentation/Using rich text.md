# Using rich text

Dawnsbury Days uses a custom rich text language for formatting text. Within any text shown on screen, you can use the following tags:

* **Bold:** `{b}Bold{/b}`
* **Italics:** `{i}Italics{/i}`
* **Color:** `{Blue}Blue text{/}` where "Blue" is an X11 name of a color. You can review the full list of colors and their names at https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.colors
* **Icon:** Shows an inline picture
  * `{icon:YellowWarning}` displays a built-in image
  * `{icon:modded:MyAssets\Portal.png}` displays a modded illustration
  * `{icon:customportrait:MyPortrait.png}` displays a custom portrait loaded by the user
* **Tooltip:** `{tooltip:StarNight}Night of the Shooting Stars{/}` where "StarNight" is a keyword you registered on mod load using `ModManager.RegisterInlineTooltip`
* **Link:** Shows a blue text that does something when you hover over it or click it
  * External webpage link: `{link:https://dawnsbury.neocities.org}Homepage{/}`
  * Spell (only works in character editor screen): `{link:SpellTechnicalName}`
  * Spell with the context of a class: `{link:Fireball:Wizard}`
  * Spell at a specific spell level: `{link:Fireball:Wizard:4}`
    * If you don't specify a spell level, it's shown at the lowest possible level, or at your character's maximum spell level if it's a cantrip or focus spell.
  * Feat: `{link:FeatTechnicalName}`