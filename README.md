> [!CAUTION]
> This plug-in is not finished an may not be suitable and production-ready. It works well enough but may change in future until it reached version 1.0

# CounterstrikeSharp - Native Map Votes

[![UpdateManager Compatible](https://img.shields.io/badge/CS2-UpdateManager-darkgreen)](https://github.com/Kandru/cs2-update-manager/)
[![GitHub release](https://img.shields.io/github/release/Kandru/cs2-native-mapvote?include_prereleases=&sort=semver&color=blue)](https://github.com/Kandru/cs2-native-mapvote/releases/)
[![License](https://img.shields.io/badge/License-GPLv3-blue)](#license)
[![issues - cs2-map-modifier](https://img.shields.io/github/issues/Kandru/cs2-native-mapvote)](https://github.com/Kandru/cs2-native-mapvote/issues)
[![](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.paypal.com/donate/?hosted_button_id=C2AVYKGVP9TRG)

This plug-in fullfills the need for 

## Installation

1. Download and extract the latest release from the [GitHub releases page](https://github.com/Kandru/cs2-native-mapvote/releases/).
2. Stop the server.
3. Move the "NativeMapVote" folder to the `/addons/counterstrikesharp/plugins/` directory.
4. Download The [CS2 Panorama Vote Manager](https://github.com/Kandru/cs2-panorama-vote-manager) and install it accordingly
5. Restart the server.

Updating is even easier: simply overwrite all plugin files and they will be reloaded automatically. To automate updates please use our [CS2 Update Manager](https://github.com/Kandru/cs2-update-manager/).


## Configuration

This plugin automatically creates a readable JSON configuration file. This configuration file can be found in `/addons/counterstrikesharp/configs/plugins/NativeMapVote/NativeMapVote.json`.

```json
{
  "enabled": true,
  "debug": false,
  "sfui_string": "#SFUI_vote_passed_changelevel",
  "sfui_prefix": "= = = =\u003E",
  "sfui_suffix": "",
  "rtv_enabled": true,
  "rtv_vote_duration": 30,
  "rtv_cooldown": 60,
  "rtv_success_command": "mp_halftime false; mp_maxrounds 1",
  "nominations_enabled": true,
  "nominations_max": 10,
  "endmap_vote_amount_total_maps": 10,
  "endmap_vote_amount_random_maps": 4,
  "changelevel_enabled": true,
  "changelevel_sfui_string": "#SFUI_vote_changelevel",
  "changelevel_vote_duration": 30,
  "changelevel_cooldown": 60,
  "changelevel_on_round_end": true,
  "feedbackvote_enabled": true,
  "feedbackvote_duration": 0,
  "feedbackvote_max_delay": 10,
  "maps": {},
  "ConfigVersion": 1
}
```

### enabled

Whether or not the plug-in is enabled.

### debug

Whether or not the console should list additional debug messages.

### sfui_string / sfui_prefix /sfui_suffix

Allows you to specify a custom SFUI-String for the cs2 panorama vote window. Defaults to the players localized changelevel string. There is a possibility to use a custom string, but each player has to move a local translation file into his game directory. It is NOT possible to transfer it to players via a workshop add-on. An example is available via [CS2 Panorama Vote Manager](https://github.com/Kandru/cs2-panorama-vote-manager) repository.

### rtv_enabled

Whether or not the !rtv command is enabled.

### rtv_vote_duration

The !rtv voting duration in seconds.

### rtv_cooldown

The cooldown after an !rtv vote ended (to avoid spamming votes).

### rtv_success_command

Command to run when !rtv was successful. Defaults to ending the match after the current round.

### nominations_enabled

Whether or not nominations for the voting after the match ended are enabled.

### nominations_max

Maximum number of nominations. Limited to 10 per default because the native map vote at match end cannot display more then 10 maps anyway...

### endmap_vote_amount_total_maps

Amount of maps to display via the native map vote at match end. This will be 50% the most liked maps and 50% the less played maps (from the map group) if not specified else via endmap_vote_amount_random_maps.

### endmap_vote_amount_random_maps

Amount of random maps from the map group displayed via the native map vote at match end.

### changelevel_enabled

Whether or not the !cl command is enabled.

### changelevel_sfui_string

Allows you to specify a custom SFUI-String for the cs2 panorama vote window. Defaults to the players localized changelevel string.

### changelevel_vote_duration

The !cl voting duration in seconds.

### changelevel_cooldown

The cooldown after an !cl vote ended (to avoid spamming votes).

### changelevel_on_round_end

Whether or not the map will be changed after round end on a successful vote. If disabled the map changes instantly.

### feedbackvote_enabled

Whether or not the feedback-vote after match end is enabled.

### feedbackvote_duration

The duration in seconds of the feedback vote. If value is 0 the value of the CVAR *mp_endmatch_votenextleveltime* will be used (which will end exactly when the native map vote at match end has finished).

### feedbackvote_max_delay

In case there is already a vote running when the feedback vote should start this value (in seconds) is the maximum delay allowed to decide whether a vote will be started or not. This is necessary because another plug-in could have a running vote and CS2 does only allow one vote at any given time. The vote will get queued and executed after the other plug-in finished the vote but maybe there is not enough time left for our feedback vote. You should only extend the default value if you have longer post-match times.

### maps

Inside maps all maps are saved with their respective positive or negative votes. You can use this via the commands below to get the best and worst maps and change your map group according to the feedback of your players.

## Commands

There are some commands players and server administrators can use:

### !skip / !rtv / !rockthevote

Starts a new Rock The Vote.

### !nom / !nominate <mapname>

Starts a nomination. Supports partly entered map names and gives a list to choose from if multiple maps are found.

### !noms / !nominations

Lists all current nominations if any.

### !cl / !cv / !map / !level / !changelevel <mapname>

Starts a vote to change the level to the given level. Supports partly entered map names and gives a list to choose from if multiple maps are found.

### nativemapvote (Server Console Only)

Ability to run sub-commands:

#### nativemapvote reload

Reloads the configuration

#### nativemapvote best_maps

Displays the best maps by end match voting.

#### nativemapvote worst_maps

Displays the worst maps by end match voting.

#### nativemapvote cleanup

Cleans up all the maps in the configuration file that are not in the current workshop maplist or in local map list.

## Compile Yourself

Clone the project:

```bash
git clone https://github.com/Kandru/cs2-native-mapvote.git
```

Go to the project directory

```bash
  cd cs2-native-mapvote
```

Install dependencies

```bash
  dotnet restore
```

Build debug files (to use on a development game server)

```bash
  dotnet build
```

Build release files (to use on a production game server)

```bash
  dotnet publish
```

Additionally add the dependencies (if not added already for the panorama-vote-manager):

```bash
git submodule add https://github.com/Kandru/cs2-panorama-vote-manager.git
git commit -m "added panorama-vote-manager as a submodule"
git push
```

## FAQ

TODO

## License

Released under [GPLv3](/LICENSE) by [@Kandru](https://github.com/Kandru).

## Authors

- [@derkalle4](https://www.github.com/derkalle4)
- [@jmgraeffe](https://www.github.com/jmgraeffe)
