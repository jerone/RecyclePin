using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Shell32;

namespace RecyclePin
{
	public class RecycleBin : NativeWindow, IDisposable
	{

		#region Fields;

		private static ShellNotifications _notifications = new ShellNotifications();
		private Boolean _registerEvents = false;

		#endregion Fields;


		public RecycleBin()
		{
			this.CreateHandle(new CreateParams());
		}


		#region Internals/Overrides;

		protected override void WndProc(ref Message m)
		{
			base.WndProc(ref m);

			if (_registerEvents && Changed != null)
			{
				if ((m.Msg == 0x401) && _notifications.NotificationReceipt(m.WParam, m.LParam))
				{
					NotifyInfos infos = (NotifyInfos)_notifications.NotificationsReceived[_notifications.NotificationsReceived.Count - 1];
					if ((infos.Notification == ShellNotifications.SHCNE.SHCNE_DELETE) || infos.Item1.Contains("$Recycle.Bin"))
					{
						if (Changed != null)
						{
							Changed(this, new EventArgs());
						}
					}
				}
			}
		}

		public void Dispose()
		{
			_notifications.UnregisterChangeNotify();
			ReleaseHandle();
		}

		~RecycleBin()
		{
			Dispose();
		}

		#endregion Internals/Overrides;


		#region Methods;

		public void RegisterEvents(IntPtr handle)
		{
			_registerEvents = true;
			_notifications.RegisterChangeNotify(this.Handle, ShellNotifications.CSIDL.CSIDL_DESKTOP, true);
		}

		public static Int32 GetItems()
		{
			try
			{
				SHQUERYRBINFO structure = new SHQUERYRBINFO();
				structure.cbSize = (uint)Marshal.SizeOf(structure);
				SHQueryRecycleBin(IntPtr.Zero, ref structure);
				return (Int32)structure.i64NumItems;
			}
			catch (Exception)
			{
				return 0;
			}
		}
		public static List<RecycleBinItem> GetItems2()
		{
			try
			{
				List<RecycleBinItem> list = new List<RecycleBinItem>();
				Shell shell = new Shell();
				Folder recycleBin = shell.NameSpace(10);

				try
				{
					list.AddRange(from FolderItem f in recycleBin.Items()
								  select new RecycleBinItem
											{
												FileName = f.Name,
												FileType = f.Type,
												FileSize = GetSize(f)
											});
				}
				catch (Exception ex)
				{
					Debug.WriteLine(String.Format("Error 1 accessing the Recycle Bin: {0}", ex.Message));
				}

				//release
				Marshal.FinalReleaseComObject(shell);

				return list;
			}
			catch (Exception ex)
			{
				Debug.WriteLine(String.Format("Error 2 accessing the Recycle Bin: {0}", ex.Message));
				return null;
			}
		}

		public static Int32 GetSize()
		{
			try
			{
				SHQUERYRBINFO structure = new SHQUERYRBINFO();
				structure.cbSize = (uint)Marshal.SizeOf(structure);
				SHQueryRecycleBin(IntPtr.Zero, ref structure);
				return (Int32)structure.i64Size;
			}
			catch (Exception)
			{
				return 0;
			}
		}
		public static Int32 GetSize(FolderItem folderItem)
		{
			//check if it's a folder, if it's not then return it's size);
			if (!folderItem.IsFolder)
			{
				return folderItem.Size;
			}

			Folder folder = (Folder)folderItem.GetFolder;

			FolderItems3 items = (FolderItems3)folder.Items();

			// Used filter to get the hidden files also;
			items.Filter((int)(SHCONTF.SHCONTF_FOLDERS | SHCONTF.SHCONTF_NONFOLDERS | SHCONTF.SHCONTF_INCLUDEHIDDEN), "*");

			return items.Cast<FolderItem>().Sum(f => GetSize(f));
		}

		public static Boolean Empty(String root = null, RecycleFlags flags = RecycleFlags.SHERB_NULL)
		{
			uint result = SHEmptyRecycleBin(IntPtr.Zero, root, flags);
			if (Emptied != null)
			{
				Emptied(new Object(), new EventArgs());
			}
			return (result > 0);
		}
		public static Boolean EmptySilent(String root = null)
		{
			return Empty(root, RecycleFlags.SHERB_NOCONFIRMATION | RecycleFlags.SHERB_NOPROGRESSUI | RecycleFlags.SHERB_NOSOUND);
		}

		public static Boolean Open()
		{
			IntPtr result = ShellExecute(IntPtr.Zero, "Open", "explorer.exe", "/root,::{645FF040-5081-101B-9F08-00AA002F954E}", "", ShowCommands.SW_SHOWNORMAL);
			return (result.ToInt32() > 32);
		}

		#endregion Methods;


		#region Events;
		// "OnXxxing" is nu bezig, moet ook een verleden tijd event hebben;
		// "OnXxxed" is verleden tijd, het is al gebeurd;

		public delegate void OnEmptied(Object sender, EventArgs e);
		public static event OnEmptied Emptied;

		public delegate void OnChanged(Object sender, EventArgs e);
		public static event OnChanged Changed;

		#endregion Events;


		#region DllImports;

		[DllImport("shell32.dll", CharSet = CharSet.Unicode)]
		private static extern int SHQueryRecycleBin(IntPtr pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

		[DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
		private static extern uint SHEmptyRecycleBin(IntPtr hwnd, String pszRootPath, RecycleFlags dwFlags);

		[DllImport("shell32.dll")]
		static extern IntPtr ShellExecute(
			IntPtr hwnd,
			String lpOperation,
			String lpFile,
			String lpParameters,
			String lpDirectory,
			ShowCommands nShowCmd);

		#endregion DllImports;


		#region Structs & Flags;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct SHQUERYRBINFO
		{
			public uint cbSize;
			public ulong i64Size;
			public ulong i64NumItems;
		}

		[Flags]
		public enum RecycleFlags : uint
		{
			SHERB_NULL = 0,
			SHERB_NOCONFIRMATION = 0x00000001,
			SHERB_NOPROGRESSUI = 0x00000002,
			SHERB_NOSOUND = 0x00000004
		}

		[Flags]
		public enum ShowCommands
		{
			SW_HIDE = 0,
			SW_SHOWNORMAL = 1,
			SW_NORMAL = 1,
			SW_SHOWMINIMIZED = 2,
			SW_SHOWMAXIMIZED = 3,
			SW_MAXIMIZE = 3,
			SW_SHOWNOACTIVATE = 4,
			SW_SHOW = 5,
			SW_MINIMIZE = 6,
			SW_SHOWMINNOACTIVE = 7,
			SW_SHOWNA = 8,
			SW_RESTORE = 9,
			SW_SHOWDEFAULT = 10,
			SW_FORCEMINIMIZE = 11,
			SW_MAX = 11
		}

		[Flags]
		public enum SHCONTF
		{
			SHCONTF_CHECKING_FOR_CHILDREN = 0x00010,
			SHCONTF_FOLDERS = 0x00020,
			SHCONTF_NONFOLDERS = 0x00040,
			SHCONTF_INCLUDEHIDDEN = 0x00080,
			SHCONTF_INIT_ON_FIRST_NEXT = 0x00100,
			SHCONTF_NETPRINTERSRCH = 0x00200,
			SHCONTF_SHAREABLE = 0x00400,
			SHCONTF_STORAGE = 0x00800,
			SHCONTF_NAVIGATION_ENUM = 0x01000,
			SHCONTF_FASTITEMS = 0x02000,
			SHCONTF_FLATLIST = 0x04000,
			SHCONTF_ENABLE_ASYNC = 0x08000,
			SHCONTF_INCLUDESUPERHIDDEN = 0x10000
		}

		#endregion Structs & Flags;


		#region Subclass;

		public class RecycleBinItem
		{
			// http://msdn.microsoft.com/en-us/library/bb787810%28v=VS.85%29.aspx
			public String FileName { get; set; }
			public String FileType { get; set; }
			public Int32 FileSize { get; set; }
		}

		#endregion Subclass;
	}
}