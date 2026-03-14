
## Notes for code structure

Quick note: Search for "FIX:" in osu.Game code to see changes made to the original code.

"UMania", "UbMania" or "UnbeatableMania" (+ variations with "Editor") may refer to the same thing and are used interchangeably.

Original files in osu.Game.Rulesets.UMania are prefixed with UMania, Ub, or Unbeatable.

# GitHub Actions Releases

Using Github Actions, releases are automatically created when a new tag is pushed.

## How to create a release

Create and push a tag:

   ```powershell
   git tag v1.0.0
   git push origin v1.0.0
   ```

A release will be automatically created by the GitHub Actions workflow.

(This document is mainly just for that I won't forget)