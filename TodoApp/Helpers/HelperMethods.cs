using System.Security.Claims;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Helpers;

public class HelperMethods
{
    public static User GetUserByClaims(ClaimsPrincipal user, AppDbContext context) =>
        context.Users.First(u => u.Email == user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}
