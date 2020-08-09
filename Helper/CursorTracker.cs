using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ProgramTracker.Helper
{
    class CursorTracker
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);
        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        public static Point GetCursorPosition()
        {
            Win32Point w32Cursor = new Win32Point();
            GetCursorPos(ref w32Cursor);
            return new Point(w32Cursor.X, w32Cursor.Y);
        }
    }

    class Point
    {
        private int x;
        private int y;

        public Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Boolean Equals(Point other)
        {
            if (this.x == other.x && this.y == other.y)
            {
                return true;
            }
            return false;
        }

        public override string ToString()
        {
            return $"{this.x}|{this.y}";
        }
    }
}
