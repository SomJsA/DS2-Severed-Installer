using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

internal static partial class InstallerEngine
{
    private const string FixedEnglishDat2Sha = "eb977e2f6f537b3d7d9b93ddaae928d6f35c45806bcf8496a2bea2319e1395ad";
    private const string FixedChineseDat2Sha = "6fbd88eb5ec906a80d1e578226f1e6923f69e1f34901a704e3fec6627fa38b0a";
    private static readonly Dictionary<uint, int> ContactBeamLabelOffsets = new Dictionary<uint, int>
    {
        { 0x040B61F4, 0xCBC12B }, { 0x13103924, 0xBA1F3F }, { 0x15A33D4B, 0xBE2ECF },
        { 0x177F0697, 0xBA2477 }, { 0x4CD4067E, 0xD41CCB }, { 0x723D4C35, 0xBCCDFF },
        { 0x78416FE9, 0xBE2DBB }, { 0x83D8BD12, 0xC38B57 }, { 0xDF248CA0, 0xBAE8B3 },
        { 0xE23DAB1A, 0xD2931F }, { 0xEDEC388B, 0xBA2A97 }
    };

    private static void FixStagedContactBeam(string path, string expectedHash)
    {
        // Offsets name the last letter of the state name inside each verified suit stream.
        // Renaming the DLC override makes the shared rifle bank supply the standing hold.
        byte[] name = Encoding.ASCII.GetBytes("pc_2h_aim_cbeam_firehold\0");
        var positions = new Dictionary<uint, long>();
        using (var file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            byte[] header = ReadExact(file, 16);
            uint count = ReadBe32(header, 8);
            long indexEnd = 16L + count * 12L;
            if (Encoding.ASCII.GetString(header, 0, 4) != "BIGH" ||
                indexEnd > ReadBe32(header, 12) || ReadBe32(header, 12) > file.Length)
                throw new InvalidDataException("Invalid DLC archive index.");

            for (uint i = 0; i < count; ++i)
            {
                byte[] entry = ReadExact(file, 12);
                uint key = ReadBe32(entry, 8);
                int relative;
                if (!ContactBeamLabelOffsets.TryGetValue(key, out relative)) continue;
                long start = ReadBe32(entry, 0);
                long size = ReadBe32(entry, 4);
                if (relative + 1 >= size || start < ReadBe32(header, 12) || start + size > file.Length)
                    throw new InvalidDataException("Invalid Contact Beam suit stream.");
                positions.Add(key, start + relative);
            }
            if (positions.Count != ContactBeamLabelOffsets.Count)
                throw new InvalidDataException("Missing Contact Beam suit streams.");

            foreach (long position in positions.Values)
            {
                file.Position = position - (name.Length - 2);
                if (!name.SequenceEqual(ReadExact(file, name.Length)))
                    throw new InvalidDataException("Contact Beam standstill state did not match.");
            }
            foreach (long position in positions.Values)
            {
                file.Position = position;
                file.WriteByte((byte)'x');
            }
        }
        if (Sha256(path) != expectedHash)
            throw new InvalidDataException("Contact Beam archive verification failed.");
    }
}
