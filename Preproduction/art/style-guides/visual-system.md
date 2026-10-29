# Mini visual design system

## Color tokens

| Token | HEX | Role |
| --- | --- | --- |
| Ink | #070B22 | Sky and deep panel backdrop |
| Surface | #141B3F | Body and architectural planes |
| Track | #35E6FF | Navigable lane edges, player identity, focused controls |
| Energy | #FF4AC8 | Thrusters and architectural accent |
| Depth | #8B5CFF | Secondary surfaces, dim background forms |
| Coin | #FFD76A | Gold ring, score reward |
| Fuel | #69F5A9 | Can, fuel bar |
| Hazard | #FF8B4D | Obstacle bodies, damage sparks |
| Text | #F2F5FF | Labels and all primary values |
| Muted | #9AA7C9 | Secondary labels only |

Colors describe display-space swatches. Emissive HDR intensity is a separate material parameter, not a different HEX string. Avoid clipping cyan and magenta into white. Keep text on near-opaque Ink panels; do not rely on Bloom for legibility. Numeric status always has labels/shape, not color alone.

## Typography and layout

Use the project's bundled TMP font for graybox. For final art choose one licensed geometric sans with open counters and clear0/O,1/I; retain its license and source URL. Bold display style for titles, medium weight for labels, tabular digits for HUD. Do not require a paid font. At1920×1080: title64–80px, HUD values36–48, labels24–28, buttons32; inspect at720p. Space on an8px grid; minimum48px screen margin, 16px internal gaps; required buttons at least240×64 reference pixels.

HUD anchors: score+best top-left, lives top-right, speed bottom-left, fuel bottom-right. Reserve middle60% of width and upper road horizon for gameplay. Every fuel state retains percentage text. Lives use3 icons plus numeric count. High score is max(previous best,current score). Numbers in image concepts are examples only.

## Controls and feedback

Default button: dark fill, thin muted border. Focus/hover: cyan border and slightly brighter fill; pressed: light fill with dark text. Disabled: muted text with no action; do not hide focus. Input selection starts on Start, Resume or Restart depending on panel. Pause menu uses the same panel style with Resume, Restart and Main Menu.

Damage: .12s flash with .25s decay; small camera impulse≤.12m; 1s grace indicated by a gentle pulsing outline, no intense strobe. Pickup burst≤.5s. Thruster streaks remain behind the vehicle. Fireworks must not overlap the road's central decision horizon; maximum two visible bursts initially. No heavy chromatic aberration or motion blur.

## Materials and shape language

Faceted bevels, octagonal arches, broad planes and two restrained accent strips. Player body cool; hazards warm; coins circular with hole; fuel rectangular with handle. Sparse window dots imply a city without detailed buildings. Three lanes are defined by two internal lines, never a dense grid of competing longitudinal lights. Architecture may have magenta side beams but they sit clearly outside the drivable width.

## Visual QA

Verify in real Unity: headlights cast road light; underglow light and emission rise with actual speed; player dark body separates from road; fuel/coin recognizability without color; all five HUD fields legible; pause and over screens usable; sky/fireworks visible on open segments. Generated images illustrate style and may exaggerate reflection/glow; the numeric technical budgets and gameplay rules take precedence.
