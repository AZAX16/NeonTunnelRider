Neon Tunnel Runner — Computer Game Design — HW2
علیرضا پویان — Alireza Pooyan — 400414039

Platform: Windows 64-bit (x86_64), Unity StandaloneWindows64, Mono.
Unity Editor used to build: 6000.6.3f1. No Unity Editor installation is needed to play.

1. Download GD_Hw2_Alireza_Pooyan_400414039.zip and verify its SHA-256 against
   the separately supplied .sha256 file / delivery verification record.
2. Extract the ENTIRE archive to a new writable directory. Do not run from
   inside the ZIP or copy only the executable.
3. Launch Build/Windows/NeonTunnelRunner.exe.
   Keep NeonTunnelRunner_Data, UnityPlayer.dll, MonoBleedingEdge, and every
   other emitted Unity file/directory in their original relative locations.
   They contain game assets and runtime code needed by the executable.
4. Start: Start button. Steer: A/D or Left/Right arrows, one lane per press.
   Pause/resume: Escape. Menus: mouse, directional navigation and Enter.
   Pause has Resume / Restart / Main Menu. Game Over has Restart.
   Quit: Escape → Main Menu → Quit (from the main menu).
5. Goal: survive, collect coins and fuel, improve the best score. Zero lives
   or zero fuel ends the run. Each coin adds 100; fuel cans add up to 30 fuel.
   Rear lane warnings precede proposed overtaking traffic. To earn the full
   near-miss effect, hold behind the same slower car in the same lane for
   2.5 continuous Playing seconds, then complete a clean adjacent pass in
   the next second. Early departure/contact cancels. Pause freezes the cue.

TEST ENVIRONMENT / HONEST LIMITS
Preparation, Unity tests and genuine supplementary screenshots used Linux Mint
and Unity 6000.6.3f1. Screenshots are Editor Play Mode, including disclosed
controlled fixtures; they are NOT Windows standalone captures.
The release is built for Windows, but native Windows launch, extracted launch,
Quit and persistence across Windows process launches are BLOCKED here: the
student confirmed there is no Windows machine/VM. No Wine proof is substituted.
No hardware minimum or additional runtime installation is certified.
If Windows/security software blocks launch, record the exact warning/error,
verify the archive hash and all companions, and use the institution's approved
handling for that specific message. Do not infer playability from ZIP CRC.

REQUIRED WINDOWS ACCEPTANCE (student / a native Windows tester)
Record Windows version, test date and release source commit from repository-link.txt.
Check startup/main menu; Start; mouse/keyboard/menu navigation; Pause/Resume;
Restart from Pause and Game Over; return to Main Menu; all three lanes and
0.12 simulation-second adjacent transitions; forward/following traffic; rear
warning and overtaking; safe NPC lane changes when admitted; strict near-miss
hold, early cancellation, contact cancellation and successful adjacent pass;
65→58→65 camera presentation; pitch-1 special music, normal music continuity
and pause/focus retention; fuel drain/refill; coins/score; life damage/grace;
zero-life AND zero-fuel Game Over; headlights/underglow at low/high speed;
fireworks, sparks and pickup effects; actual Quit.
Earn a score, quit completely, relaunch and check best persisted. Preserve any
higher earned score. Record each result as PASS/FAIL/BLOCKED/PENDING/NOT RUN.

REPOSITORY / ATTRIBUTION
See repository-link.txt: public URL is supplied; publication of the exact local
release commit remains pending until verified remotely. No automatic push.
See CREDITS.md: near-miss excerpt download provenance and redistribution
permission remain unresolved; no permission is invented.

COURSE UPLOAD AND DOWNLOAD VERIFICATION — PENDING STUDENT ACTION
Upload the exact named ZIP only after resolving the pending acceptance items.
Keep the .sha256 value separately. Download the uploaded ZIP again from the
course system; do not merely reuse your local file. In PowerShell:
  Get-FileHash .\GD_Hw2_Alireza_Pooyan_400414039.zip -Algorithm SHA256
Compare the full 64-digit value. With Python available, run:
  python Preproduction/tools/verify_submission.py GD_Hw2_Alireza_Pooyan_400414039.zip
Or check CRC independently:
  python -c "import zipfile; z=zipfile.ZipFile('GD_Hw2_Alireza_Pooyan_400414039.zip'); print(z.testzip())"
CRC returns None on success. Extract the downloaded copy into another new
directory and launch its Windows executable; repeat the acceptance checklist.
Keep the real submission receipt/time and downloaded hash/results. No upload,
receipt, downloaded copy or post-upload launch has been fabricated.
