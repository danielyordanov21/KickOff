public class RegisterDto : AuthDto
{
    public new required string UserName { get; set; }
    public new required string Email { get; set; }
}