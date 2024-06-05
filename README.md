# PortalRenderHelper
Helper mod for Celeste that adds a 2D equivalent to the world portals seen in other games (e.g. in Antichamber).

Non-Euclidean Celeste go brrrrr

## Building
Assuming you are familiar with building Celeste code mods (as explained [here](https://github.com/EverestAPI/Resources/wiki/Code-Mod-Setup)), this mod can by built by cloning this repo into the `Celeste/Mods/PortalRenderHelper` directory (i.e. there should be a file at `Celeste/Mods/PortalRenderHelper/everest.yaml`), and then by running these commands in the `PortalRenderHelper` directory:
```bash
dotnet restore
dotnet build
```

In addition, there is a test map packed as a separate mod in the `TestMap` folder. To use, add a shortcut at `Celeste/Mods/PortalRenderHelperTestMap` that points at the `TestMap` folder in this directory. (Alternatively, you can move the `TestMap` folder to the `Mods` folder.)
