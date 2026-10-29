# Neon Tunnel Runner — game design

## Play promise

An original, low-poly hovercar races automatically through luminous geometric tunnels and open night highways. The player chooses among three lanes to avoid parked and moving traffic, collect gold coins, and reach the next green fuel can. Three lives and a draining tank make every lane choice meaningful. Runs end when either resource reaches zero; the persistent best score invites another run.

The required MVP is a single-player desktop keyboard game. No jumping, shooting, weapons, free steering, flight, procedural curves, networking, shops or unlock economy. The hovercar remains at a fixed ride height; tilt and bob exist only on its visual child.

## Controls and run loop

| Action | Binding | Behavior |
| --- | --- | --- |
| One lane left | A or Left Arrow | One command on button press; bounded at left lane |
| One lane right | D or Right Arrow | One command on button press; bounded at right lane |
| Pause/resume | Escape through System/Pause | Toggle only Playing/Paused; cannot double-trigger UI Cancel |
| Menu navigation | Arrow keys, Enter, pointer | New Input System UI actions |

Start initializes a clean run. The car starts in the center at 22 m/s with three lives and full fuel. The first 180 m are a forgiving pickup-only tutorial stretch, showing “A / D or ← / → — change lanes.” Traffic appears after this warmup. The road begins open to establish the nighttime sky, then alternates 3 open / 3 tunnel modules with entrance/exit variants at the boundaries. Random choices alter safe traffic patterns and decorative variants. These remain a genuine runtime procedural endless sequence, not one long prebuilt level.

## Tuning baseline — proposals, not assignment constants

| Parameter | Initial value | Rationale |
| --- | --- | --- |
| Unit scale / axes | 1 unit = 1 m; +Z forward, +Y up | Shared modeling and physics convention |
| Lane centers / road width | -3.2, 0, +3.2 m / 9.6 m | Three readable 3.2 m lanes |
| Player root / visible size | y0.6 m; 1.8×1.0×4.0 m | Compact hovercar; logical bounds are documented separately |
| Lane transition | 0.28 s per adjacent lane | Fast response with predictable duration |
| Lane queue | One command; last press replaces queued command | Never skip the center collision corridor |
| Start / max speed | 22 / 50 m/s (79 / 180 km/h) | High speed feel with capped difficulty |
| Difficulty | d=clamp01(max(elapsed/180, distance/6000)) | Both time and travel advance progression |
| Obstacle occupancy | lerp(0.25,0.8,d) per allowed non-safe lane slot | Density increases while safe corridor survives |
| Lives / collision grace | 3 / 1.0 playing second | One crash cannot consume every life |
| Fuel capacity / drain / refill | 100 / 2 per playing second / +30 | 50 s tank; 15 s restored per can |
| Fuel guarantee | One safe-route can every ≤220 m | ≤10 s at initial speed; recovery remains possible |
| Score | floor(total distance in m) + 100×coins | Distance rewards survival; coins reward route choices |
| Segment length / coverage | 60 m / ≥300 m ahead, ≥60 m behind | Stable visibility and recycling budget |
| Protected corridor block | 360 m traffic span + 120 m transition buffer | Simple conservative fairness with moving traffic |
| Fireworks | First launch 2–4 s, then 8–14 playing seconds in open sky | Background spectacle away from decisions |

## Lane and collision semantics

Target lanes are discrete. A command starts an adjacent transition; positions interpolate linearly in gameplay and the visual child may ease/roll. During a transition a second command occupies one queue slot, applied after arrival. Simultaneous opposite presses in one input update cancel. A held key does not repeat. Entering pause, menu or game over clears pending inputs.

Every obstacle activation can produce at most one encounter. An accepted encounter costs one life and starts grace. A second obstacle touched during grace is marked encountered without damage so lingering overlap cannot cause delayed damage. Damage, coin and fuel events are mutually typed; grace never blocks pickups. High-speed contact uses a relative swept-box test, not trigger callbacks alone.

Fuel drains only while Playing. Contacts are processed in time-of-impact order, pickups before obstacle on an exact tie; then fuel drain and end conditions are resolved. A fuel can reached on the final tick can rescue the run. If lives and fuel reach zero together, the final cause is Lives. Terminal resolution is idempotent.

## Fairness and pacing

Each 480 m block chooses a protected lane through its 360 m traffic span. No obstacle, including the entire future path of a moving car, enters that lane. The 120 m tail is a traffic-free transition to the next protected lane; a change can require at most two 0.28 s lane shifts. At 50 m/s, 120 m provides 2.4 s, comfortably exceeding 0.7 s reaction + 0.56 s motion + a 0.3 s margin. All traffic bounds stay outside transition buffers until the player has passed; invalid proposals are rejected. Keep the same protected lane if a new block cannot safely transition. These conservative rules favor a provable MVP over dense, arbitrary traffic.

Moving traffic stays in its original lane and travels +Z at 8 m/s. It is distinct from stationary traffic in world motion. Do not add lane-changing enemy AI. Preview its whole trajectory over the conservative encounter horizon before accepting it. Pickups may tempt the player into other clear lanes, but required fuel always has a route on the protected lane or the clear buffer. Pickup supply is spaced by distance and confirmed against the fuel budget; purely random fuel is not acceptable.

## Readability and feedback

Use cyan for navigable road/player, magenta for architecture, orange/red for obstacles, gold ring silhouettes for coins and green can silhouettes for fuel. Shape and placement must communicate meaning without relying on color. Two interior lane dividers plus outer road edges must always read as exactly three lanes. Reserve the center horizon for threats; fireworks occur high and lateral. The HUD displays speed, fuel, three lives, current score and best. Pause and Game Over freeze gameplay while leaving responsive menus.

## Completion boundary

The game is ready for assignment delivery only when every mandatory R row has real evidence, all required visual/audio/UI checks pass, a clean clone opens, and an extracted standalone build runs. This document and the concept images are preproduction specifications, not evidence that the Unity game exists.
