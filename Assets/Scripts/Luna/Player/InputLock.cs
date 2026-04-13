// Global input lock used across systems. Kept in the global namespace for simplicity.
public static class InputLock
{
    // When true, input-consuming systems should ignore user input (inventory, jump, etc.).
    public static bool Locked = false;
}
