
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shagun.DBRepo;
using Shagun.Models;
using Shagun.Services.Interfaces.IUserService;

namespace Shagun.Services.UserService
{ 
    public class UserService : IUserService
    {

    private readonly ApplicationDbContext _context;
    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }
     public async  Task<User?> GetUserByPhone(string phone)
      { 
         var user =  await _context.Users.FirstOrDefaultAsync(u=>u.Phone==phone);
        return user; 
        }    
     public   Task<User?> GetUserAdmin(string phone)
     { 
         var user = _context.Users.FirstOrDefault(u=>u.Phone==phone);
        return Task.FromResult<User?>(user); 
        }
    public Task<List<User>> GetUsers()
        {
                var users = _context.Users.ToList();
                return Task.FromResult(users);
        }
      public  Task<User> CreateUser(User user)
        { return Task.FromResult<User>(user); }
    }
}