using CapsLockKing.Core;

namespace CapsLockKing.Tests;

public class SenderTests
{
    /// <summary>
    /// เคยพลาดมาแล้ว: ถ้า INPUT union ไม่มี MOUSEINPUT ขนาดจะเหลือ 32 (x64)
    /// แล้ว SendInput จะ fail ด้วย ERROR_INVALID_PARAMETER โดยไม่มีใครรู้
    /// </summary>
    [Fact]
    public void Input_struct_size_matches_windows_expectation()
    {
        Assert.Equal(Sender.ExpectedInputSize, Sender.InputSize);
        Assert.Equal(IntPtr.Size == 8 ? 40 : 28, Sender.InputSize);
    }
}
