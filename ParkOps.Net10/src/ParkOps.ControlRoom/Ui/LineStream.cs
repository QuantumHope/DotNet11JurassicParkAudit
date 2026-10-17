using System.Text;

namespace ParkOps.ControlRoom.Ui;

/// <summary>A write-only stream that hands each completed line of UTF-8 text to a callback.</summary>
public sealed class LineStream(Action<string> onLine) : Stream
{
    private readonly StringBuilder pending = new();
    private readonly Decoder decoder = Encoding.UTF8.GetDecoder();

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Span<char> chars = stackalloc char[Encoding.UTF8.GetMaxCharCount(buffer.Length)];
        var length = decoder.GetChars(buffer, chars, flush: false);
        foreach (var c in chars[..length])
        {
            if (c == '\n') EmitPending();
            else if (c != '\r') pending.Append(c);
        }
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Write(buffer.AsSpan(offset, count));
        return Task.CompletedTask;
    }

    public override void Flush() { }

    protected override void Dispose(bool disposing)
    {
        EmitPending();
        base.Dispose(disposing);
    }

    private void EmitPending()
    {
        if (pending.Length == 0) return;
        onLine(pending.ToString());
        pending.Clear();
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
