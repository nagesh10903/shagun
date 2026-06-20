using Shagun.Models;
namespace Shagun.Services.Interfaces.IUserService
{
    public interface IUserService
    {
      public  Task<User?> GetUserByPhone(string phone);
      public  Task<User?> GetUserAdmin(string phone);
     public   Task<List<User>> GetUsers();
      public  Task<User> CreateUser(User user);
    }
}
