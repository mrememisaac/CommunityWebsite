namespace CommunityWebsite.Core.Specifications;

/// <summary>
/// Specification pattern for encapsulating business rules
/// Follows DDD (Domain-Driven Design) principles
/// </summary>
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);
    string GetErrorMessage();
}
