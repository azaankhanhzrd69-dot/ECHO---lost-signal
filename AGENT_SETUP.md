# AGENT_SETUP.md: set up the agents first (about 30 minutes)

Do this on all three laptops before anyone starts a lane task.

> Items marked **(verify)** depend on your exact Antigravity version or free Nemotron host. I can't check those from here, so confirm them in the app or host docs.

## 1. Why two models
- **Gemini 3.8 Flash in Antigravity IDE** is your planner, logic writer, debugger and reviewer.
- **Nemotron 3.5 Lightning (free host)** is your fast junior for mechanical edits and boilerplate.
- They are separate tools with separate chats, so the **task card** is the hand-off. Gemini writes the card, a human pastes it to Nemotron, then the human pastes Nemotron's diff back to Gemini for review.

## 2. Repo prep (one person, once)
1. Make sure the cleanup commit and Git/LFS setup are on `main`.
2. Add to the repo root: `AGENTS.md`, `BUILDSPEC.md`, `AGENT_SETUP.md`. Add `docs/STORY.md` (your final storyboard as markdown) and `docs/CONTEXT.md` (10 lines: Unity version, folder layout, contract summary).
3. Fill in the Unity version in `AGENTS.md`.
4. Commit, push, everyone pulls (or copy by USB/LAN to save LFS bandwidth).

## 3. Antigravity IDE setup (each laptop)
1. Install Antigravity, sign in, open the project folder (the repo root containing `Assets/`).
2. Select Gemini 3.8 Flash as the model for the agent. Note your quota limit.
3. It reads `AGENTS.md` from the repo root. Start each session by saying "Read AGENTS.md first" to be sure.
4. Antigravity always asks before running terminal commands. Keep it that way, and never approve `git push` or anything with `--force`.
5. Add an Antigravity workflow/prompt snippet named "task" containing the prompt template from `BUILDSPEC.md` section 10, if your version supports saved prompts.
6. Confirm Antigravity doesn't index `Library/`, `Temp/`, `Logs/` or `Builds/` (they should be in `.gitignore`, which tools normally respect). Index only `Assets/ECHO`, `docs`, and the markdown files if there's a setting.

## 4. Nemotron setup (each laptop)
1. Run Opencode from the repo root and pick Nemotron 3.5 Lightning (free). Note its request limits and context window, since free tiers can be lower than the model's maximum.
2. Opencode reads `AGENTS.md` from the repo root. Confirm by asking it to summarise the hard rules before your first task.
3. Free tiers throttle. Keep each Nemotron task small: one file, one sentence of change, one example to copy.
4. Do not give Nemotron the whole 2,900-line `EchoGame.cs`; give it just the partial file or class involved.

## 5. Four laptops, one rule per laptop
Each person uses their own two sessions. Each laptop has its own quota, so the team has three times the tokens, but only if you don't duplicate work. **Claim a task in `BUILDSPEC.md` before you start it.**

## 6. The per-task loop
1. **Claim:** mark the task `[~]` with your name, push the tiny commit.
2. **Branch:** `git checkout -b core/C-05-monster-ai` (use your lane prefix).
3. **Plan (Gemini):** paste the prompt template with the task ID. Ask it to produce a *task card*: files to touch, acceptance criteria, and a list of sub-tasks marked "mechanical".
4. **Mechanical sub-tasks (Nemotron):** paste the card and one worked example. Run it one file at a time.
5. **Compile:** Editor open means paste Console errors back to whichever model wrote the code. Editor closed means run the batch command from `AGENTS.md`.
6. **Do the Editor steps yourself** (the list the agent returned): add components, assign fields, set layers.
7. **Play-test** by jumping to your beat with F1. Fix, repeat.
8. **Review:** paste the diff into the *other* model and ask it to review against `AGENTS.md`'s reviewer checklist.
9. **PR** into `main` with the Inspector steps listed. Core merges after a smoke test.

## 7. First-hour agent tasks (to prove the setup works)
| Who | Task | Model | Success |
|-----|------|-------|---------|
| Core | C-01 contract files | Gemini | Compiles |
| Core | Add annotated comments to the contract | Nemotron | Comments only, no logic change |
| World | W-03 geometry linter | Gemini | Menu item appears in Editor |
| Interact | Draft `voicelines.csv` and `sfx.csv` from the storyboard | Nemotron | CSV matches story beats |

If any of these fails, fix the setup before starting lane work.

## 8. Troubleshooting
- **Agent edits a scene or `.meta`:** revert with `git checkout -- <file>`, remind it of rule 1, and strengthen the rules file.
- **Agent uses deprecated APIs:** point to the API notes in `AGENTS.md`.
- **Agent invents story:** attach `docs/STORY.md` and say "do not add content not in this file".
- **Nemotron rate-limited:** move that task to Gemini and keep the Nemotron queue for big mechanical batches.
- **Merge conflicts in a scene:** you broke the one-owner rule; keep the owner's version and re-apply the other change via a prefab.
- **Pink materials after an import:** a URP/HDRP shader mismatch; Editor > Render Pipeline > Upgrade materials, human only.
