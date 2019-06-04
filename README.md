# MakeMKV

MakeMKV Automatic Beta key Update Tool

## Reasoning

The beta key expires quite often.
This requires an annoying extra step to obtain the new MakeMKV beta Key.

## How it Works

This tool contacts the API of https://cable.ayra.ch/makemkv/ to get the latest key. It uses the "xml" API method.
It will store some metadata in the registry to not exessively contact the API.
Under normal circumstances the tool will only check for a key once the old one has expired.
This means you don't need an internet connection to launch the updater until the key expires.

## Download

Check the "[Releases](https://github.com/AyrA/MakeMKV/releases)" section for the latest builds.
You can also clone the repository and build "as-is".
No dependencies outside of .NET 4.5 at all.

## Installing

The application will check various locations for the installed version of MakeMKV, this includes the default installation directory.
The chapters below are in the order the updater will check for the MakeMKV installation.

Regardless of how you installed MakeMKV, the updater always needs to be launched manually.
If you just launch MakeMKV itself, the key will not update.
The updater will in turn launch MakeMKV after the update check.

### Installed Version with Default Path

If you installed MakeMKV using the provided installer and didn't change the installation path,
you can launch the key updater from any location on your computer directly.

### Portable Version or Installed Version with Custom Path

If you installed into a custom directory or use MakeMKV in a portable fashion,
place the updater into the same directory that contains `MakeMKV.exe` and (optional) create a shortcut to the updater on the Desktop.

## Command Line Argument

You can force a key update with the `/force` switch
