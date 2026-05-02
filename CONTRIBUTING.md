# Contributing to CopyTrail

Thank you for your interest in CopyTrail! This guide explains how to get set up and contribute.

---

## Prerequisites

- Windows 10 or later
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- VS Code (recommended) or any editor that supports C#

---

## Getting Started

1. **Fork the repository** on GitHub, then clone your fork:

   ```
   git clone https://github.com/<your-username>/CopyTrail.git
   cd CopyTrail
   ```

2. **Build the project:**

   ```
   dotnet build CopyTrail.sln
   ```

3. **Run the app:**

   ```
   dotnet run --project src/CopyTrail/CopyTrail.csproj
   ```

4. **Run tests:**

   ```
   dotnet test CopyTrail.sln
   ```

---

## Project Layout

```
src/CopyTrail/          Main WPF application
tests/CopyTrail.Tests/  Unit tests (xUnit)
docs/ai/                AI agent working docs (not for contributors)
```

---

## How to Contribute

### Bug fixes and small improvements

1. Open an issue describing the bug or improvement.
2. Fork and create a fix on your fork.
3. Make sure `dotnet build` and `dotnet test` both pass with 0 errors.
4. Open a pull request against `main`.

### New features

Please open an issue first to discuss the feature before writing code. This avoids wasted effort if the feature is out of scope for V1.

**Features that are permanently out of scope for V1:**
- Cloud sync or backup
- User accounts
- Telemetry or analytics
- AI or ML features
- Browser extensions
- Network calls of any kind
- Windows + V interception

---

## Code Style

- C# with nullable reference types enabled.
- Follow the existing patterns in `Services/`, `ViewModels/`, and `Models/`.
- Do not leave `TODO` comments or empty method bodies in merged code.
- All public-facing logic must have unit tests.

---

## Running a Release Build

```
dotnet publish src/CopyTrail/CopyTrail.csproj -c Release -r win-x64 --self-contained true -o publish/
```

See [README.md](README.md) for the full release checklist.

---

## Reporting Security Issues

Please read [SECURITY.md](SECURITY.md) before reporting security vulnerabilities.
