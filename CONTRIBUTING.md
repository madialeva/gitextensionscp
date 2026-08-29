# Contributing to Git Extensions Cross Platform

This is an independent fork of Git Extensions, maintained as a solo project with AI-assisted
development. The goal is migrating the UI from Windows Forms to Avalonia while keeping the
original application buildable and usable as a functional reference.

## Project model

- Development is tracked through [OpenSpec](openspec/) change proposals. Each change has a
  `proposal.md`, `design.md`, `specs/` and `tasks.md` under `openspec/changes/`.
- Implementation is assisted by AI agents that follow the specifications.
- Pull requests are the primary mechanism for integrating changes into the `avalonia/main`
  branch. CI (`fork-ci.yml`) validates every PR on Windows and Linux.

## How to contribute

Contributions are welcome. If you'd like to propose a change:

1. Open an issue describing what you want to do and why.
2. If the change is non-trivial, draft an OpenSpec proposal (`proposal.md` + `design.md`).
3. Implement the change on a feature branch and open a pull request against `avalonia/main`.
4. PRs must pass CI (build + unit tests on Windows and Linux) and be reviewed before merging.

## Code style

- Follow the existing conventions of the codebase (StyleCop analyzers are enforced at build
  time).
- Keep changes focused and minimal — one concern per PR.
- Run `eng/Verify.ps1` locally before opening a PR to confirm the full Windows solution builds
  and all unit tests pass.
- On Linux, run `bash eng/Verify-Linux.sh` to perform the equivalent cross-platform check.

## Credits

This project builds on [Git Extensions](https://github.com/gitextensions/gitextensions), a
project with hundreds of contributors over two decades. See
[contributors.txt](contributors.txt) for the upstream contributor list.
