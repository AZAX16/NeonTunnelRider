# Submission checklist

**This preproduction package is not the final HW2 submission.** It has no executable game or real gameplay screenshots. Complete tasks01–16, then submit the actual game.

## Required filename and content

Exact PDF pattern (printed p.1): `GD_Hw2_[firstName]_[lastName]_[StudentId].zip`. Replace bracketed placeholders with the student's actual details. The student's surname/ID were not provided and are intentionally not guessed.

```text
GD_Hw2_FirstName_LastName_StudentId.zip
  repository-link.txt
  Build/
    NeonTunnelRunner.x86_64
    NeonTunnelRunner_Data/
    UnityPlayer.so
    other emitted build companions
  ImplementationReport.pdf  (or clearly readable report format accepted by course)
  Screenshots/
  README-run.txt
  CREDITS.md
```

PDF p.5 explicitly requires a text file with Git repository link, executable build and short report describing implementation structure with final game images. Extra run instructions/credits/screenshots folder support those requirements. The full Unity source belongs in the Git repository; the PDF does not explicitly require duplicating it in the ZIP. If course staff separately requests source in the archive, follow that newer requirement and record it.

## Repository and build gate

- [ ] Use the exact accompanying `.gitignore`, preserved under source/assignment.gitignore here. Verify root copy matches; do not silently substitute a generic template.
- [ ] Commit Assets with `.meta`, Packages manifest/lock and ProjectSettings. Exclude Library, Temp, Obj, Logs, UserSettings and generated build folders according to supplied rules.
- [ ] Fresh clone/import with6000.6.3f1 has no missing scripts/GUIDs or compile errors. Record commit.
- [ ] Git URL is real, complete and accessible to the grader; do not change sharing without authorization. Include it in repository-link.txt.
- [ ] Confirm targetOS with grader. Linux x86_64 is the user's development default; another platform needs its actual build module and a tested build.
- [ ] Build succeeds in the actual Editor; save BuildReport. Include all emitted companions, not just executable.
- [ ] Extract to a clean folder and launch without Unity Editor. Check menus, bindings, both death conditions, restart, persistent best, VFX and all sounds. Quit works.
- [ ] For Linux preserve executable bit; README-run explains `chmod +x NeonTunnelRunner.x86_64` if extraction loses it. Test shared-library compatibility on target machine.

## Report and assessment evidence

- [ ] Briefly explain scene architecture, state transitions, pools, Interactable inheritance, safe procedural world, input actions, high score and pause/reset.
- [ ] Include actual final-game screenshots: main menu; HUD/open sky; tunnel; headlights/underglow at low/high speed; fireworks; collision/pickup effects; pause; GameOver. Captions identify relevant R rows and final commit.
- [ ] Credit collaborators and exact external source URLs (p.1); include asset/font/audio license provenance and AI assistance under course policy.
- [ ] Every mandatory row in requirement-evidence.csv has implementation and PASS evidence. Recommended streetlights are labeled accurately.

## Archive and course upload gate

- [ ] Replace every naming placeholder; preserve `GD_Hw2_` spelling/case and `.zip`.
- [ ] Run the provided archive verifier with the real submission path; inspect any failure. It checks naming, structure/CRC and reports a hash, not gameplay correctness.
- [ ] Extract the final archive to a fresh location and run that extracted copy; record SHA-256 and file list.
- [ ] Student uploads to the course system (p.1). This specification cannot claim upload without authorized access and an actual result.
- [ ] **Download the uploaded ZIP again**, CRC-test it, compare SHA-256 to the submitted copy, extract and run it. PDF says corrupted files receive no grade; the student is responsible for integrity.
- [ ] Record course submission receipt/time and re-download verification in task16 note. If the platform legitimately rewrites archive bytes, compare extracted file hashes and report the difference instead of falsely claiming identical ZIP hashes.
