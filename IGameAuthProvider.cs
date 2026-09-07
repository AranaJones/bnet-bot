namespace BNetDiscordBridge.Bncs;

/// <summary>
/// The two pieces of BNCS login that are game/version-specific and can't be
/// hard-coded generically: CD key hashing, and the CheckRevision exe-version
/// challenge (proves you're running a real, current game client).
///
/// These are well-documented but intentionally NOT implemented here from
/// scratch — every working BNCS bot in the wild (PvPGN clients, bnetdocs
/// sample code, various Discord bridges) delegates this to BNCSutil
/// (https://github.com/HarpyWar/bncsutil or the original SF.net project),
/// which computes them from your actual game files. Wire an implementation
/// of this interface using BNCSutil (native lib + P/Invoke, or one of its
/// C#/.NET ports) for the game/version you're targeting, then pass it into
/// BncsClient. Trying to hand-roll the CheckRevision math without the game
/// files to validate against is a good way to get silently rejected by the
/// server with no useful error.
/// </summary>
public interface IGameAuthProvider
{
    /// <summary>4CC product code, e.g. "STAR" (StarCraft), "W2BN" (WC2 BNE),
    /// "D2DV"/"D2XP" (Diablo II), "WAR3"/"W3XP" (WC3).</summary>
    string ProductId { get; }

    /// <summary>Exe version DWORD reported in SID_AUTH_CHECK, as produced by
    /// the CheckRevision formula for this client's version.exe/CheckRevision.mpq.</summary>
    uint ExeVersion { get; }

    /// <summary>
    /// Runs CheckRevision against the server-supplied formula string /
    /// value strings (from SID_AUTH_INFO) and returns the resulting hash
    /// to submit in SID_AUTH_CHECK, plus the exe info string.
    /// </summary>
    (uint exeVersion, uint exeHash, string exeInfo) CheckRevision(
        string formula, string[] valueStrings, uint serverToken, uint clientToken);

    /// <summary>
    /// Decodes one CD key into the (keyLength, product, publicValue, hash)
    /// tuple SID_AUTH_CHECK expects, keyed off the given client/server tokens.
    /// </summary>
    (uint keyLength, uint product, uint publicValue, byte[] hash) HashCdKey(
        string cdKey, uint clientToken, uint serverToken);
}
