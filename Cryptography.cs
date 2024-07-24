using Branca;
using System.Security.Cryptography;
using System.Text;

namespace appman;

public static class Cryptography {
    public static string ComputeSHA256(string s) {
        string hash = string.Empty;

        using (SHA256 sha256 = SHA256.Create()) {
            byte[] hashValue = sha256.ComputeHash(Encoding.UTF8.GetBytes(s));

            foreach (byte b in hashValue) hash += $"{b:x2}";
        }

        return hash;
    }


    static BrancaService branca = null;

    private static void CheckBrancaService() {
        if (branca == null) {
            byte[] b = Encoding.UTF8.GetBytes(Program.Setting["TokenGenKey"]);
            branca = new BrancaService(b, new BrancaSettings {
                MaxStackLimit = 1024,
                TokenLifetimeInSeconds = 43200
            });
        }
    }

    public static string GenerateAccessToken(int userId, string salt) {
        CheckBrancaService();
        return branca.Encode(userId.ToString() + "\n" + salt);
    }

    public static int CheckAccessToken(string token) {
        CheckBrancaService();
        try {
            if (branca.TryDecode(token, out byte[] payload)) {
                string data = Encoding.UTF8.GetString(payload);
                return Convert.ToInt32(data.Split("\n")[0]);
            }
        } catch (Exception ex) {
            return -3;
        }
        return -2;
    }
}