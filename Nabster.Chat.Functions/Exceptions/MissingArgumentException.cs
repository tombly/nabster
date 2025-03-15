namespace Nabster.Chat.Functions.Exceptions;

public class MissingArgumentException(string name) : Exception($"Missing argument '{name}'")
{
}