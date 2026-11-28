using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

internal static partial class InstallerEngine
{
    private const string TiebaBeta01Dat0Sha = "e71468528a77581ace20dc597b4bc0189774a0221eada02927518cc062133d6e";
    private const string TiebaBeta011Dat0Sha = "987c3483790fc895158707b9662506ce24d5884292176db87f6e1e86bfd1ab15";

    private static bool IsLegacyDat0Hash(string hash)
    {
        return hash == TiebaBeta01Dat0Sha || hash == TiebaBeta011Dat0Sha;
    }

    private static bool IsDat0BaselineHash(string hash)
    {
        return hash == OriginalDat0Sha || IsLegacyDat0Hash(hash);
    }

    private static bool HasVerifiedDat0Backup(string game, string backup)
    {
        if (!File.Exists(backup)) return false;
        string hash = Sha256(backup);
        string recorded = StateValue(game, "dat0_backup_sha256");
        return hash == (recorded ?? OriginalDat0Sha) && IsDat0BaselineHash(hash);
    }

    private static Dictionary<uint, uint> ReadLegacyDat0EntrySizes()
    {
        // The beta packers recorded aligned lengths. Use the original payload lengths
        // so upgrading the old archive produces the same BIGH as a clean install.
        var sizes = new Dictionary<uint, uint>();
        using (var reader = new StringReader(ReadResourceText("DS2_LEGACY_DAT0_LAYOUT")))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] fields = line.Split(' ');
                sizes.Add(uint.Parse(fields[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                    uint.Parse(fields[1], CultureInfo.InvariantCulture));
            }
        }
        return sizes;
    }
}
