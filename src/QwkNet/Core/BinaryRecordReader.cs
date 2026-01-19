using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QwkNet.Core;

/// <summary>
/// Reads 128-byte fixed-length records from QWK message files.
/// </summary>
/// <remarks>
/// QWK messages are stored as contiguous 128-byte records in MESSAGES.DAT.
/// This reader provides byte-accurate access with minimal allocations using Span.
/// </remarks>
public sealed class BinaryRecordReader : IDisposable
{
  /// <summary>
  /// The fixed size of each QWK record in bytes.
  /// </summary>
  public const int RecordSize = 128;

  private readonly Stream _stream;
  private readonly bool _leaveOpen;
  private bool _disposed;

  /// <summary>
  /// Initialises a new instance of the <see cref="BinaryRecordReader"/> class.
  /// </summary>
  /// <param name="stream">The stream to read from.</param>
  /// <param name="leaveOpen">Whether to leave the stream open after disposal.</param>
  /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
  public BinaryRecordReader(Stream stream, bool leaveOpen = false)
  {
    _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    _leaveOpen = leaveOpen;
  }

  /// <summary>
  /// Gets the current position in records (not bytes).
  /// </summary>
  public long RecordPosition => _stream.Position / RecordSize;

  /// <summary>
  /// Gets the total number of records in the stream.
  /// </summary>
  public long RecordCount => _stream.Length / RecordSize;

  /// <summary>
  /// Reads a single 128-byte record into the provided buffer.
  /// </summary>
  /// <param name="buffer">The buffer to read into (must be at least 128 bytes).</param>
  /// <returns>The number of bytes read (0 if end of stream, 128 for success).</returns>
  /// <exception cref="ArgumentException">Thrown when buffer is less than 128 bytes.</exception>
  /// <exception cref="ObjectDisposedException">Thrown when reader is disposed.</exception>
  public int ReadRecord(Span<byte> buffer)
  {
    ThrowIfDisposed();

    if (buffer.Length < RecordSize)
    {
      throw new ArgumentException(
        $"Buffer must be at least {RecordSize} bytes.",
        nameof(buffer));
    }

    int totalRead = 0;
    Span<byte> target = buffer.Slice(0, RecordSize);

    while (totalRead < RecordSize)
    {
      int bytesRead = _stream.Read(target.Slice(totalRead));
      if (bytesRead == 0)
      {
        // End of stream reached
        return totalRead;
      }
      totalRead += bytesRead;
    }

    return totalRead;
  }

  /// <summary>
  /// Reads a single 128-byte record asynchronously.
  /// </summary>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>A byte array containing the record, or null if end of stream.</returns>
  /// <exception cref="ObjectDisposedException">Thrown when reader is disposed.</exception>
  public async Task<byte[]?> ReadRecordAsync(
    CancellationToken cancellationToken = default)
  {
    ThrowIfDisposed();

    byte[] buffer = ArrayPool<byte>.Shared.Rent(RecordSize);
    try
    {
      int totalRead = 0;
      Memory<byte> target = buffer.AsMemory(0, RecordSize);

      while (totalRead < RecordSize)
      {
        int bytesRead = await _stream.ReadAsync(
          target.Slice(totalRead),
          cancellationToken).ConfigureAwait(false);

        if (bytesRead == 0)
        {
          // End of stream
          return totalRead == 0 ? null : null;
        }
        totalRead += bytesRead;
      }

      // Copy to exact-sized array
      byte[] result = new byte[RecordSize];
      Array.Copy(buffer, 0, result, 0, RecordSize);
      return result;
    }
    finally
    {
      ArrayPool<byte>.Shared.Return(buffer);
    }
  }

  /// <summary>
  /// Seeks to a specific record position.
  /// </summary>
  /// <param name="recordNumber">The zero-based record number to seek to.</param>
  /// <exception cref="ArgumentOutOfRangeException">
  /// Thrown when recordNumber is negative or beyond stream length.
  /// </exception>
  /// <exception cref="ObjectDisposedException">Thrown when reader is disposed.</exception>
  public void SeekToRecord(long recordNumber)
  {
    ThrowIfDisposed();

    if (recordNumber < 0)
    {
      throw new ArgumentOutOfRangeException(
        nameof(recordNumber),
        "Record number cannot be negative.");
    }

    long bytePosition = recordNumber * RecordSize;
    if (bytePosition > _stream.Length)
    {
      throw new ArgumentOutOfRangeException(
        nameof(recordNumber),
        $"Record {recordNumber} exceeds stream length.");
    }

    _stream.Seek(bytePosition, SeekOrigin.Begin);
  }

  /// <summary>
  /// Reads all remaining records from the current position.
  /// </summary>
  /// <returns>A collection of byte arrays, each containing one record.</returns>
  /// <exception cref="ObjectDisposedException">Thrown when reader is disposed.</exception>
  /// <remarks>
  /// This method loads all data into memory. Use with caution on large files.
  /// </remarks>
  public byte[][] ReadAllRecords()
  {
    ThrowIfDisposed();

    long remainingRecords = RecordCount - RecordPosition;
    if (remainingRecords == 0)
    {
      return Array.Empty<byte[]>();
    }

    byte[][] records = new byte[remainingRecords][];
    Span<byte> buffer = stackalloc byte[RecordSize];

    for (long i = 0; i < remainingRecords; i++)
    {
      int bytesRead = ReadRecord(buffer);
      if (bytesRead != RecordSize)
      {
        // Partial record at end - resize array
        Array.Resize(ref records, (int)i);
        break;
      }

      records[i] = buffer.ToArray();
    }

    return records;
  }

  /// <summary>
  /// Validates that the stream length is a multiple of 128 bytes.
  /// </summary>
  /// <returns>True if stream length is valid, false otherwise.</returns>
  public bool ValidateStreamLength()
  {
    return _stream.Length % RecordSize == 0;
  }

  /// <summary>
  /// Disposes the reader and optionally the underlying stream.
  /// </summary>
  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    if (!_leaveOpen)
    {
      _stream.Dispose();
    }

    _disposed = true;
  }

  private void ThrowIfDisposed()
  {
    if (_disposed)
    {
      throw new ObjectDisposedException(nameof(BinaryRecordReader));
    }
  }
}
