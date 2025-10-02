using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MedicalAI.Core.Security;

namespace MedicalAI.Infrastructure.Security
{
    public class SecurityService : ISecurityService
    {
        private readonly ILogger<SecurityService> _logger;
        private readonly Dictionary<string, byte[]> _encryptionKeys;

        public SecurityService(ILogger<SecurityService> logger)
        {
            _logger = logger;
            _encryptionKeys = new Dictionary<string, byte[]>();
            
            // Initialize with a default key (in production, use proper key management)
            _encryptionKeys["default"] = GenerateKey();
        }

        public async Task<FileValidationResult> ValidateFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating file security: {FilePath}", filePath);

            var issues = new List<string>();
            bool isValid = true;
            bool isSafe = true;

            try
            {
                var fileInfo = new FileInfo(filePath);
                
                // Check file existence
                if (!fileInfo.Exists)
                {
                    issues.Add("File does not exist");
                    isValid = false;
                    return new FileValidationResult(false, false, issues, string.Empty);
                }

                // Check file size (prevent extremely large files that could cause DoS)
                const long maxFileSize = 2L * 1024 * 1024 * 1024; // 2GB limit
                if (fileInfo.Length > maxFileSize)
                {
                    issues.Add($"File size exceeds maximum allowed size ({maxFileSize} bytes)");
                    isSafe = false;
                }

                // Check file extension
                var allowedExtensions = new[] { ".dcm", ".nii", ".nii.gz", ".bin", ".dat" };
                if (!allowedExtensions.Contains(fileInfo.Extension.ToLowerInvariant()))
                {
                    issues.Add($"File extension '{fileInfo.Extension}' is not allowed");
                    isSafe = false;
                }

                // Generate file hash for integrity
                var fileHash = await GenerateFileHashAsync(filePath, cancellationToken);

                // Basic malware scan (check for suspicious patterns)
                await PerformBasicMalwareScanAsync(filePath, issues, cancellationToken);

                if (issues.Any(i => i.Contains("malware") || i.Contains("suspicious")))
                {
                    isSafe = false;
                }

                _logger.LogInformation("File validation completed. Valid: {IsValid}, Safe: {IsSafe}, Issues: {IssueCount}", 
                    isValid, isSafe, issues.Count);

                return new FileValidationResult(isValid, isSafe, issues, fileHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating file: {FilePath}", filePath);
                issues.Add($"Validation error: {ex.Message}");
                return new FileValidationResult(false, false, issues, string.Empty);
            }
        }

        public async Task<byte[]> EncryptDataAsync(byte[] data, string keyId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Encrypting data with key: {KeyId}", keyId);

            if (!_encryptionKeys.TryGetValue(keyId, out var key))
            {
                throw new ArgumentException($"Encryption key '{keyId}' not found");
            }

            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            
            // Write IV to the beginning of the stream
            await msEncrypt.WriteAsync(aes.IV, 0, aes.IV.Length, cancellationToken);
            
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                await csEncrypt.WriteAsync(data, 0, data.Length, cancellationToken);
            }

            return msEncrypt.ToArray();
        }

        public async Task<byte[]> DecryptDataAsync(byte[] encryptedData, string keyId, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Decrypting data with key: {KeyId}", keyId);

            if (!_encryptionKeys.TryGetValue(keyId, out var key))
            {
                throw new ArgumentException($"Encryption key '{keyId}' not found");
            }

            using var aes = Aes.Create();
            aes.Key = key;

            using var msDecrypt = new MemoryStream(encryptedData);
            
            // Read IV from the beginning of the stream
            var iv = new byte[aes.IV.Length];
            await msDecrypt.ReadAsync(iv, 0, iv.Length, cancellationToken);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var msPlain = new MemoryStream();
            
            await csDecrypt.CopyToAsync(msPlain, cancellationToken);
            return msPlain.ToArray();
        }

        public string GenerateSecureHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(data);
            return Convert.ToHexString(hash);
        }

        public bool VerifyDataIntegrity(byte[] data, string expectedHash)
        {
            var actualHash = GenerateSecureHash(data);
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        private async Task<string> GenerateFileHashAsync(string filePath, CancellationToken cancellationToken)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
            return Convert.ToHexString(hash);
        }

        private async Task PerformBasicMalwareScanAsync(string filePath, List<string> issues, CancellationToken cancellationToken)
        {
            try
            {
                // Read first 1KB of file to check for suspicious patterns
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var buffer = new byte[Math.Min(1024, stream.Length)];
                await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                var content = Encoding.ASCII.GetString(buffer);

                // Check for suspicious patterns (basic heuristics)
                var suspiciousPatterns = new[]
                {
                    "eval(",
                    "exec(",
                    "system(",
                    "shell_exec",
                    "cmd.exe",
                    "powershell.exe"
                };

                foreach (var pattern in suspiciousPatterns)
                {
                    if (content.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        issues.Add($"Suspicious pattern detected: {pattern}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not perform malware scan on file: {FilePath}", filePath);
                issues.Add("Could not complete security scan");
            }
        }

        private static byte[] GenerateKey()
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            return aes.Key;
        }
    }
}