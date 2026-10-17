#pragma warning disable WFDEV005
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ComIDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace MultiBox.Core
{
    [ComImport]
    [Guid("00000122-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDropTarget
    {
        [PreserveSig]
        int DragEnter([In] ComIDataObject pDataObj, [In] int grfKeyState, [In] POINTL pt, [In, Out] ref int pdwEffect);

        [PreserveSig]
        int DragOver([In] int grfKeyState, [In] POINTL pt, [In, Out] ref int pdwEffect);

        [PreserveSig]
        int DragLeave();

        [PreserveSig]
        int Drop([In] ComIDataObject pDataObj, [In] int grfKeyState, [In] POINTL pt, [In, Out] ref int pdwEffect);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINTL
    {
        public int X;
        public int Y;
    }

    public class OleFileDropTarget : IDropTarget
    {
        private const int S_OK = 0;
        private const int DROPEFFECT_NONE = 0;
        private const int DROPEFFECT_COPY = 1;

        private readonly Action<bool> _onHover;
        private readonly Action<string> _onDrop;

        public OleFileDropTarget(Action<bool> onHover, Action<string> onDrop)
        {
            _onHover = onHover;
            _onDrop = onDrop;
        }

        public int DragEnter(ComIDataObject pDataObj, int grfKeyState, POINTL pt, ref int pdwEffect)
        {
            try
            {
                var data = new System.Windows.Forms.DataObject(pDataObj);
                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    pdwEffect = DROPEFFECT_COPY;
                    _onHover(true);
                    return S_OK;
                }
            }
            catch { }

            pdwEffect = DROPEFFECT_NONE;
            return S_OK;
        }

        public int DragOver(int grfKeyState, POINTL pt, ref int pdwEffect)
        {
            pdwEffect = DROPEFFECT_COPY;
            return S_OK;
        }

        public int DragLeave()
        {
            try
            {
                _onHover(false);
            }
            catch { }
            return S_OK;
        }

        public int Drop(ComIDataObject pDataObj, int grfKeyState, POINTL pt, ref int pdwEffect)
        {
            try
            {
                _onHover(false);
                var data = new System.Windows.Forms.DataObject(pDataObj);
                if (data.GetDataPresent(DataFormats.FileDrop))
                {
                    if (data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
                    {
                        pdwEffect = DROPEFFECT_COPY;
                        _onDrop(files[0]);
                        return S_OK;
                    }
                }
            }
            catch { }

            pdwEffect = DROPEFFECT_NONE;
            return S_OK;
        }
    }
}
