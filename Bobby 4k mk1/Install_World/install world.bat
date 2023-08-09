ECHO OFF
ECHO:
ECHO:
ECHO:
ECHO:
ECHO TERAGON's World Installation script (by Pille)
ECHO:
ECHO:
ECHO:
ECHO This script installs the present world as well as all POIs necessary to run it. 
ECHO For this purpose, the world files are copied to the directory
ECHO:
ECHO %appdata%\7DaysToDie\GeneratedWorlds\
ECHO:
ECHO and the modlet "Teragon POIs" is copied to
ECHO:
ECHO %appdata%\7DaysToDie\Mods\
ECHO:
ECHO If the modlet or the world already exist, they will be updated.
ECHO:
for %%I in ("%~dp0\..\.") do set ParentFolderName=%%~nxI
PAUSE
::Copy or modify the following line to provide your own POI modlets for your 7 Days world
robocopy ./"Teragon POIs" %appdata%/7DaysToDie/Mods/"Teragon POIs" /E /IS
robocopy ../ %appdata%/7DaysToDie/GeneratedWorlds/"%ParentFolderName%" /E /IS
ECHO:
ECHO:
ECHO:
ECHO The world has been installed and can now be selected from the main menu in game! The original world folder containing this script is no longer needed. Press ENTER to close this script.
ECHO:
ECHO:
ECHO:
PAUSE