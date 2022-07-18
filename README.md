# Rune Factory 5 Fix
[![Github All Releases](https://img.shields.io/github/downloads/Lyall/RF5Fix/total.svg)]()

This BepInEx plugin for the game Rune Factory 5 features:
- Proper ultrawide and non-16:9 aspect ratio support with pillarbox removal.
- Smoother camera movement with a higher update rate.
- Intro/logos skip.
- Graphical tweaks to increase fidelity.
- Adjusting field of view.
- Overriding mouse sensitivity.

## Installation
- Grab the latest release of RF5Fix from [here.](https://github.com/Lyall/RF5Fix/releases)
- Extract the contents of the release zip in to the game directory.<br />(e.g. "**steamapps\common\Rune Factory 5**" for Steam).
- Run the game once to generate a config file located at **<GameDirectory>\BepInEx\config\RF5Fix.cfg**
- The first launch may take a little while as BepInEx does it's magic.

### Linux
- If you are running Linux (for example with the Steam Deck) then the game needs to have it's launch option changed to load BepInEx.
- You can do this by going to the game properties in Steam and finding "LAUNCH OPTIONS".
- Make sure the launch option is set to: ```WINEDLLOVERRIDES="winhttp=n,b" %command%```

| ![steam launch options](https://user-images.githubusercontent.com/695941/179568974-6697bfcf-b67d-441c-9707-88cd3c72a104.jpeg) |
|:--:|
| Steam launch options. |

## Configuration
- See the generated config file to adjust various aspects of the plugin.

## Known Issues
Please report any issues you see.

- Run into issues after updating the mod? Try deleting your config file, then booting the game to generate a new one.
- If you get startup issues try disabling "Show launcher at start" in the game launcher as shown in the picture below.

| ![launcher](https://user-images.githubusercontent.com/695941/179290368-5c491498-76c1-4ca9-8d2c-60b582549a5f.jpg) |
|:--:|
| Thanks to pho on the WSGF Discord. |

## Screenshots
| ![ezgif-2-f2f4b5f8b5](https://user-images.githubusercontent.com/695941/179136231-ef35cf6d-99cf-46f4-8ff9-e6f34b9a6333.gif) |
|:--:|
| Ultrawide pillarbox removal. | 

## Credits
- [BepinEx](https://github.com/BepInEx/BepInEx) is licensed under the GNU Lesser General Public License v2.1.
- [@KingKrouch](https://github.com/KingKrouch) for various contributions.
