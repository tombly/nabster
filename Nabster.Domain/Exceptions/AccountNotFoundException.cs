namespace Nabster.Domain.Exceptions;

public class AccountNotFoundException(string name) : Exception($"Account '{name}' not found")
{
}