namespace BNetDiscordBridge.Bncs;

/// <summary>
/// BNLS-based implementation of IGameAuthProvider.
/// Uses the BNLS server to handle all authentication details.
/// </summary>
public sealed class BnlsGameAuthProvider : IGameAuthProvider
{
    private readonly BnlsClient _bnls;

    public string ProductId { get; }

    public uint ExeVersion { get; private set; }

    public BnlsGameAuthProvider(string productId, BnlsClient bnls)
    {
        ProductId = productId;
        _bnls = bnls;
    }

    public (uint exeVersion, uint exeHash, string exeInfo) CheckRevision(
        string formula, string[] valueStrings, uint serverToken, uint clientToken)
    {
        var task = _bnls.VersionCheckAsync(
            ProductId, "Game.exe", 0, formula, clientToken, serverToken, CancellationToken.None);
        task.Wait();

        var (exeVersion, exeHash, exeInfo) = task.Result;
        ExeVersion = exeVersion;
        return (exeVersion, exeHash, exeInfo);
    }

    public (uint keyLength, uint product, uint publicValue, byte[] hash) HashCdKey(
        string cdKey, uint clientToken, uint serverToken)
    {
        var task = _bnls.HashCdKeyAsync(cdKey, clientToken, serverToken, ProductId, CancellationToken.None);
        task.Wait();
        return task.Result;
    }
}