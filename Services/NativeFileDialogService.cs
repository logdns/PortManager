using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace PortManager.Services;

/// <summary>
/// Uses the Windows common dialogs instead of the brokered WinUI picker.
/// The brokered picker can fail with E_FAIL when the unpackaged app is elevated.
/// </summary>
internal static class NativeFileDialogService
{
    private const int MaxPath = 32_768;
    private const int OfnExplorer = 0x0008_0000;
    private const int OfnFileMustExist = 0x0000_1000;
    private const int OfnPathMustExist = 0x0000_0800;
    private const int OfnOverwritePrompt = 0x0000_0002;
    private const int OfnNoChangeDir = 0x0000_0008;

    public static string? OpenJson(IntPtr owner)
    {
        var file = CreateFileName(owner, "Open firewall rules", OfnExplorer | OfnFileMustExist | OfnPathMustExist | OfnNoChangeDir);
        return GetOpenFileName(ref file) ? file.FileName.ToString() : ThrowIfDialogFailed();
    }

    public static string? SaveJson(IntPtr owner)
    {
        var file = CreateFileName(owner, "Export firewall rules", OfnExplorer | OfnPathMustExist | OfnOverwritePrompt | OfnNoChangeDir);
        file.DefaultExtension = "json";
        file.FileName.Append("PortManager-rules.json");
        return GetSaveFileName(ref file) ? file.FileName.ToString() : ThrowIfDialogFailed();
    }

    private static OpenFileName CreateFileName(IntPtr owner, string title, int flags) => new()
    {
        StructSize = Marshal.SizeOf<OpenFileName>(),
        Owner = owner,
        Filter = "JSON files (*.json)\0*.json\0All files (*.*)\0*.*\0\0",
        FileName = new StringBuilder(MaxPath),
        MaxFile = MaxPath,
        Title = title,
        Flags = flags
    };

    private static string? ThrowIfDialogFailed()
    {
        var error = CommDlgExtendedError();
        if (error == 0) return null; // User cancelled.
        throw new Win32Exception((int)error, $"Windows file dialog failed (0x{error:X8}).");
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetOpenFileName(ref OpenFileName fileName);

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSaveFileName(ref OpenFileName fileName);

    [DllImport("comdlg32.dll")]
    private static extern uint CommDlgExtendedError();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenFileName
    {
        public int StructSize;
        public IntPtr Owner;
        public IntPtr Instance;
        [MarshalAs(UnmanagedType.LPWStr)] public string Filter;
        public IntPtr CustomFilter;
        public int MaxCustFilter;
        public int FilterIndex;
        [MarshalAs(UnmanagedType.LPWStr)] public StringBuilder FileName;
        public int MaxFile;
        [MarshalAs(UnmanagedType.LPWStr)] public string FileTitle;
        public int MaxFileTitle;
        [MarshalAs(UnmanagedType.LPWStr)] public string InitialDirectory;
        [MarshalAs(UnmanagedType.LPWStr)] public string Title;
        public int Flags;
        public short FileOffset;
        public short FileExtension;
        [MarshalAs(UnmanagedType.LPWStr)] public string DefaultExtension;
        public IntPtr CustData;
        public IntPtr Hook;
        [MarshalAs(UnmanagedType.LPWStr)] public string TemplateName;
        public IntPtr ReservedPtr;
        public int Reserved;
        public int FlagsEx;
    }
}
