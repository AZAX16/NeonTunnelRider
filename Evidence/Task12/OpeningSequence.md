# Normal saved-scene opening sequence

Original runtime camera renders, unmodified. No forced firework call, effect video, concept image or skybox effect. Both sizes render the same saved Game camera at the recorded simulation instant. Native paused Game-view frames are included separately.

| Frame | Playing time | Effect age | Rocket / child particles | Rocket or child mean world position |
| --- | ---: | ---: | --- | --- |
| [normal opening-launch](normal-opening-launch.png) · [4:3](normal-opening-launch-4x3.png) | 2.340s | 0.020s | 1/0 | [-16.0048141, 10.24, 151.905441] |
| [normal opening-ascent](normal-opening-ascent.png) · [4:3](normal-opening-ascent-4x3.png) | 2.780s | 0.460s | 1/0 | [-16.0048141, 15.5199986, 151.905441] |
| [normal opening-late-ascent](normal-opening-late-ascent.png) · [4:3](normal-opening-late-ascent-4x3.png) | 3.240s | 0.920s | 1/0 | [-16.0048141, 20.7999935, 151.905441] |
| [normal opening-burst-ignition](normal-opening-burst-ignition.png) · [4:3](normal-opening-burst-ignition-4x3.png) | 3.480s | 1.160s | 0/180 | [-16.0030689, 20.7980652, 151.9079] |
| [normal opening-burst](normal-opening-burst.png) · [4:3](normal-opening-burst-4x3.png) | 3.860s | 1.540s | 0/180 | [-15.9908123, 20.631176, 151.928467] |
| [fast opening-launch](fast-opening-launch.png) · [4:3](fast-opening-launch-4x3.png) | 2.340s | 0.020s | 1/0 | [-16.0048141, 10.24, 151.905441] |
| [fast opening-ascent](fast-opening-ascent.png) · [4:3](fast-opening-ascent-4x3.png) | 2.780s | 0.460s | 1/0 | [-16.0048141, 15.5199986, 151.905441] |
| [fast opening-late-ascent](fast-opening-late-ascent.png) · [4:3](fast-opening-late-ascent-4x3.png) | 3.260s | 0.940s | 1/0 | [-16.0048141, 20.7999935, 151.905441] |
| [fast opening-burst-ignition](fast-opening-burst-ignition.png) · [4:3](fast-opening-burst-ignition-4x3.png) | 3.480s | 1.160s | 0/180 | [-16.0030689, 20.7980652, 151.9079] |
| [fast opening-burst](fast-opening-burst.png) · [4:3](fast-opening-burst-4x3.png) | 3.860s | 1.540s | 0/180 | [-15.9908123, 20.631176, 151.928467] |

The ascent pause adds ten wall seconds without changing playing time, effect age or particles. The sequence resumes from that same state. `normal-visual.json` and `fast-visual.json` contain all timestamps, source states, counts, launch-sky checks and mean particle positions. `normal-native-ascent-paused.png` and `fast-native-ascent-paused.png` are native Game-view captures.

Bloom pairs, pickups, collision/terminal feedback, tunnel transitions, cap horizon and rebases are indexed in those JSON files.
