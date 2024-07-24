using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace appman.DataModels;

public class APIError {
    public int Code { get; private set; }
    public string Message { get; private set; }

    internal APIError(int code, string message) {
        Code = code;
        Message = message;
    }

    internal static Dictionary<int, string> Errors = new Dictionary<int, string> {
        { 1, "Unauthorized" },
        { 2, "Invalid access token" },
        { 3, "Failed to check access token" },
        { 4, "Failed to generate access token" },
        { 5, "Invalid credentials" },
        //
        { 10, "Internal server error" },
        { 11, "Unknown method passed" },
        { 12, "One of the parameters specified was missing or invalid" },
        { 13, "Access denied" },
        { 14, "Not found" },
        { 15, "Not implemented yet" }
    };
}

public class APIResponse<T> {
    public T? Response { get; private set; }
    public APIError Error { get; private set; }

    public APIResponse(T resp) {
        Response = resp;
    }

    public APIResponse() {
        Response = default;
    }

    public static APIResponse<T> GetError(int code, string extra = null) {
        if (code == 0) code = 1;
        var err = APIError.Errors[code];
        if (!String.IsNullOrEmpty(extra)) err += $": {extra}";
        return new APIResponse<T> {
            Error = new APIError(code, err)
        };
    }
}

public class APIList<T> {
    public int Count { get; private set; }

    public List<T> Items { get; private set; }

    public APIList(List<T> items, int count) {
        Items = items;
        Count = count;
    }
}

public class AuthenticationResponse {
    public int UserId { get; init; }
    public string AccessToken { get; init; }
    public int ExpiresIn { get; init; }
}

[Index("Id", IsUnique = true)]
public class User {
    public int Id { get; private set; }

    [StringLength(20)]
    public string Username { get; set; }

    public static User GetSuperUser(string name) {
        return new User { Id = 1, Username = name };
    }
}

[Index("Id", IsUnique = true)]
public class Credentials {
    public int Id { get; set; }
    public string Password { get; set; }
}

[Index("Id", IsUnique = true)]
public class Invite {
    public int Id { get; private set; }
    public int OwnerId { get; set; }
    public long CreationTime { get; set; }

    [StringLength(64)]
    public string Code { get; set; }
    public int InvitedUserId { get; set; }
    public long InvitationTime { get; set; }
}

[Index("Id", IsUnique = true)]
public class Application {
    public int Id { get; private set; }
    public int OwnerId { get; set; }
    public string Name { get; set; }
    public string PrivateKey { get; set; }
}

[Index("Id", IsUnique = true)]
public class AppAccess {
    public int Id { get; private set; }
    public int ApplicationId { get; set; }
    public int UserId { get; set; }
    public int ScopeFlags { get; set; }
}

[Index("Id", IsUnique = true)]
public class AppBranch {
    public int Id { get; set; }
    [JsonIgnore]
    public int ApplicationId { get; set; }
    public string Name { get; set; }
}

[Index("Id", IsUnique = true)]
public class AppBuild {
    public int Id { get; private set; }
    public int ApplicationId { get; set; }
    public string Branch { get; set; }
    public int BuildNumber { get; set; }
    public string BinaryUrl { get; set; }
}

[Index("Id", IsUnique = true)]
public class CrashLog {
    public int Id { get; private set; }
    public int ApplicationId { get; set; }
    public string Branch { get; set; }
    public int Build { get; set; }
    public string OSName { get; set; }
    public string OSVersion { get; set; }
    public long Timestamp { get; set; }
    public int Code { get; set; }
    public string Info { get; set; }
}