# Rain Collector

Turns the dew collector into something more like a rain collector.

## Features

* The dew collector fills more rapidly when it is foggy.
* The dew collector fills much more rapidly when it is raining.
* The dew collector will not do anything if the outside temperature is below the freezing point of water.
* To make up for the fog and rain boost, the dew collector takes longer to fill up overall.
* Adds the _ability_ to increase the number of "slots" in the dew collector.
  The XML changes are in `blocks.xml` and `windows.xml` but are commented out.
  See below for instructions.
  _Added in version 21.0.1.1._

This is all configurable in the XML for the dew collector.

## Technical Details

**This modlet contains custom C# code.**
It is *not* compatible with EAC.
It must be installed on both clients and servers.

I **strongly** recommend starting a new game after installing this mod.

However, if you choose to install it into an existing game,
then _at a minimum_ you should pick up and re-place existing dew collectors.
(See below for the reason.)

### Customizing

This mod adds these properties to the dew collector block:

* `MinConvertTemperature`:
  The minimum temperature, in degrees fahrenheit, at which the dew collector will work.
  The minimum temperature is set to 32, which is the freezing point of water at sea level.
  _This means the dew collector will often stall in the snow biome._
* `FogConvertMultiplier`:
  This a multiplier for the fog density.
  The mod takes the existing collection amount, multiplies it by the fog density,
  then by this multiplier, and adds the result to the existing collection amount.
  The fog density is usually in the range of `0.1 - 0.45` in the vanilla game.
  Clear days are usually around 0.1, foggy days are usually around 0.3,
  and the fog density may go up to 4.5 during heavy rains.
  The multiplier is set to 3, which means foggy days usually double the collection amount.
* `RainConvertMultiplier`:
  This a multiplier for the rainfall amount.
  The mod takes the existing collection amount, multiplies it by the rainfall amount,
  then by this multiplier, and adds the result to the existing collection amount.
  When raining, rainfall can be in the range of `0.1 - 1` in the vanilla game.
  The multiplier is set to 5, which means a light rain can double the collection amount,
  and a heavy rain can increase it five fold.
* `IsChunkObserver`:
  If true (the current value), this makes the dew collector a chunk observer.
  This will make the rain and fog calculations accurate when the player is not in the same chunk.
  However, this will have a minor performance impact per additional active chunk,
  so I made it configurable with this property.
  If set to false, when the player re-enters the chunk,
  the additional collection amount for the _entire_ time the player was away
  will be calculated using _only the current_ weather values.

All of this is configurable by changing the values.
In addition, if you want to totally disable a new feature (like the minimum temperature),
you can remove the relevant `<property>` tag.

Because the collection amounts are significantly increased during fog or rain,
I also doubled the time that it takes for a dew collector to collect a jar of water.

This is done by doubling the vanilla `MinConvertTime` and `MaxConvertTime` values.

#### Changing the number of "slots"

The "slots" are the number of empty squares in the dew collector's output grid.
Each slot generates one bottle of water when filled.
There are three slots in the vanilla game.

To change the number of slots, you must make two changes:

1. In `blocks.xml`, add the "ContainerSize" property:
    ```xml
    <property name="ContainerSize" value="4,2" />
    ```
    The value represents the number of _columns,_ then the number of _rows._

2. In `XUi/windows.xml`, set the columns and rows on the "windowDewCollector" grid:
    ```xml
    <set xpath="//window[@name='windowDewCollector']/rect/grid/@cols">4</set>
    <set xpath="//window[@name='windowDewCollector']/rect/grid/@rows">2</set>
    ```

**These values must match!**
If they do not, _your game will crash._

This XML is already in `blocks.xml` and `windows.xml` but is commented out.
I used two rows of four columns each, but you can use any numbers you want (within reason).
