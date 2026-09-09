namespace VoarVR.Flight
{
    // Bridges the character-select scene to the flight scene. A plain static field is enough:
    // it only needs to outlive one SceneManager.LoadScene call within a single play session.
    public static class CharacterSelection
    {
        public const string ResourcesFolder = "Characters";
        public const string DefaultCharacterName = "Duck";
        public static BirdCharacterDefinition Chosen;
    }
}
