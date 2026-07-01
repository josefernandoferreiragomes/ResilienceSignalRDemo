---
description: Commits, pushes, and creates a PR from the current branch to main
agent: build
subtask: true
---

Create a git commit (with pull first) and push, then create a PR to main.

**Instructions**

1. **Pull** — run `git pull` to sync with remote before committing

2. **Commit** all uncommitted code:
   - `git add -A`
   - `git commit -m <message>`

3. **Push** — `git push`

4. **Inspect** changes vs main with `git diff main...HEAD` (or the default branch name)

5. **Create PR** with:
   - **head**: current branch name (`git branch --show-current`)
   - **base**: main
   - **title**: short title based on changes
   - **body**: bullet points of changes

   CLI: `gh pr create --base main --head <branch> --title "<title>" --body "<body>"`

If `gh` is not installed, return "gh command not installed!"
