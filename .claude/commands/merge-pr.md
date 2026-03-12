# Merge PR Command

Finalize and merge the current branch PR end-to-end: commit and push code first, merge, sync `main`, and delete the old branch locally/remotely.

## Instructions

1. **Inspect git state:**
   - Run `git status --short`
   - Run `git branch --show-current`
   - If current branch is `main` or `master`, stop and say a feature branch is required

2. **Ensure all code is committed:**
   - If there are changes, run `git add -A`
   - Commit with a clear message (use user-provided message when available)
   - If no message is provided, use: `chore: finalize <branch> before merge`

3. **Push branch before merge:**
   - Run `git push -u origin <branch>`
   - Continue only if push succeeds

4. **Find or create PR:**
   - Try `gh pr view --json number,url,state,headRefName,baseRefName`
   - If PR does not exist, create it targeting `main` (or repo default base when `main` is absent)

5. **Merge PR:**
   - Use requested method (`--merge`, `--squash`, `--rebase`), default to `--merge`
   - Run `gh pr merge <number> <method>`
   - If merge fails (checks/conflicts/policy), stop and report the exact blocker

6. **Sync local main branch:**
   - Run `git checkout main`
   - Run `git pull --ff-only origin main`

7. **Clean up old branch:**
   - Run `git branch -d <old-branch>`
   - Run `git push origin --delete <old-branch>`
   - If remote deletion says branch does not exist, treat as already cleaned up

8. **Return merge results:**
   - Include PR URL and merge result
   - Confirm `main` is updated locally
   - Confirm local + remote branch cleanup status

## Important Notes

- Never delete branches if merge did not complete.
- Never force-delete unmerged local branches.
