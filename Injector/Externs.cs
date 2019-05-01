using System.Runtime.InteropServices;

namespace Lizard
{
	public static class Externs
	{
		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern int MessageBox(System.IntPtr hWnd, string text, string caption, int options);

		public static void MessageBox(string title, string description) => MessageBox(System.IntPtr.Zero, description, title, 0);
	}
}
