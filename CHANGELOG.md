# Changelog

## 1.6.1 - 2026-09-03

### Changed

- Reorganized WSL management into responsive Overview, Settings, Storage, and Network & USB tabs.
- Removed the redundant introductory WSL information banner and exposed previously clipped settings.
- Added WSL version information, update, and shutdown controls.
- Added an irreversible-data-loss confirmation before unregistering a distribution.

### Fixed

- Wait for elevated WSL installation to finish and report cancellation or a non-zero exit code instead of always reporting that installation started successfully.
- Install a new Ubuntu distribution without launching an interactive first-run shell inside the management workflow.
- Pass external command arguments structurally so spaces and quotes cannot change argument boundaries.
- Preserve non-Unicode operating-system arguments and report startup failures in the optional Rust WSL helper.

### CI and security

- Pin GitHub Actions to immutable commit hashes, move artifact handling to Node.js 24-based actions, restrict write access to the release job, validate tag/version consistency, and test both optional native helpers.
- Treat moderate, high, and critical NuGet vulnerability warnings as build errors.
- Publish SHA-256 checksums alongside release packages.
