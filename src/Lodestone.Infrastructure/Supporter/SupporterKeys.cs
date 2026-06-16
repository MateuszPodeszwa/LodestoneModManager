namespace Lodestone.Infrastructure.Supporter;

/// <summary>
/// The embedded ECDSA P-256 public key used to verify supporter codes (base64 SPKI). Generate a key
/// pair with <see cref="SupporterCodeIssuer.GenerateKeyPair"/>, keep the private key secret, and paste
/// the public key here. While this is blank, the verifier reports codes as "not configured" and simply
/// no perks can be unlocked — core functionality is unaffected. See docs/SUPPORTERS.md.
/// </summary>
public static class SupporterKeys
{
    // ECDSA P-256 public key (SPKI, base64). The matching private key is kept secret by the
    // maintainer (keys/, git-ignored) and used by `lodestone issue` to mint codes. Safe to commit.
    public const string DefaultPublicKey =
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEIuvwsBO94NHMwXbse+TRTibyDfT5Z9XSRl+8ChQAbZnoom2TRJn8s2elR3Jb5jx7EMdquQgiwT5jtxxAi/JYvg==";
}
