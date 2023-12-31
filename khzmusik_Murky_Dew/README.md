# Murky Dew

Dew collectors produce murky water, but no heat.

Many people believe dew collectors should not add heat to the heat map, which is reasonable.
But without generating heat, dew collectors are somewhat overpowered,
especially with "farms" (multiple collectors) passively generating potable water.

This seems like a good trade-off.
Heat will still be generated,
but it will be generated by the campfires that are used to boil the murky water.

Since the water from dew collectors must be boiled to be potable,
dew collector farms will be less passive and overpowered.

## Features

* Dew collectors produce murky water, which must be boiled in a cooking pot to produce clean water.
* Dew collectors no longer produce "heat" for the heat map.

## Technical Details

This modlet uses XPath to modify XML files, and does not require C#.
It does not contain any additional images or Unity assets.

It is safe to install only on the server.
