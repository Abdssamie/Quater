using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Quater.Backend.Api.Infrastructure;

/// <summary>
/// Production-ready certificate loader supporting multiple sources:
/// - File system (PFX/PKCS12)
/// - Environment variables (Base64 encoded)
/// - Raw byte arrays
/// </summary>
public static class CertificateLoader
{
    /// <summary>
    /// Loads a certificate from configuration with multiple fallback strategies.
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    /// <param name="certificateType">Type of certificate (e.g., "Encryption", "Signing")</param>
    /// <param name="logger">Logger for diagnostics</param>
    /// <returns>Loaded X509Certificate2</returns>
    /// <exception cref="InvalidOperationException">When certificate cannot be loaded</exception>
    public static X509Certificate2 LoadCertificate(
        IConfiguration configuration, 
        string certificateType,
        ILogger logger)
    {
        var configPrefix = $"OpenIddict:{certificateType}Certificate";
        
        // Strategy 1: Load from environment variable (Base64 encoded PFX)
        var envVarName = $"{configPrefix}Base64";
        var base64Cert = configuration[envVarName];
        if (!string.IsNullOrEmpty(base64Cert))
        {
            logger.LogInformation("Loading {CertType} certificate from environment variable {EnvVar}", 
                certificateType, envVarName);
            return LoadFromBase64(base64Cert, configuration[$"{configPrefix}Password"]);
        }
        
        // Strategy 2: Load from file path
        var certPath = configuration[$"{configPrefix}Path"];
        if (!string.IsNullOrEmpty(certPath))
        {
            logger.LogInformation("Loading {CertType} certificate from file: {Path}", 
                certificateType, certPath);
            return LoadFromFile(certPath, configuration[$"{configPrefix}Password"]);
        }
        
        // Strategy 3: Load from thumbprint (Windows Certificate Store)
        var thumbprint = configuration[$"{configPrefix}Thumbprint"];
        if (!string.IsNullOrEmpty(thumbprint))
        {
            logger.LogInformation("Loading {CertType} certificate from store with thumbprint: {Thumbprint}", 
                certificateType, thumbprint);
            return LoadFromStore(thumbprint, configuration[$"{configPrefix}StoreName"], 
                configuration[$"{configPrefix}StoreLocation"]);
        }
        
        throw new InvalidOperationException(
            $"No valid certificate configuration found for {certificateType}. " +
            $"Please configure one of: {envVarName}, {configPrefix}Path, or {configPrefix}Thumbprint");
    }
    
    /// <summary>
    /// Loads certificate from a Base64-encoded PFX string.
    /// Ideal for Docker secrets, Kubernetes secrets, or environment variables.
    /// </summary>
    public static X509Certificate2 LoadFromBase64(string base64Data, string? password = null)
    {
        try
        {
            var certBytes = Convert.FromBase64String(base64Data);
            return LoadFromBytes(certBytes, password);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "Failed to decode Base64 certificate data. Ensure the certificate is properly Base64 encoded.", ex);
        }
    }
    
    /// <summary>
    /// Loads certificate from a file (PFX/PKCS12 format).
    /// </summary>
    public static X509Certificate2 LoadFromFile(string filePath, string? password = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Certificate file not found: {filePath}");
        }
        
        try
        {
            var certBytes = File.ReadAllBytes(filePath);
            return LoadFromBytes(certBytes, password);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            throw new InvalidOperationException(
                $"Failed to load certificate from file: {filePath}. " +
                "Ensure the file is a valid PFX/PKCS12 certificate and the password is correct.", ex);
        }
    }
    
    /// <summary>
    /// Loads certificate from raw bytes (PFX/PKCS12 format).
    /// Uses the modern X509CertificateLoader API (.NET 8+).
    /// </summary>
    public static X509Certificate2 LoadFromBytes(byte[] certBytes, string? password = null)
    {
        try
        {
            // Use modern X509CertificateLoader API (available in .NET 8+)
            // This is the recommended approach and avoids obsolete API warnings
            return X509CertificateLoader.LoadPkcs12(
                certBytes, 
                password,
                X509KeyStorageFlags.MachineKeySet | 
                X509KeyStorageFlags.PersistKeySet | 
                X509KeyStorageFlags.Exportable);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to load certificate from bytes. " +
                "Ensure the data is a valid PFX/PKCS12 certificate and the password is correct.", ex);
        }
    }
    
    /// <summary>
    /// Loads certificate from Windows Certificate Store by thumbprint.
    /// Useful for Windows Server deployments.
    /// </summary>
    public static X509Certificate2 LoadFromStore(
        string thumbprint, 
        string? storeName = null, 
        string? storeLocation = null)
    {
        var store = new X509Store(
            Enum.Parse<StoreName>(storeName ?? "My"),
            Enum.Parse<StoreLocation>(storeLocation ?? "CurrentUser"));
        
        try
        {
            store.Open(OpenFlags.ReadOnly);
            
            var certificates = store.Certificates
                .Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
            
            if (certificates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Certificate with thumbprint '{thumbprint}' not found in " +
                    $"{storeLocation ?? "CurrentUser"}\\{storeName ?? "My"} store.");
            }
            
            var certificate = certificates[0];
            
            if (!certificate.HasPrivateKey)
            {
                throw new InvalidOperationException(
                    $"Certificate with thumbprint '{thumbprint}' does not have a private key. " +
                    "OpenIddict requires certificates with private keys for signing and encryption.");
            }
            
            return certificate;
        }
        finally
        {
            store.Close();
        }
    }
    
    /// <summary>
    /// Validates that a certificate is suitable for OpenIddict usage.
    /// </summary>
    public static void ValidateCertificate(X509Certificate2 certificate, string certificateType, ILogger logger)
    {
        if (!certificate.HasPrivateKey)
        {
            throw new InvalidOperationException(
                $"{certificateType} certificate does not have a private key. " +
                "OpenIddict requires certificates with private keys.");
        }
        
        // Check expiration
        var now = DateTime.UtcNow;
        if (certificate.NotBefore > now)
        {
            throw new InvalidOperationException(
                $"{certificateType} certificate is not yet valid. " +
                $"Valid from: {certificate.NotBefore:yyyy-MM-dd HH:mm:ss} UTC");
        }
        
        if (certificate.NotAfter < now)
        {
            throw new InvalidOperationException(
                $"{certificateType} certificate has expired. " +
                $"Expired on: {certificate.NotAfter:yyyy-MM-dd HH:mm:ss} UTC");
        }
        
        // Warn if expiring soon (within 30 days)
        var daysUntilExpiry = (certificate.NotAfter - now).TotalDays;
        if (daysUntilExpiry < 30)
        {
            logger.LogWarning(
                "{CertType} certificate expires in {Days} days on {ExpiryDate}. Please renew soon.",
                certificateType, (int)daysUntilExpiry, certificate.NotAfter.ToString("yyyy-MM-dd"));
        }
        else
        {
            logger.LogInformation(
                "{CertType} certificate is valid until {ExpiryDate} ({Days} days remaining)",
                certificateType, certificate.NotAfter.ToString("yyyy-MM-dd"), (int)daysUntilExpiry);
        }
    }
}
