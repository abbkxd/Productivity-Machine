namespace ProductiveMachine.WebApp.Services;

public interface ITwoFactorService
{
    // Generate a new secret key for a user
    string GenerateSecretKey();
    
    // Generate a QR code for the user to scan with their authenticator app
    byte[] GenerateQrCode(string secretKey, string email, string issuer = "ProductiveMachine");
    
    // Validate a token provided by the user
    bool ValidateToken(string secretKey, string token);
    
    // Enable 2FA for a user
    Task<bool> EnableTwoFactorAsync(string userId, string secretKey);
    
    // Disable 2FA for a user
    Task<bool> DisableTwoFactorAsync(string userId);
    
    // Check if 2FA is enabled for a user
    Task<bool> IsTwoFactorEnabledAsync(string userId);
} 