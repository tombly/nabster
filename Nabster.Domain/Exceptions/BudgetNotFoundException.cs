namespace Nabster.Domain.Exceptions;

public class BudgetNotFoundException : Exception
{
    public BudgetNotFoundException(string budgetName) : base($"Budget '{budgetName}' not found")
    {
    }
    
    public BudgetNotFoundException() : base("No budgets found")
    {
    }
}