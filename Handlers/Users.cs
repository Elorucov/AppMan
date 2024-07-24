using appman.DataModels;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace appman;

public class Users {
    public static async Task<IResult> GetAsync(HttpContext context, ApplicationContext db, int userId = 0) {
        int current = AppMan.GetAuthenticatedUserId(context.Request.Headers.Authorization);
        if (current <= 0) return Results.Json(APIResponse<object>.GetError(current * -1));

        if (userId == 0) userId = current;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Results.Json(APIResponse<object>.GetError(14));
        return Results.Json(new APIResponse<User>(user));
    }

    public static async Task<IResult> CreateInviteAsync(HttpContext context, ApplicationContext db) {
        int current = AppMan.GetAuthenticatedUserId(context.Request.Headers.Authorization);
        if (current <= 0) return Results.Json(APIResponse<object>.GetError(current * -1));
        if (current != 1) return Results.Json(APIResponse<object>.GetError(13));

        string code = Cryptography.ComputeSHA256($"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}{current}");
        long creationTime = DateTimeOffset.Now.ToUnixTimeSeconds();

        await db.Invites.AddAsync(new Invite { 
            OwnerId = current,
            CreationTime = creationTime,
            Code = code,
            InvitedUserId = 0
        });
        await db.SaveChangesAsync();

        return Results.Json(new APIResponse<object>(new {
            CreationTime = creationTime,
            Code = code
        }));
    }

    public static async Task<IResult> GetInvitesAsync(HttpContext context, ApplicationContext db, int userId = 0) {
        int current = AppMan.GetAuthenticatedUserId(context.Request.Headers.Authorization);
        if (current <= 0) return Results.Json(APIResponse<object>.GetError(current * -1));
        if (current != 1) return Results.Json(APIResponse<object>.GetError(13));
        if (userId == 0) userId = current;

        var invites = await db.Invites.Where(i => i.OwnerId == userId).ToListAsync();
        APIList<Invite> result = new APIList<Invite>(invites, invites.Count);
        return Results.Json(new APIResponse<APIList<Invite>>(result));
    }

    public static async Task<IResult> RegisterNewAsync(HttpContext context, ApplicationContext db, string username, string password, string inviteCode) {
        var usernameRegex = new Regex(@"^\w+$", RegexOptions.Compiled);

        if (String.IsNullOrWhiteSpace(username)) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(username)} is missing"));
        if (username.Length < 2) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(username)} must be longer than 2 characters"));
        if (username.Length > 20) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(username)} must be shorter than 20 characters"));
        if (!usernameRegex.IsMatch(username)) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(username)} must be contains only letters, numbers and underscore"));
        
        if (String.IsNullOrWhiteSpace(password)) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(password)} is missing"));
        if (password.Length < 6) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(password)} must be longer than 6 symbols"));
        
        if (String.IsNullOrWhiteSpace(inviteCode)) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(inviteCode)} is missing"));

        var invite = await db.Invites.FirstOrDefaultAsync(i => i.Code == inviteCode);
        if (invite == null) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(inviteCode)} is invalid"));
        if (invite.InvitedUserId > 0) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(inviteCode)} is used"));

        var user = await db.Users.FirstOrDefaultAsync(i => i.Username == username);
        if (user != null) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(username)} is used"));

        var newUser = await db.AddAsync(new User {
            Username = username
        });
        await db.SaveChangesAsync();

        invite.InvitedUserId = newUser.Entity.Id;
        invite.InvitationTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        db.Invites.Update(invite);
        await db.SaveChangesAsync();

        return Results.Json(new APIResponse<object>(new {
            UserId = invite.InvitedUserId,
            Login = username,
            CreationTime = invite.InvitationTime,
        }));
    }
}