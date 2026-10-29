# HW2 — complete English reading

Source has a cover followed by printed pages1–5. This is a faithful reading in plain English, organized by the original pages. Requirement IDs and exact implementation mapping are in document01. No numeric grading weights or deadline are supplied.

## Cover

Iran University of Science and Technology, Faculty of Computer Engineering, Computer Game Design. Instructor: Dr. Behrouz Minaei. Assistants: Amirhossein Mohammadipour and Mohammad Haghighat. Homework2, first semester1405–1406.

## Printed page1 — rules and considerations

If you consult other students or use sources beyond class content, you must state the names of the people and the exact addresses of sources. Put the project file in a ZIP named according to `GD_Hw2_[firstName]_[lastName]_[StudentId]` and upload it to the course system. The student is responsible for the uploaded file's integrity. Corrupted files receive no grade. Be sure to download and check your own file after uploading.

## Printed page2 — practical project and core requirements

Implement a3D endless car game with Unity. The player controls a car on an endless three-lane road at night, viewed in third person. The car travels at high speed; the player switches lanes to avoid obstacle cars, collect coins and manage fuel. The exercise develops3D game skills, procedural environments, object pooling and design patterns, with special attention to visual effects, nighttime lighting and artistic quality.

The road must be generated procedurally and endlessly. Segments are dynamically created ahead of the player and recycled after the car passes. There are3 lanes, changed with A/D or left/right arrow keys. All game inputs must exclusively use Unity's New Input System, with Input Actions and Action Maps. Projects implemented with the old Input system receive no grade for this section.

The player's car has3 lives, shown in the UI. Contact with an obstacle car removes1 life and must give suitable visual feedback, for example a color change or camera shake. At zero lives the game ends. The car has a fuel tank that decreases with time. Fuel cans along the route replenish it when collected.

## Printed page3 — systems and visual presentation

Running out of fuel ends the game, and remaining fuel must be shown as a fuel bar. With elapsed time and greater distance, difficulty must gradually increase through higher car speed and greater obstacle-car density.

All recurring game objects—road segments, obstacle cars, coins and fuel cans—must use Object Pooling. Repeated creation/removal using Instantiate/Destroy is not acceptable. Include at least two obstacle types (stationary and moving cars) and two collectible types (coin and fuel can). All these elements must inherit a shared abstract class named Interactable and use virtual and override methods. The states Menu, Playing, Paused and GameOver must be managed through a GameManager using the Singleton pattern.

Artistic and visual appeal is an important part of assessment and the following items earn credit. The environment must be nighttime, with an appropriate starry or cloudy night skybox, localized darkness and nighttime road illumination. The player's car must have headlights plus neon underglow. Neon intensity/glow must be adjusted to instantaneous speed, becoming brighter as speed increases. Streetlights along the road are recommended. A fireworks system must launch periodically or randomly into the sky and create attractive colored effects.

## Printed page4 — particles, audio, UI and persistence

ParticleSystem effects are necessary for exhaust smoke/fire, collision sparks and disappearing collectibles. Bloom, described as from the Post-Processing package, is mandatory for realistic glow on neon and firework explosions.

The game needs suitable sound: background music, engine sound, collision sound and pickup sounds for coins and fuel. Main Menu contains Start and Quit. Pause Menu contains Resume, Restart and Return to Main Menu. Game Over shows current score, highest score and Restart. In-game HUD shows live car speed, fuel bar, lives remaining, current score and highest score. High Score must be saved using PlayerPrefs so it is retained after exiting the game.

## Printed page5 — delivery

Put the Unity project in a Git repository, such as GitHub or GitLab. The final file uploaded to the course system is a ZIP containing: (1) a text file with the project's Git repository link—be sure to use the .gitignore supplied alongside the assignment so large folders such as Library are not uploaded and only necessary project files are included; (2) an executable build of the game; and (3) a short report explaining implementation structure with images of the final game output.
