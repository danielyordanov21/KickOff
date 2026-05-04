namespace KickOffAPI.DTOs
{
    public class ConfirmAccountActionDto
    {
        public required string CurrentPassword { get; set; }
        public required string ConfirmationText { get; set; }
    }
}
