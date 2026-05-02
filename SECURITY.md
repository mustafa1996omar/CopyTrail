# Security Policy

## Supported Versions

Only the latest release of CopyTrail receives security fixes.

| Version | Supported |
|---------|-----------|
| 1.0.x   | ✅ Yes     |

---

## Reporting a Vulnerability

If you discover a security vulnerability in CopyTrail, please **do not open a public GitHub issue**.

Instead, report it privately by emailing the repository owner (see the GitHub profile for contact details) or by using GitHub's [private security advisory](../../security/advisories/new) feature.

Please include:
- A description of the vulnerability
- Steps to reproduce it
- The version of CopyTrail you tested against
- Any relevant logs or screenshots (redact any sensitive clipboard content)

We will acknowledge your report within 5 business days and aim to issue a fix within 30 days for confirmed vulnerabilities.

---

## Scope

CopyTrail is a local Windows desktop application. It does not make network calls, has no server-side component, and does not transmit any data externally. The primary security concerns are:

- **Local data access** — clipboard history is stored in SQLite at `%LOCALAPPDATA%\CopyTrail\CopyTrail.db`. This file is readable by any process running as the same Windows user.
- **Process injection** — CopyTrail listens to `WM_CLIPBOARDUPDATE` and has write access to the Windows clipboard. A malicious process could potentially send crafted clipboard data.
- **Registry write** — CopyTrail optionally writes to `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` for startup.

If you find an issue in any of these areas, please report it privately as described above.

---

## Out of Scope

The following are known limitations, not vulnerabilities:

- Clipboard content is visible to any process running as the same user — this is normal Windows behavior and not a CopyTrail-specific issue.
- The SQLite database is not encrypted — by design, history is local and user-accessible.
