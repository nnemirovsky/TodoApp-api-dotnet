using System.Security.Claims;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Helpers;

public static class HelperMethods
{
    public static User GetUserByClaims(ClaimsPrincipal user, AppDbContext dbContext) =>
        dbContext.Users.First(u => u.Email == user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
