public static class Level2NarratorFlags
{
    // Fake finish denendi mi? (çarpıp öldü / girdi -> artık TRUE)
    public static bool FakeFinishTried = false;

    // Parkour intro bir kere çaldı mı?
    public static bool RealParkourIntroPlayed = false;

    // İstersen level başında resetlemek için:
    public static void ResetAll()
    {
        FakeFinishTried = false;
        RealParkourIntroPlayed = false;
    }
}
