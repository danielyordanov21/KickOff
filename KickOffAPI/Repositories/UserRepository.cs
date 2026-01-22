public class UserRepository : BaseRepository<User>
{
    public UserRepository(UserDbContext context) : base(context)
    {
    }
}