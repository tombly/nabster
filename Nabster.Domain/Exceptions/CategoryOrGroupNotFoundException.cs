namespace Nabster.Domain.Exceptions;

public class CategoryOrGroupNotFoundException(string name) : Exception($"Category or group '{name}' not found")
{
}