namespace TailorSpadi.Application.Abstractions;

/// <summary>Identity supplied by authenticated server context in Module 2.</summary>
public interface ICurrentUser
{
    string? UserId { get; }
    bool IsAuthenticated { get; }
}
