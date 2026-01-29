# Create PR Command

Create a feature branch from current changes and open a pull request.

## Instructions

1. **Check current state:**
   - Run `git status` to see uncommitted changes
   - Run `git branch` to see current branch
   - Run `git log --oneline -5` to see recent commits

2. **Handle uncommitted changes (if any):**
   - Stage relevant files with `git add <files>` (prefer specific files over `git add .`)
   - Commit with a descriptive message following conventional commits format
   - Include `Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>`

3. **Create feature branch (if on main):**
   - Create descriptive branch name: `feature/<short-description>` or `fix/<short-description>`
   - Run `git checkout -b <branch-name>`

4. **Push and create PR:**
   - Push branch: `git push -u origin <branch-name>`
   - Create PR using `gh pr create` with:
     - Clear, concise title (under 70 chars)
     - Summary section with bullet points
     - Test plan section
     - Footer: `Generated with [Claude Code](https://claude.com/claude-code)`

5. **Return the PR URL** to the user

## PR Body Format

```
## Summary
- <bullet point describing change>
- <bullet point describing change>

## Test plan
- [ ] <testing step>
- [ ] <testing step>

🤖 Generated with [Claude Code](https://claude.com/claude-code)
```

## Important Notes

- **Do NOT merge** - PRs require user approval before merging
- If PR already exists for the branch, provide the existing PR URL instead
- For trivial changes, ask the user if they want to commit directly to main instead
