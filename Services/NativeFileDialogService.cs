using System.ComponentModel;
using System.Runtime.InteropServices;

namespace PortManager.Services;

/// <summary>
/// Windows common file dialogs. These work when the unpackaged app is elevated,
/// where the brokered WinUI picker may return E_FAIL.
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
        return Show(owner, "Open firewall rules", OfnExplorer | OfnFileMustExist | OfnPathMustExist | OfnNoChangeDir, null);
    }

    public static string? SaveJson(IntPtr owner)
    {
        return Show(owner, "Export firewall rules", OfnExplorer | OfnPathMustExist | OfnOverwritePrompt | OfnNoChangeDir, "json");
    }

    private static string? Show(IntPtr owner, string title, int flags, string? defaultExtension)
    {
        var filter = Marshal.StringToHGlobalUni("JSON files (*.json)\0*.json\0All files (*.*)\0*.*\0\0");
        var fileBuffer = Marshal.AllocHGlobal(MaxPath * sizeof(char));
        var titleBuffer = Marshal.StringToHGlobalUni(title);
        var extensionBuffer = defaultExtension is null ? IntPtr.Zero : Marshal.StringToHGlobalUni(defaultExtension);
        try
        {
            for (var index = 0; index < MaxPath; index++)
                Marshal.WriteInt16(fileBuffer, index * sizeof(char), 0);

            if (defaultExtension is not null)
            {
                var initialName = "win-xinai-de-tools-rules.json";
                for (var index = 0; index < initialName.Length; index++)
                    Marshal.WriteInt16(fileBuffer, index * sizeof(char), initialName[index]);
            }

            var dialog = new OpenFileName
            {
                StructSize = Marshal.SizeOf<OpenFileName>(),
                Owner = owner,
                Filter = filter,
                FileName = fileBuffer,
                MaxFile = MaxPath,
                Title = titleBuffer,
                DefaultExtension = extensionBuffer,
                Flags = flags
            };

            var succeeded = defaultExtension is null
                ? GetOpenFileName(ref dialog)
                : GetSaveFileName(ref dialog);
            if (succeeded)
                return Marshal.PtrToStringUni(dialog.FileName);

            var error = CommDlgExtendedError();
            if (error == 0) return null; // User cancelled.
            throw new Win32Exception((int)error, $"Windows file dialog failed (0x{error:X8}).");
        }
        finally
        {
            if (extensionBuffer != IntPtr.Zero) Marshal.FreeHGlobal(extensionBuffer);
            Marshal.FreeHGlobal(titleBuffer);
            Marshal.FreeHGlobal(fileBuffer);
            Marshal.FreeHGlobal(filter);
        }
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetOpenFileName(ref OpenFileName fileName);

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSaveFileName(ref OpenFileName fileName);

    [DllImport("comdlg32.dll")]
    private static extern uint CommDlgExtendedError();

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenFileName
    {
        public int StructSize;
        public IntPtr Owner;
        public IntPtr Instance;
        public IntPtr Filter;
        public IntPtr CustomFilter;
        public int MaxCustFilter;
        public int FilterIndex;
        public IntPtr FileName;
        public int MaxFile;
        public IntPtr FileTitle;
        public int MaxFileTitle;
        public IntPtr InitialDirectory;
        public IntPtr Title;
        public int Flags;
        public short FileOffset;
        public short FileExtension;
        public IntPtr DefaultExtension;
        public IntPtr CustData;
        public IntPtr Hook;
        public IntPtr TemplateName;
        public IntPtr ReservedPtr;
        public int Reserved;
        public int FlagsEx;
    }
}
