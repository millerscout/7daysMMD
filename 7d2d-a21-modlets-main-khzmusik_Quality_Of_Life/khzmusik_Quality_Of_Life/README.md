# Quality Of Life

A collection of small modifications that should make your in-game life more convenient.

These are intended to be modifications that are uncontroversial,
and do not require any custom assets.

## Features

* The "already read book" icon is now a semi-translucent green check mark.
    Inspired by this post on the forums:
    [Mod that lets you know you've already read a book?](https://community.7daystodie.com/topic/24306-mod-that-lets-you-know-youve-already-read-a-book/)
* The time it takes to scrap or smelt brass is reduced by half.
* If you have the required knowledge to craft ammo bundles,
    you can craft them from loose ammo (not just from raw materials).
* Dropped loot bags stick around for 60 (real-time) minutes before despawning.
    _(New for A21.)_
* Anvils can be scrapped for iron, or smelted in the forge.
    _(New for A21.)_

This version does not contain Khaine's Lockable Inventory slots, but it can be enabled in XML.
See below for details.

## Technical Details

This modlet uses XPath to modify XML files, and does not require SDX or DMT.
It should be compatible with EAC.
It does _not_ contain any new assets (such as images or models).
Servers should automatically push the XML modifications to their clients, so separate client
installation should _not_ be necessary.

Starting a new game should not be necessary, but is always recommended just in case.

### Ammo Bundles

There are new recipes for all ammo bundles,
where the ammo itself is the required ingredients,
not the raw materials.

These should take exactly as much ammo to craft as they give back.
If players want the raw material bonuses, they have to use the raw materials.

However, the bundles made from ammo also take 2/3 the time to craft.
This seem more "realistic,"
and takes into consideration the time the player may have spent crafting the loose ammo.

### Modifying the _icons_ for read/unread books

It is possible to also change the icons for both unread and already-read books.
If you want to change them, uncomment the relevant code in this modlet's `items.xml` file.

The icons in that code are the "add" image (a plus sign) for unread books,
and a check mark for already-read books.
But you can add any icon(s) you like.

Open `controls.xml`, and look at this entry in the `item_stack` node:
```xml
<sprite depth="8" name="itemtypeicon" width="24" height="24" sprite="ui_game_symbol_{itemtypeicon}" pos="2,-2" foregroundlayer="true" visible="{hasitemtypeicon}" color="{itemtypeicontint}" />
```

Now, match up the variables (in `{}`) with these properties in the "schematicMaster" item in `items.xml`:
```xml
<property name="ItemTypeIcon" value="book"/>
<property name="AltItemTypeIcon" value="book_read"/>
```

Putting it all together, I found the `ui_game_symbol_check.tga` filename,
stripped out the "ui_game_symbol_" prefix,
and used it as the value of the "AltItemTypeIcon" property.

I did the same for the "ItemTypeIcon" property,
except that I chose the `ui_game_symbol_add.tga` file.

There's a list of `ui_game_symbol_{x}` TGA files in the `XML.txt` file.

There is also a list of available icons here:
http://smuggerstech.com/7DTD/Assets/Sprites/

That website is not associated with 7D2D or The Fun Pimps,
so the link might be stale in the future.

### Lockable Inventory Slots

This modlet _used to_ include a modified version of Khaine's
[Lockable Inventory Slots](https://github.com/KhaineGB/KhaineA20ModletsXML/tree/main/KHA20-LockableInvSlots).

However, for this release, I decided not to include it.
Most people who want it already know about Khaine's modlet,
and if both this modlet and his are installed, they would conflict.

The XML for this feature _is_ included in `Config/XUi/windows.xml`, but it is commented out.
Go to that file and un-comment the XML if you prefer to use this modlet only.

I also included commented-out XML which adds the lockable inventory slots to _lootable containers._
This is done in order to add them to the junk drone's inventory,
which does not use the same window as vehicle storage.

However, this adds them to _any_ lootable container, such as a cabinet or cardboard box.
If this is not desired, leave this code commented out in `Config/XUi/windows.xml`:

```xml
<!-- <append xpath="/windows/window[@name='windowLooting']/panel[@name='header']/rect[@controller='ContainerStandardControls']">
    <combobox depth="3" name="cbxLockedSlots" pos="70,-7" width="100" height="30" tooltip_key="xuiStashLockedSlots" type="ComboBoxInt" value_min="0" value_max="{container_slots}" value_wrap="true" value_increment="1" />
</append> -->
```
