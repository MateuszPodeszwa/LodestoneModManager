# Security Policy

## Supported versions

Lodestone is pre-1.0, so only the latest release gets security fixes.

## Reporting a vulnerability

Please don't open a public issue for a security vulnerability.

Report it privately through GitHub's [Report a vulnerability](../../security/advisories/new) form, or
email podinatubie@gmail.com with the details and steps to reproduce. I'll acknowledge within a few
days and work out a fix and a disclosure timeline with you.

## Scope notes

- Supporter codes are verified offline with ECDSA. The app embeds only the public key; the private
  signing key never ships and isn't in this repository, so a leaked public key doesn't let anyone
  forge codes. If you think the private key has been exposed, treat it as critical and report it
  privately.
- Mod downloads are checked against the SHA-512 published by the source (Modrinth) before anything
  lands in the game folder.
- Mod descriptions render in a WebView2 with JavaScript disabled and a strict Content-Security-Policy.
  Reports of a CSP bypass or script execution from an untrusted description are in scope.

Thanks for helping keep Lodestone and its users safe.
