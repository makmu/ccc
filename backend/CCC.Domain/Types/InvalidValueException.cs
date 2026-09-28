namespace CCC.Domain.Types;

// Thrown when input is malformed, so a value object can't be constructed. The
// API turns it into a 400 before any command runs.
public class InvalidValueException(string message) : Exception(message);
