internal static partial class InstallerEngine
{
    private const string FontCandidate6Dat0Sha="3311f5c137f390b6b01435403d48ba5b5faa2d2d893a54069322b55dea9a67d8";
    private const string FontCandidate5Dat0Sha="d899a1756d6023ba1c97747c65cfecdc532a2744f3eb5570deacaf4b2e896942";
    private const string FontCandidate4Dat0Sha="5bedaaf5f14c4ad526dc2926b45a0f9fb1e0a1d790aa1e3e398bb12866d1afbd";
    private const string FontCandidate3Dat0Sha="36bd541560b42508672804dfa3e4fe0878c600b626249bbe1bf7d93ea533512e";
    private const string FontCandidate2Dat0Sha="e964e79ec6fbdb6914b1aefcfd58790fcac237d0290bcb21b0a0e9a709df5dc4";
    private const string FontCandidate1Dat0Sha="a0d682520fe71734ec110c9909c50da4409f54ee35c47ffecb43bea6300be406";
    private static bool IsKnownPatchedDat0Hash(string hash){return hash==PatchedDat0Sha || hash=="ba9d93daf7e926e17c93b4827d27c89c60a79d7f7f02421c29a5299208f5937e" || hash=="45786bec8ef72d41b587895c5b3ce9b49805bbf2cefdab4ca953e25702f39a18" || hash==FontCandidate6Dat0Sha || hash==FontCandidate5Dat0Sha || hash==FontCandidate4Dat0Sha || hash==FontCandidate3Dat0Sha || hash==FontCandidate2Dat0Sha || hash==FontCandidate1Dat0Sha || hash=="e9a5a0d212c1848009543ace7876a745d39f498b4c514673bd02df123c182583";}
}
