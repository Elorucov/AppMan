using appman.DataModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Xml.Linq;

namespace appman;

public class AppMan {
    public static int GetAuthenticatedUserId(string authHeader) {
        if (String.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ")) return -1;
        string token = authHeader.Substring(7);
        int userId = Cryptography.CheckAccessToken(token);
        return userId;
    }

    public static async Task<List<Application>> GetUserAppsAsync(ApplicationContext db, int userId) {
        var ownedApps = await db.Applications.Where(a => a.OwnerId == userId).ToListAsync();
        var notOwnedAppsIds = await db.AppAccesses.Where(a => a.UserId == userId).Select(a => a.ApplicationId).ToListAsync();
        var notOwnedApps = await db.Applications.Where(a => notOwnedAppsIds.Contains(a.Id)).ToListAsync();
        var apps = ownedApps.Union(notOwnedApps).ToList();
        apps.Sort(delegate (Application x, Application y) {
            return x.Id.CompareTo(y.Id);
        });
        return apps;
    }

    // Public

    public static async Task<IResult> AuthAsync(ApplicationContext db, string login, string password) {
        User user = await db.Users.FirstOrDefaultAsync(u => u.Username == login);
        if (user != null) {
            string hash = Cryptography.ComputeSHA256(password);
            Credentials cred = await db.Credentials.FirstOrDefaultAsync(c => c.Id == user.Id && c.Password == hash);
            if (cred != null) {
                try {
                    string token = Cryptography.GenerateAccessToken(user.Id, hash);
                    return Results.Json(new APIResponse<AuthenticationResponse>(new AuthenticationResponse {
                        UserId = user.Id, AccessToken = token, ExpiresIn = 1800
                    }));
                } catch (Exception ex) {
                    return Results.Json(APIResponse<object>.GetError(4, ex.Message));
                }
            }
        }
        return Results.Json(APIResponse<object>.GetError(5));
    }

    public static async Task<IResult> GetUserAsync(HttpContext context, ApplicationContext db, int userId = 0) {
        int current = GetAuthenticatedUserId(context.Request.Headers.Authorization);
        if (current <= 0) return Results.Json(APIResponse<object>.GetError(current * -1));

        if (userId == 0) userId = current;
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Results.Json(APIResponse<object>.GetError(14));
        return Results.Json(new APIResponse<User>(user));
    }

    // Applications

    public static async Task<IResult> GetAppsAsync(HttpContext context, ApplicationContext db) {
        int current = GetAuthenticatedUserId(context.Request.Headers.Authorization);
        if (current <= 0) return Results.Json(APIResponse<object>.GetError(current * -1));

        var apps = await GetUserAppsAsync(db, current);

        APIList<Application> result = new APIList<Application>(apps, apps.Count);
        return Results.Json(new APIResponse<APIList<Application>>(result));
    }

    public static async Task<IResult> GetAppBranchesAsync(HttpContext context, ApplicationContext db, int appId = 0) {
        int current = GetAuthenticatedUserId(context.Request.Headers.Authorization);
        if (current <= 0) return Results.Json(APIResponse<object>.GetError(current * -1));

        if (appId <= 0) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(appId)} must be greater than 0"));
        var app = (await GetUserAppsAsync(db, current)).Where(a => a.Id == appId).FirstOrDefault();
        if (app == null) return Results.Json(APIResponse<object>.GetError(13));

        var branches = await db.Branches.Where(b => b.ApplicationId == appId).ToListAsync();
        APIList<AppBranch> result = new APIList<AppBranch>(branches, branches.Count);
        return Results.Json(new APIResponse<APIList<AppBranch>>(result));
    }

    public static async Task<IResult> CreateAppAsync(HttpContext context, ApplicationContext db, string name) {
        int current = GetAuthenticatedUserId(context.Request.Headers.Authorization);
        if (current <= 0) return Results.Json(APIResponse<object>.GetError(current * -1));

        if (String.IsNullOrWhiteSpace(name)) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(name)} is missing"));
        if (name.Length > 32) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(name)}'s length must be less or equal 32"));

        var app = await db.Applications.AddAsync(new Application {
            OwnerId = current,
            Name = name,
            PrivateKey = Cryptography.ComputeSHA256(Guid.NewGuid().ToString())
        });
        await db.SaveChangesAsync();

        await db.Branches.AddAsync(new AppBranch {
            ApplicationId = app.Entity.Id,
            Name = "release",
        });
        await db.SaveChangesAsync();
        
        return Results.Json(new APIResponse<Application>(app.Entity));
    }

    public static async Task<IResult> DeleteAppAsync(HttpContext context, ApplicationContext db, int id) {
        int current = GetAuthenticatedUserId(context.Request.Headers.Authorization);
        if (current <= 0) return Results.Json(APIResponse<object>.GetError(current * -1));

        if (id <= 0) return Results.Json(APIResponse<object>.GetError(12, $"{nameof(id)} must be greater than 0"));

        var app = await db.Applications.Where(a => a.Id == id).FirstOrDefaultAsync();
        var appac = await db.AppAccesses.Where(a => a.ApplicationId == id).ToListAsync();
        if (app == null && appac.Count == 0) return Results.Json(APIResponse<object>.GetError(14));

        db.Applications.Remove(app);
        foreach(var item in appac) {
            db.AppAccesses.Remove(item);
        }
        await db.SaveChangesAsync();

        return Results.Json(new APIResponse<bool>(true));
    }
}