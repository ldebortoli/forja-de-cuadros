using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ForjaDeCuadros
{
    public static class WindowsIdentity
    {
        private const string AppId = "io.github.ldebortoli.ForjaDeCuadros";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PropertyKey
        {
            public Guid FormatId;
            public uint PropertyId;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant
        {
            [FieldOffset(0)] public ushort ValueType;
            [FieldOffset(8)] public IntPtr PointerValue;
        }

        [ComImport]
        [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            [PreserveSig] int GetCount(out uint propertyCount);
            [PreserveSig] int GetAt(uint propertyIndex, out PropertyKey key);
            [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
            [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
            [PreserveSig] int Commit();
        }

        [DllImport("shell32.dll")]
        private static extern int SHGetPropertyStoreForWindow(IntPtr windowHandle, ref Guid interfaceId, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore propertyStore);

        [DllImport("ole32.dll")]
        private static extern int PropVariantClear(ref PropVariant value);

        public static bool Apply(Window window)
        {
            IntPtr handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return false;
            Guid interfaceId = typeof(IPropertyStore).GUID;
            int result = SHGetPropertyStoreForWindow(handle, ref interfaceId, out IPropertyStore store);
            if (result != 0) return false;
            var key = new PropertyKey { FormatId = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), PropertyId = 5 };
            var value = new PropVariant { ValueType = 31, PointerValue = Marshal.StringToCoTaskMemUni(AppId) };
            try
            {
                int setResult = store.SetValue(ref key, ref value);
                int commitResult = setResult == 0 ? store.Commit() : setResult;
                return setResult == 0 && commitResult == 0;
            }
            finally
            {
                PropVariantClear(ref value);
                Marshal.ReleaseComObject(store);
            }
        }
    }
}
