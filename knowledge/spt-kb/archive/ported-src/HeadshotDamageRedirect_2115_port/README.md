*Credit to Solarint for the original mod and Somtam for enhancements. I've updated it for SPT 4.0.7 and verified it works great!*

# Headshot Damage Redirect

A BepInEx mod for Single Player Tarkov that redirects headshot and chest damage to other body parts, giving you a chance to survive those instant-death moments.

## Features

- **Damage Redirection**: Splits headshot/chest damage between multiple body parts
- **Fully Configurable**: Adjust redirect percentage, thresholds, and target body parts
- **Smart Options**: Minimum damage threshold, maximum damage cap, chance-based activation
- **Dad Gamer Mode**: Global damage reduction for casual play
- **In-Game Config**: Press F12 to configure all settings
- **FIKA Compatible**: Works in multiplayer

## Installation

1. Download the latest `perfk.HeadShotRedirect.dll` from [Releases](https://github.com/perfk/Solarint-HeadshotDamageRedirect/releases)
2. Place it in `SPT/BepInEx/plugins/`
3. Launch the game
4. Press F12 to configure settings

## How It Works

The mod splits incoming damage proportionally:

**Example (60% redirect on 100 damage headshot):**
- Head takes: 40 damage
- Other body parts: 60 damage (distributed)

## Configuration

Press **F12** in-game to access all settings:

- Redirect percentage (0-100%)
- Minimum damage threshold
- Maximum damage cap
- Chance to redirect
- Body part selection
- Global damage scaling

Config file: `BepInEx/config/com.perfk.dmgRedirect.cfg`

## Compatibility

- SPT 4.0.7
- FIKA

## Credits

- **Original**: [Solarint](https://github.com/Solarint/Solarint-HeadshotDamageRedirect)
- **Updated by**: [Somtam](https://github.com/gitTerebi/Solarint-HeadshotDamageRedirect)
- **Updated for 4.0.7**: [Perfk](https://github.com/perfk/Solarint-HeadshotDamageRedirect)

## License

MIT License - See [LICENSE](LICENSE) for details
