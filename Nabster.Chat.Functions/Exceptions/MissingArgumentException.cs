namespace Nabster.Chat.Functions.Exceptions;

internal class MissingArgumentException(string name) : Exception($"Missing argument '{name}'")
{
}