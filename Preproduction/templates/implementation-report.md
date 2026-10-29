# Neon Tunnel Runner — implementation report template

Status: TEMPLATE — complete only after actual implementation. Replace placeholders and remove this status line before final delivery.

Student: [first name / last name / student ID]  
Course: Computer Game Design, HW2  
Repository / final commit: [exact URL and hash]  
Editor / packages / OS / backend: [actual tested versions]  
Executable target / hardware: [actual values]

## Game and controls

Describe the three-lane endless car loop, lives, fuel, coin/distance score and difficulty. List A/D, arrow keys, Escape and menu controls. Explain win objective (best score) and both Game Over conditions.

## Implementation structure

Summarize GameManager singleton/states; RunSession ownership; fixed tick; player motion and swept contacts; abstract Interactable plus four overrides; bounded pools and reset; seeded safe generation and origin rebasing; New Input System actions/maps; PlayerPrefs best; UI/audio/VFX event subscribers. Include a small class/state diagram or screenshot of the actual hierarchy.

## Final runtime screenshots

Insert actual screenshots with captions and requirement IDs: Main Menu; HUD on open night road; tunnel; headlights/underglow low/high speed; fireworks launch/burst; collision sparks and pickup effect; Pause; GameOver with score/best. Do not use generated concepts as implementation evidence.

## Verification and limitations

Report actual test counts/results, seed coverage, platform, resolution, profiler p95 and memory/allocations, clean-clone and extracted-build checks. Link evidence. List known issues honestly. Do not write PASS for a check not executed.

## Sources and collaborators

List names/contributions, exact external URLs/licenses and AI tool assistance. Include Unity documentation and all asset/font/audio sources. State original concept inspiration without copying franchise assets.

## Submission checks

Final ZIP filename: [real name]  
SHA-256: [measured]  
Course upload receipt/time: [actual]  
Re-downloaded archive integrity/extracted launch: [actual result or pending]
