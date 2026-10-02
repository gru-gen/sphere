namespace Sphere.Ordering.Domain.Abstract;

// summary: a broken business rule. The API layer maps it to a 422 problem document.
public sealed class DomainException(string message) : Exception(message);
