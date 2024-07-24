using appman.DataModels;
using Microsoft.EntityFrameworkCore;

namespace appman;

public class Auth {
    public static async Task<IResult> GetAccessTokenAsync(ApplicationContext db, string login, string password) {
        User user = await db.Users.FirstOrDefaultAsync(u => u.Username == login);
        if (user != null) {
            string hash = Cryptography.ComputeSHA256(password);
            Credentials cred = await db.Credentials.FirstOrDefaultAsync(c => c.Id == user.Id && c.Password == hash);
            if (cred != null) {
                try {
                    string token = Cryptography.GenerateAccessToken(user.Id, hash);
                    return Results.Json(new APIResponse<AuthenticationResponse>(new AuthenticationResponse {
                        UserId = user.Id, AccessToken = token, ExpiresIn = 43200
                    }));
                } catch (Exception ex) {
                    return Results.Json(APIResponse<object>.GetError(4, ex.Message));
                }
            }
        }
        return Results.Json(APIResponse<object>.GetError(5));
    }
}