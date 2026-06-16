# Supporters & donations

Lodestone is **free, and always will be**. Donations are optional and **never gate functionality** —
supporters get cosmetic/convenience perks and our thanks. There is **no payment processor in the app**:
pledging happens on Patreon, and a redeemable code unlocks the perks offline.

## How it works (for users)

1. Pledge on the Patreon page (the **Support us** screen's buttons open it).
2. You receive a **supporter code**.
3. Paste it into **Support us → "Redeem code"**. The app verifies it offline and unlocks your perks.

### What a code unlocks (cosmetic / convenience only)

- A **Supporter badge** in the title bar (live today).
- **Extra accent themes** (entitlement: `SupporterService.CanUseExtraThemes`).
- An opt-in **beta update channel** (entitlement: `SupporterService.CanUseBetaChannel`).

Core mod-managing — install, update, compatibility checks, everything — is always free.

## How codes work (for the maintainer)

Codes are short signed tokens: `base64url(payload).base64url(signature)`. The payload is
`{"t":tier,"h":holder,"e":expiry?}`, signed with **ECDSA P-256 / SHA-256**. The app verifies them with
an **embedded public key** (`SupporterKeys.DefaultPublicKey`). Only the public key ships; the private
key stays with you. Because the perks are cosmetic, signature verification is sufficient — there is no
server and nothing secret in the client.

### One-time setup

```powershell
# Generate a key pair (do this once)
dotnet run --project src/Lodestone.Cli -- keygen
#  → PRIVATE=...   (keep secret — save under keys/, which is git-ignored)
#  → PUBLIC=...    (paste into src/Lodestone.Infrastructure/Supporter/SupporterKeys.cs)
```

### Issuing a code after a pledge

```powershell
dotnet run --project src/Lodestone.Cli -- issue `
  --key "@keys/supporter.private.b64" `
  --tier Supporter --holder "patron@example.com" [--expires 2027-01-01T00:00:00Z]
# → prints the code to send to the patron

# Sanity-check any code against the embedded public key:
dotnet run --project src/Lodestone.Cli -- verify --pub "@keys/supporter.public.b64" --code <code>
```

> ⚠️ **Never commit the private key.** `keys/` is git-ignored. If it ever leaks, run `keygen` again,
> replace `SupporterKeys.DefaultPublicKey`, and ship an update — old codes simply stop verifying.

## Configuring the Patreon link

Set your real Patreon URL in `DonateViewModel.PatreonUrl`
(`src/Lodestone.App/ViewModels/DonateViewModel.cs`).
