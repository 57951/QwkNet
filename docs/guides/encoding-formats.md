# Character Encoding in QWK Packets

This guide explains character encoding considerations when working with QWK packets. It covers why encoding matters, common issues you might encounter, and how QWK.NET handles encoding automatically.

## Why Encoding Matters for QWK Packets

QWK packets were created by DOS-based BBS systems from the 1980s and 1990s. These systems used **CP437** (Code Page 437), the original IBM PC character encoding, as their native format. This historical context means:

- **BBS names** often contain box-drawing characters for visual formatting
- **Message content** may include accented characters, mathematical symbols, or special characters
- **Line terminators** use a specific byte (0xE3) that represents the π character in CP437
- **Extended ASCII** characters have different meanings in CP437 compared to modern encodings

Using the wrong encoding when reading or displaying QWK packets can cause:
- Box-drawing characters to appear as accented letters or symbols
- Message line breaks to be unrecognised
- Accented characters to display incorrectly
- Loss of visual formatting in BBS names and messages

## CP437 in Practice

CP437 is a single-byte encoding with 256 characters. The first 128 characters (0x00-0x7F) are standard ASCII, whilst the extended range (0x80-0xFF) contains characters specific to CP437, including box-drawing characters, accented letters, and mathematical symbols.

The same byte value can represent different characters in different encodings. For example, byte 0xE3 represents π (pi) in CP437 but ã (a-tilde) in Latin-1 or Windows-1252. This is why using the correct encoding is essential for accurate display and preservation of QWK packet content.

## Common Symptoms of Encoding Problems

If you encounter encoding issues, you might see:

**Box-drawing characters display incorrectly:**
- Expected: `─══ StarLink BBS ══─`
- Seen: `ã══ StarLink BBS ══ã` or similar

**Line breaks not working:**
- Message bodies appear as single long lines
- Line breaks appear in wrong places

**Accented characters wrong:**
- Names like "José" appear as "JosÃ©" or "Jos?"

**Validation warnings:**
- Warnings about unmappable bytes or invalid characters

**Round-trip failures:**
- QWK → REP → QWK cycle produces different bytes

## How QWK.NET Approaches Encoding

QWK.NET handles encoding automatically to ensure correct reading and preservation of QWK packets.

### Default Behaviour

The library uses **CP437 encoding by default** for all text fields in QWK packets:
- Control data (BBS name, sysop name, etc.)
- Message headers (From, To, Subject)
- Message bodies
- Optional files (WELCOME, NEWS, GOODBYE)

You don't need to configure encoding manually—the library applies CP437 automatically.

### Encoding During Reading

When reading QWK packets:
1. Raw bytes are read from the archive without conversion
2. CP437 decoding converts bytes to Unicode strings for API access
3. Original bytes are preserved in raw properties for round-trip fidelity
4. Line terminators (0xE3) are detected and converted to line breaks

This means you can access decoded text through the API whilst the library preserves the original bytes for accurate round-trip conversion.

### Encoding During Writing

When writing REP packets:
1. Unicode strings are encoded back to CP437 bytes
2. Line terminators are converted to 0xE3 bytes
3. Records are padded according to QWK format requirements
4. Round-trip fidelity ensures QWK → REP → QWK preserves all bytes

The library ensures that packets you create are correctly encoded and compatible with QWK format requirements.

### Byte Preservation

QWK.NET prioritises byte-level fidelity. The library preserves original bytes when reading packets and ensures deterministic output when writing packets. This means that given the same input, the library produces identical output bytes, supporting accurate round-trip conversion and preservation of historical packet content.

## What You Need to Know

**For most users:**
- QWK.NET handles encoding automatically—you don't need to configure anything
- The library uses CP437 by default, which is correct for historical QWK packets
- If you see encoding-related display issues, check your console or display system encoding settings

**For developers:**
- The library preserves original bytes for round-trip fidelity
- You can access both decoded text (for display) and raw bytes (for preservation)
- Encoding issues may appear as validation warnings—check validation reports

**When troubleshooting:**
- Encoding problems often manifest as display issues (wrong characters, broken line breaks)
- Check validation reports for encoding-related warnings
- Ensure your display system supports CP437 characters if viewing box-drawing characters

## Further Reading

- [API Overview](../api-overview.md) - API methods for accessing decoded text and raw bytes
