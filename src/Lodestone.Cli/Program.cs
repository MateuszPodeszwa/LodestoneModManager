// Lodestone headless CLI — a thin surface over the same core the WPF app uses. Today it hosts the
// supporter-code tooling the maintainer runs offline (key generation, issuing, verifying), proving
// the core is fully usable without any UI.

using Lodestone.Domain.Common;
using Lodestone.Infrastructure.Supporter;

return Run(args);

static int Run(string[] args)
{
    if (args.Length == 0)
    {
        PrintUsage();
        return 1;
    }

    return args[0].ToLowerInvariant() switch
    {
        "keygen" => KeyGen(),
        "issue" => Issue(args),
        "verify" => Verify(args),
        _ => PrintUsage(),
    };
}

static int KeyGen()
{
    (string priv, string pub) = SupporterCodeIssuer.GenerateKeyPair();
    Console.WriteLine("PRIVATE=" + priv);
    Console.WriteLine("PUBLIC=" + pub);
    Console.WriteLine();
    Console.WriteLine("Keep PRIVATE secret (e.g. keys/supporter.private.b64, git-ignored).");
    Console.WriteLine("Paste PUBLIC into SupporterKeys.DefaultPublicKey.");
    return 0;
}

static int Issue(string[] args)
{
    string? key = ResolveKey(GetOption(args, "--key"));
    string? tier = GetOption(args, "--tier");
    string? holder = GetOption(args, "--holder");
    string? expires = GetOption(args, "--expires");

    if (key is null || string.IsNullOrWhiteSpace(tier) || string.IsNullOrWhiteSpace(holder))
    {
        Console.Error.WriteLine("usage: lodestone issue --key <base64|@file> --tier <Tier> --holder <name> [--expires <ISO-8601>]");
        return 1;
    }

    DateTimeOffset? expiry = string.IsNullOrWhiteSpace(expires)
        ? null
        : DateTimeOffset.Parse(expires, System.Globalization.CultureInfo.InvariantCulture);

    Console.WriteLine(SupporterCodeIssuer.Issue(key, tier!, holder!, expiry));
    return 0;
}

static int Verify(string[] args)
{
    string? pub = ResolveKey(GetOption(args, "--pub"));
    string? code = GetOption(args, "--code");
    if (pub is null || string.IsNullOrWhiteSpace(code))
    {
        Console.Error.WriteLine("usage: lodestone verify --pub <base64|@file> --code <code>");
        return 1;
    }

    Result<Lodestone.Application.Supporter.SupporterEntitlement> result = new SignedSupporterCodeVerifier(pub).Verify(code!);
    if (result.IsSuccess)
    {
        Console.WriteLine($"VALID  tier={result.Value.Tier}  holder={result.Value.Holder}  expires={result.Value.Expires?.ToString("u") ?? "never"}");
        return 0;
    }

    Console.WriteLine("INVALID  " + result.Error.Message);
    return 2;
}

static int PrintUsage()
{
    Console.WriteLine("Lodestone CLI");
    Console.WriteLine("  keygen                                   generate an ECDSA key pair");
    Console.WriteLine("  issue  --key <b64|@file> --tier <T> --holder <name> [--expires <ISO>]");
    Console.WriteLine("  verify --pub <b64|@file> --code <code>");
    return 0;
}

static string? GetOption(string[] args, string name)
{
    int i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

// Accepts a literal base64 key or "@path" to read it from a file.
static string? ResolveKey(string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return null;
    }

    return value.StartsWith('@') ? File.ReadAllText(value[1..]).Trim() : value;
}
