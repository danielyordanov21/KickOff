public class RefreshToken
{
    public int Id { get; set; }

    public string Token { get; set; } = default!;

    public DateTime Expires { get; set; }

    public DateTime Created { get; set; } = DateTime.UtcNow;

    public string CreatedByIp { get; set; } = default!;

    public DateTime? Revoked { get; set; }

    public string? RevokedByIp { get; set; }

    public string? ReplacedByToken { get; set; }

    public string UserId { get; set; } = default!;
    public ApplicationUser User { get; set; } = default!;

    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsActive => Revoked == null && !IsExpired;
}
