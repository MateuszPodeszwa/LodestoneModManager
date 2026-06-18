using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lodestone.Application.Supporter;
using Lodestone.Domain.Common;

namespace Lodestone.Infrastructure.Supporter;

/// <summary>
/// The signed body of a supporter code. <c>iat</c> (issued-at, UTC unix seconds) drives the app-side
/// 1-hour activation window; <c>k</c> is the kind (always "supporter" for now - all patrons are equal).
/// </summary>
internal sealed record SupporterCodePayload(
    [property: JsonPropertyName("v")] int Version,
    [property: JsonPropertyName("k")] string? Kind,
    [property: JsonPropertyName("h")] string Holder,
    [property: JsonPropertyName("iat")] long IssuedAtUnix);

/// <summary>
/// Verifies offline supporter codes of the form <c>base64url(payload).base64url(signature)</c> using
/// an embedded ECDSA P-256 public key. No payment processing or network call is involved; the private
/// key never ships. Verification only checks the signature and decodes the claims - the 1-hour
/// activation window is enforced by <see cref="SupporterService"/> so the policy can't be smuggled in
/// (or extended) by a leaked code.
/// </summary>
public sealed class SignedSupporterCodeVerifier : ISupporterCodeVerifier
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.General);

    private readonly string _publicKeyBase64;

    public SignedSupporterCodeVerifier(string publicKeyBase64) => _publicKeyBase64 = publicKeyBase64;

    public Result<SupporterCode> Verify(string code)
    {
        if (string.IsNullOrWhiteSpace(_publicKeyBase64))
        {
            return Result.Failure<SupporterCode>("supporter.unavailable",
                "Supporter codes aren't configured in this build.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure<SupporterCode>("supporter.invalid", "Enter your supporter code.");
        }

        string[] parts = code.Trim().Split('.');
        if (parts.Length != 2)
        {
            return Result.Failure<SupporterCode>("supporter.invalid", "That code doesn't look right.");
        }

        byte[] payload;
        byte[] signature;
        try
        {
            payload = Base64Url.DecodeFromChars(parts[0]);
            signature = Base64Url.DecodeFromChars(parts[1]);
        }
        catch (FormatException)
        {
            return Result.Failure<SupporterCode>("supporter.invalid", "That code doesn't look right.");
        }

        try
        {
            using ECDsa ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(_publicKeyBase64), out _);
            if (!ecdsa.VerifyData(payload, signature, HashAlgorithmName.SHA256))
            {
                return Result.Failure<SupporterCode>("supporter.invalid", "This code couldn't be verified.");
            }
        }
        catch (CryptographicException)
        {
            return Result.Failure<SupporterCode>("supporter.invalid", "This code couldn't be verified.");
        }

        try
        {
            SupporterCodePayload? body = JsonSerializer.Deserialize<SupporterCodePayload>(payload, PayloadOptions);
            if (body is null || body.IssuedAtUnix <= 0)
            {
                return Result.Failure<SupporterCode>("supporter.invalid", "This code is missing its issue time.");
            }

            string holder = string.IsNullOrWhiteSpace(body.Holder) ? "Supporter" : body.Holder;
            DateTimeOffset issuedAt = DateTimeOffset.FromUnixTimeSeconds(body.IssuedAtUnix);
            return Result.Success(new SupporterCode(holder, issuedAt));
        }
        catch (JsonException)
        {
            return Result.Failure<SupporterCode>("supporter.invalid", "This code couldn't be read.");
        }
    }
}

/// <summary>
/// Issues supporter codes. Run by the maintainer (offline) with the private key - e.g. via the
/// <c>lodestone</c> CLI after a Patreon pledge. Never bundled with a shipped build.
/// </summary>
public static class SupporterCodeIssuer
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.General);

    /// <summary>Generates a new ECDSA P-256 key pair as base64 (PKCS#8 private, SPKI public).</summary>
    public static (string PrivateKeyBase64, string PublicKeyBase64) GenerateKeyPair()
    {
        using ECDsa ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return (
            Convert.ToBase64String(ecdsa.ExportPkcs8PrivateKey()),
            Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo()));
    }

    /// <summary>Signs and encodes a redeemable code for a patron. <paramref name="issuedAt"/> defaults to
    /// now; the app accepts the code for one hour after it.</summary>
    public static string Issue(string privateKeyBase64, string holder, DateTimeOffset? issuedAt = null)
    {
        long iat = (issuedAt ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(
            new SupporterCodePayload(Version: 1, Kind: "supporter", holder, iat), PayloadOptions);

        using ECDsa ecdsa = ECDsa.Create();
        ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyBase64), out _);
        byte[] signature = ecdsa.SignData(payload, HashAlgorithmName.SHA256);

        return $"{Base64Url.EncodeToString(payload)}.{Base64Url.EncodeToString(signature)}";
    }
}
