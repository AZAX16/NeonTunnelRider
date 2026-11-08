# Task 17 — Git publication and verification

Student: **Alireza Pooyan — علیرضا پویان — 400414039**. Project: **Neon Tunnel Runner**.

Date: **2026-11-08**. Repository: **https://github.com/AZAX16/NeonTunnelRider.git**.
Published branch: **main**. **PASS: both existing commits published and independently verified in an anonymous fresh clone.**

## Commit identity and scope

| Item | Status | Actual result |
| --- | --- | --- |
| Existing release source | PASS | `e97572aba25d8bcff7e336c4bf6ef8b6e3c540fc` |
| Release source parent | PASS | `ecb192e9fd855c24ed67a05bffb0208548f98bc6` |
| Existing delivery commit / published release snapshot | PASS | `d9578524cfa2018b5f04ca7f86155fdae3de995b` |
| Delivery commit parent | PASS | `e97572aba25d8bcff7e336c4bf6ef8b6e3c540fc` |
| Both commits reachable from local main | PASS | Both `git merge-base --is-ancestor` commands exited 0 |
| Published release HEAD | PASS | `d9578524cfa2018b5f04ca7f86155fdae3de995b`, matched local/remote main before the documentation follow-up |
| Both commits reachable from published main | PASS | Both ancestor checks passed in the anonymous GitHub clone |

Read the supplied submission checklist, generated-file extension, Tasks 01/16 verification, root and supplied `.gitignore`, `CREDITS.md`, `Docs/Environment.md`, and original preproduction Git requirements. No `AGENTS.md` was found in the project or ancestor search. The configured fetch/push URL agrees with Task 16 and `delivery/repository-link.txt`; no remote or repository settings were changed.

Both requested commits retain their exact identities and parent relationships. Reviewed their complete stats and name/status lists: **56 files** in the release commit and **86 files** in the delivery commit. The source changes are the previously completed Task 15B implementation and delivery builder; the following commit adds delivery documentation, report, screenshots and verification evidence. No development or Task 16 build/test procedure was repeated.

The original index was empty. Existing modified documentation and untracked recovery, historical evidence, ZIP and test-result files were preserved and were not staged. The Task 17 follow-up is limited to this record, supporting evidence and the necessary publication-status correction in delivery/repository-link.txt. It is a separate documentation checkpoint; neither requested commit is rewritten. No unrelated working-tree changes are staged.

## Repository integrity and ignore policy

| Check | Status | Evidence and scope |
| --- | --- | --- |
| Root `.gitignore` identity | PASS | SHA-256 `c735b75517ecf18563ce4bf269eb4fe158a5539f7d554823578a85eaec795f45` |
| Supplied `Preproduction/source/assignment.gitignore` | PASS | Same SHA-256; working file and committed copies match |
| Generated directories | PASS | No tracked Library, Temp, Obj, Build, Builds, Logs, UserSettings, MemoryCaptures or Recordings paths |
| Supplied ignore rules | PASS | Explicit directory probes match Library/, Temp/, Obj/, Build/, Builds/, Logs/ and UserSettings/ |
| Separate generated solution exclusion | PASS | Exact `/*.slnx` remains in `.git/info/exclude`; NeonTunnelRunner.slnx resolves to that rule; no tracked .slnx |
| Other documented local exclusion | PASS | Existing `/Evidence/Task14/editor.log` exclusion preserved; sanitized evidence is tracked |
| Tracked paths matching ignores | PASS | `git ls-files -ci --exclude-standard` empty; all **1,742 historical paths** also checked with `git check-ignore --no-index --stdin -z`, no matches |
| Required source and metadata | PASS | Assets, Packages manifest/lock, ProjectSettings, Game scene, documentation, report and delivery files present in HEAD; **1,742 tracked files**, **120,424,239 bytes** before Task 17 additions |
| Unity metadata | PASS | Zero missing asset/folder .meta files, zero orphan .meta files, zero duplicate asset GUIDs; .gitkeep files and Unity folder metadata retained |
| Sensitive data review | PASS | All **2,265 reachable objects**, including **1,821 blobs / 1,450 text blobs**, reviewed with private-key, service-token, credential-URL, password/secret assignment, command-line credential and JWT patterns; zero matches and no sensitive filenames. This is a bounded static review, not a guarantee against every possible secret format |
| Large/unusual files | PASS | All 18 reachable blobs of at least 1 MB identified: project audio, supplied reference PNGs, TMP asset, report PDF and historical sanitized/audio evidence. Largest is VectorNight.wav, 11,289,644 bytes. The deliberately retained 787,484-byte LastBuild.buildreport is documented verification evidence, not a player/cache directory |
| Machine-specific files | PASS | Existing tracked VS Code configuration uses the relative solution name and is documented as preserved; absolute paths in historical evidence describe the recorded environment. Raw credential-bearing Editor log remains locally excluded |
| Git object integrity | PASS | Local `git fsck --full` exited 0; reported existing dangling blobs, no corrupt/missing objects. Unreachable blobs are not part of the main history being published |
| Original files preserved | PASS | Original nonignored files plus complete existing Windows output and local exclude file were hashed before work; preservation audit found no unexpected modification/deletion |

`git check-ignore -v Library Temp Obj Builds` reports existing directory matches. Obj does not currently exist, so the additional **Obj/** probe explicitly checks its directory-only rule. No assignment ignore change or speculative ignore pattern was added. Local `.git/info/exclude` is intentionally not transported by Git; a fresh clone must apply the documented optional .slnx exclusion locally if Unity generates that file.

Evidence: [`../../Evidence/Task17/preflight.json`](../../Evidence/Task17/preflight.json), [`sensitive-file-review.json`](../../Evidence/Task17/sensitive-file-review.json), [`historical-path-review.json`](../../Evidence/Task17/historical-path-review.json), complete requested command outputs in `Evidence/Task17/preflight-00.txt` through `preflight-17.txt`, and the supporting audit script [`audit.py`](../../Evidence/Task17/audit.py). Hash-only preservation baseline is retained locally at `/tmp/neon-git-publication-20261108-PuJgZh/preservation-before.json`.

## Remote and publication results

The exact release snapshot verified below is **`d9578524cfa2018b5f04ca7f86155fdae3de995b`**, published at **2026-11-08 04:07:45 UTC**, then anonymously cloned and checked at **04:09:08 UTC**. A following documentation-only commit adds this record/evidence and corrects the delivery link. The full identity of that final commit is its Git metadata, available with `git log -1 --format=%H -- Docs/Verification/17-git-publication.md`; embedding its own hash in its contents would be self-referential. Final remote/local equality and clone checks are performed again after that commit; their full-hash result is retained locally in `/tmp/neon-git-publication-20261108-PuJgZh/final-publication.json` and reported in the final handoff.

| Check | Status | Actual result |
| --- | --- | --- |
| Configured repository identity | PASS | origin fetch/push both `https://github.com/AZAX16/NeonTunnelRider.git` |
| Initial remote baseline | PASS | `git ls-remote --heads origin` and `git ls-remote origin` exited 0 with no output; no heads or other refs |
| Anonymous web accessibility | PASS | GitHub repository HTTPS returned HTTP 200 before and after publication; both full commit pages are publicly reachable |
| Initial `git push -u origin main` attempt | FAIL | 2026-11-08 04:01:53 UTC, exit **128**. Configured VS Code credential helper connection refused; GitHub reports “No anonymous write access” and authentication failure |
| Authentication restored | PASS | Student authenticated GitHub CLI as AZAX16 and confirmed readiness; gh auth status succeeded. No token value recorded |
| Retry `git push -u origin main` | PASS | Exit 0; new main branch created and upstream tracking configured. Remote was rechecked empty immediately before retry |
| Publication completion | PASS | Both existing commits published without force or history rewriting |
| Remote immediately after failed first push | PASS | Rechecked heads and all refs: still empty; nothing published or overwritten |
| Published release main equals local HEAD | PASS | Remote reference equals `d9578524cfa2018b5f04ca7f86155fdae3de995b`; clone HEAD and full tree agree |
| Fresh remote clone and cloned main verification | PASS | Anonymous HTTPS clone into /tmp/neon-task17-github-clone-17Pgan/project; main at expected full hash; both requested commits reachable; clean tree and git fsck --full exit 0 with no findings |
| Fresh-clone Unity folders, metadata and ignore hash | PASS | All 1,742 release files present; zero missing essentials/metas, orphan metas, tracked generated/ignored paths. Working and committed ignore hashes match the supplied SHA-256. Documented .slnx extension applied only to clone-local info/exclude; ignore probes pass |

See [`initial-push.json`](../../Evidence/Task17/initial-push.json), [`remote-before.json`](../../Evidence/Task17/remote-before.json) and [`publication-result.json`](../../Evidence/Task17/publication-result.json). The first authentication failure is historical and resolved. Successful retry, reference comparison and anonymous-clone evidence are retained in `retry-push.json`, `remote-after-initial-publication.json`, `remote-clone-command.json` and `fresh-clone.json`. No original working-project file was changed during fresh-clone verification.

## Delivery link and preserved archive

**PASS:** `delivery/repository-link.txt` now contains the configured repository URL, published branch, full tested source hash **`e97572aba25d8bcff7e336c4bf6ef8b6e3c540fc`**, published delivery snapshot **`d9578524cfa2018b5f04ca7f86155fdae3de995b`**, direct release-commit URL and actual successful publication/anonymous-clone status. Its exact diff was reviewed before the separate documentation commit.

**PASS — preservation:** the existing `GD_Hw2_Alireza_Pooyan_400414039.zip` remains unchanged, SHA-256 **`80a9ab2b2e2458d2e20a05bb08fcbca29f020b2b2683534c1d9dd2a3d9d199b5`**. **Its embedded repository-link.txt now differs from the corrected delivery copy:** the old archive still states publication PENDING. The archived URL and tested source hash remain accurate. This task deliberately retains that archive and its existing hash; it does not silently replace or recertify the submission ZIP. A future intentionally updated submission archive must receive a new hash and archive verification.

All **3,071** original nonignored/protected files were checked against the pre-task hash baseline. Only the explicitly allowed delivery/repository-link.txt correction may differ. No ZIP, Persian report, Windows output, gameplay script, scene, prefab, GUID, package, setting, local exclude rule or build configuration was changed.

## Exact commands and reproduction

Commands below ran from `/home/alireza/NeonTunnelRider/NeonTunnelRunner`. Required outputs and exit codes are retained in the evidence files. The audit script runs read-only Git/file checks; its `snapshot` mode writes a hash-only baseline into its own evidence directory and must not replace a baseline needed for preservation comparison.

```bash
git status --short
git branch --show-current
git remote -v
git log --oneline --decorate -10
git rev-parse HEAD
git rev-parse e97572a
git rev-parse d957852
git show --stat e97572a
git show --stat d957852
git diff-tree --no-commit-id --name-status -r e97572a
git diff-tree --no-commit-id --name-status -r d957852
git show -s --format='%H%n%P%n%s' e97572a d957852
git merge-base --is-ancestor e97572aba25d8bcff7e336c4bf6ef8b6e3c540fc main
git merge-base --is-ancestor d9578524cfa2018b5f04ca7f86155fdae3de995b main
git ls-files
git ls-files -ci --exclude-standard
git status --short --ignored
git check-ignore -v Library Temp Obj Builds
git check-ignore -v Library/ Temp/ Obj/ Build/ Builds/ Logs/ UserSettings/ NeonTunnelRunner.slnx
sha256sum .gitignore Preproduction/source/assignment.gitignore
git diff --cached --stat
git ls-tree -rlz HEAD
git rev-list --objects --all
git cat-file --batch
git log --all --format= --name-only -z
git check-ignore --no-index --stdin -z
git diff e97572a^ e97572a -- Packages ProjectSettings .gitignore .vscode CREDITS.md Assets/NeonTunnelRunner/Scripts/Editor/BuildDelivery.cs
git diff d957852^ d957852 -- delivery/repository-link.txt Docs/Setup/Task16Package.py
git diff e97572a^ e97572a --numstat
git diff d957852^ d957852 --numstat
git fsck --full
git ls-remote --heads origin
git ls-remote origin
git push -u origin main
git ls-remote --heads origin
git ls-remote origin
curl --head --silent --show-error --location --max-time 30 --output /tmp/neon-git-publication-20261108-PuJgZh/repository-http-headers.txt --write-out 'Repository HTTP status: %{http_code}\n' https://github.com/AZAX16/NeonTunnelRider
python3 -B /tmp/neon-git-publication-20261108-PuJgZh/audit.py snapshot
python3 -B /tmp/neon-git-publication-20261108-PuJgZh/audit.py preflight
python3 -B /tmp/neon-git-publication-20261108-PuJgZh/audit.py secrets
python3 -B /tmp/neon-git-publication-20261108-PuJgZh/audit.py preservation
```

`git cat-file --batch` receives the object IDs from `git rev-list --objects --all`; matched values are never printed. `git check-ignore --no-index --stdin -z` receives the unique historical paths. Structured argv, working directories and exit codes for script-invoked commands are recorded in [`commands.jsonl`](../../Evidence/Task17/commands.jsonl). The retained script has portable root/output defaults; at execution, those defaults selected this working project and the temporary evidence directory named above.

## Final verification commands

These follow the first push and anonymous clone (the captured JSON files include exact argv, timestamps and results):

```bash
gh auth status
git ls-remote --heads origin
git ls-remote origin
git push -u origin main
git rev-parse HEAD
git ls-remote --heads origin refs/heads/main
GIT_TERMINAL_PROMPT=0 GIT_ASKPASS=/bin/false git -c credential.helper= clone --single-branch --branch main https://github.com/AZAX16/NeonTunnelRider.git /tmp/neon-task17-github-clone-17Pgan/project
python3 -B /tmp/neon-git-publication-20261108-PuJgZh/audit.py verify /tmp/neon-task17-github-clone-17Pgan/project
```

The clone verification checks main, exact HEAD/tree, both ancestor relationships, `git fsck --full`, clean checkout, required source/documentation files, .meta pairs, .gitkeep files, the actual checkout .gitignore hash and generated-file absence. The documented `/*.slnx` line was appended to **the clone's** local .git/info/exclude and explicit ignore probes passed. No Unity import, player build or Task 16 recertification was performed.

The separate documentation checkpoint uses only explicit Task 17 paths plus delivery/repository-link.txt. Review `git diff --cached --stat` and `git diff --cached -- delivery/repository-link.txt` before committing, then use `git push -u origin main`. After that push, compare `git ls-remote --heads origin refs/heads/main` with `git rev-parse HEAD`, fetch/fast-forward the isolated clone, and rerun the Git integrity checks against the final full hash. Full final commands and results are recorded in the local final-publication.json described above; their immutable pre-commit record is this reproduction procedure.

## Remaining actions and deferred issues

- **PASS:** no outstanding Git authentication, ignore-policy or source-integrity issue. Both requested commits are published; original history and unrelated local work are preserved.
- **PENDING:** if the student wants the submission ZIP to include the corrected publication-status text, prepare a deliberately reviewed new archive and hash later. The existing ZIP is retained unchanged and is not represented as byte-identical to the corrected delivery document.
- **PENDING, deferred by explicit Task 17 authorization:** existing Windows font-import warning and unresolved near-miss excerpt original source/redistribution permission. Both remain unresolved, unchanged and nonblocking for Git publication. No investigation or correction was performed.
- **NOT RUN:** new build, Unity import/test runs, performance certification and stopped soak. **PENDING:** native Windows acceptance, course upload and verification of the actual course download, retaining the existing Task 16 requirements and limitations.

Task 17 ends after the final publication checks. No later task is started.
