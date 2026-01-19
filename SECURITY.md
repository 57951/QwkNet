# Security Policy

## Supported versions
Security updates apply to the latest released version only.

## Reporting a vulnerability
Please report security issues privately.

Include:
- A clear description of the issue.
- Steps to reproduce.
- Impact assessment.
- Any suggested fix, if you have one.

Do not open a public issue for suspected vulnerabilities.

---

## File Sizes
Typical QWK packet sizes are heavily influenced by the 128-byte record limitations.

### Average QWK Packet Size
Most QWK packets range between 10KB and 500KB (0.01MB – 0.5MB). Even though modern systems can handle much larger files, the historical and architectural design for dial-up speeds kept packets well under 1MB for efficient transfer.

### Largest QWK Packet Size
While there is no strict internal hard limit on the maximum size of a QWK packet beyond the filesystem limits of the era (e.g., FAT16's 2GB), individual component files like `MESSAGES.DAT` use ASCII-based record counting. Practical maximums are often capped by the mail door or offline mail reader software at around 16MB to 32MB to prevent processing crashes and extreme download times.

### Average Message Size in a QWK Packet
A typical message is roughly 1KB to 3KB (0.001MB – 0.003MB). The format stores messages in fixed-length 128-byte records; the first record being the header, and subsequent records hold the text body. Most messages span approximately 8 to 24 of these records.

---

## Security Advice for New Developers
For applications processing untrusted input, consider explicit limits:

```csharp
// Without entry size control, defaults to maximum size of 100MB for archives and 16MB for messages.
using QwkPacket packet = QwkPacket.Open("packet.qwk", 
  ValidationMode.Lenient, // Use Lenient for compatibility, Strict for untrusted input
  maxMessageSizeMB: 1);

// With explicit entry size control
using QwkPacket packet = QwkPacket.Open("packet.qwk",
  ValidationMode.Lenient,
  maxMessageSizeMB: 1,
  maxEntrySizeMB: 10); // Explicit limit for untrusted input
```

When processing untrusted QWK packets:
- Set explicit size limits
```csharp
using QwkPacket packet = QwkPacket.Open(
  path,
  ValidationMode.Strict, // Use Strict validation mode for untrusted input
  maxMessageSizeMB: 1, // Limit individual messages
  maxEntrySizeMB: 10   // Limit archive entries
);
```

- Monitor memory usage
- Process packets in isolated processes if possible
- Set memory limits at process level
- Consider streaming processing for very large packets
- Validate file paths before passing to library
```csharp
// Normalize and validate paths
string normalizedPath = Path.GetFullPath(userProvidedPath);
if (!normalizedPath.StartsWith(allowedDirectory))
{
  throw new SecurityException("Path outside permitted directory");
}
```
- Handle exceptions securely
- Don't log full file paths in production logs
- Sanitize error messages before displaying to users
- Use structured logging with path redaction
- Limit archive extensions
- Only register trusted extensions
- Validate extension behavior before production use
- Consider sandboxing extension execution
- Resource limits
- Set process-level memory limits
- Use timeouts for parsing operations
- Monitor for resource exhaustion
- Input validation
- Validate file sizes before opening
- Check file signatures/magic bytes
- Reject obviously malformed files early
