// ── Supporter-code signing ───────────────────────────────────────────────
// Produces codes the Lodestone app accepts. Must match, byte-for-byte, the
// scheme in src/Lodestone.Infrastructure/Supporter/SignedSupporterCode.cs:
//
//   code     = base64url(payload) "." base64url(signature)     (no '=' padding)
//   payload  = utf8 JSON  {"v":1,"k":"supporter","h":<holder>,"iat":<unix-secs>}
//   signature= ECDSA P-256 over the *payload bytes*, SHA-256, IEEE-P1363 (raw r‖s)
//
// .NET's ECDsa.SignData defaults to IEEE-P1363, so Node MUST set
// `dsaEncoding: 'ieee-p1363'` (its default is DER - which the app would reject).
import { createPrivateKey, createPublicKey, sign as nodeSign, verify as nodeVerify } from 'node:crypto'

function b64urlEncode(buf: Buffer): string {
  return buf.toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

function b64urlToBuffer(s: string): Buffer {
  const b64 = s.replace(/-/g, '+').replace(/_/g, '/')
  const pad = b64.length % 4 === 0 ? '' : '='.repeat(4 - (b64.length % 4))
  return Buffer.from(b64 + pad, 'base64')
}

export interface IssuedCode {
  code: string
  iat: number
  /** Last instant the app will still redeem this code (iat + 1 hour). */
  expiresAt: Date
}

/**
 * Sign a redeemable supporter code for `holder`. `issuedAt` defaults to now; the
 * app accepts the code for one hour after it.
 */
export function issueSupporterCode(privateKeyB64: string, holder: string, issuedAt: Date = new Date()): IssuedCode {
  if (!privateKeyB64) {
    throw new Error('Supporter signing key is not configured (NUXT_SUPPORTER_PRIVATE_KEY_B64).')
  }
  const iat = Math.floor(issuedAt.getTime() / 1000)
  // Key order matters only for byte-identity; the app deserializes by name.
  const payload = Buffer.from(JSON.stringify({ v: 1, k: 'supporter', h: holder, iat }), 'utf8')

  const key = createPrivateKey({ key: Buffer.from(privateKeyB64, 'base64'), format: 'der', type: 'pkcs8' })
  const signature = nodeSign('sha256', payload, { key, dsaEncoding: 'ieee-p1363' })

  return {
    code: `${b64urlEncode(payload)}.${b64urlEncode(signature)}`,
    iat,
    expiresAt: new Date((iat + 3600) * 1000),
  }
}

/** Self-check a code against the embedded public key (used as a boot sanity test). */
export function verifySupporterCode(publicKeyB64: string, code: string): boolean {
  try {
    const [p, s] = code.split('.')
    if (!p || !s) return false
    const payload = b64urlToBuffer(p)
    const signature = b64urlToBuffer(s)
    const key = createPublicKey({ key: Buffer.from(publicKeyB64, 'base64'), format: 'der', type: 'spki' })
    return nodeVerify('sha256', payload, { key, dsaEncoding: 'ieee-p1363' }, signature)
  } catch {
    return false
  }
}
