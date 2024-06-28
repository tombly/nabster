namespace Nabster.Domain.Exceptions;

public class MissingArgumentException(string name) : Exception($"Missing argument '{name}'")
{
}