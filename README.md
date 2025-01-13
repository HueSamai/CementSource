# Note
This repo has been archived because it has been moved to [here](https://github.com/CementGB-4-0/CementSource).

# CementSource

This is the source code for the Cement Mod Loader for Gang Beasts. This outlines the structure of this repo. If you want to learn how to use Cement, go to the documentation [here](https://github.com/HueSamai/CementSource/wiki/).

Okay now let me show you around.

## cement

The assetbundle for Cement.

## Cement.cs

This C# file contains the Cement class, which is the actual BepInEx plugin. It manages all the other classes as well as the interactions with Gang Beasts itself. If you want to dig around start here, in the Awake method.

## Helpers

The Helpers directory just contains a bunch of helper classes, not anything interesing.

## ModDownload

This directory contains the classes which all help downloading mods in some way.

## ModMenu

I mean it's pretty obvious this directory just contains the classes which handle the mod menu. Although I should mention that the mod menu is instantiated in the Cement.cs file.

## Mods

This directory contains all the classes which deal with mods directly, for instance, the ModFile class.

## That's all folks

That's literally it. There are almost no comments, so be warned. If you want to fix an issue, create a pull request, you know how it goes. 
