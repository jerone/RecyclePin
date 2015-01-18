using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Windows.Shell;

namespace RecyclePin
{
	public partial class RecyclePinForm : Form
	{
		[DllImport("dwmapi.dll", EntryPoint = "#127", PreserveSig = false)]
		public static extern void DwmGetColorizationParameters(out WDM_COLORIZATION_PARAMS parameters);

		[DllImport("dwmapi.dll", EntryPoint = "#131", PreserveSig = false)]
		public static extern void DwmSetColorizationParameters(WDM_COLORIZATION_PARAMS parameters, uint uUnknown);

		public struct WDM_COLORIZATION_PARAMS
		{
			public uint Color1;
			public uint Color2;
			public uint Intensity;
			public uint Unknown1;
			public uint Unknown2;
			public uint Unknown3;
			public uint Opaque;
		}


		[DllImport("dwmapi.dll")]
		internal static extern int DwmSetIconicThumbnail(
			IntPtr hwnd, IntPtr hbitmap, uint flags);

		[DllImport("dwmapi.dll")]
		internal static extern int DwmInvalidateIconicBitmaps(IntPtr hwnd);

		[DllImport("dwmapi.dll")]
		internal static extern int DwmSetIconicLivePreviewBitmap(
			IntPtr hwnd,
			IntPtr hbitmap,
			ref NativePoint ptClient,
			uint flags);





		#region Fields;

		private TaskbarManager _taskbar = TaskbarManager.Instance;

		private ThumbnailToolBarButton _tbtnOpen;
		private ThumbnailToolBarButton _tbtnEmpty;
		private ThumbnailToolBarSeperator _tbtnSeperator;
		private ThumbnailToolBarButton _tbtnClose;
		private ThumbnailToolBarButton _tbtnPin;

		private TabbedThumbnail _thumb;

		private StockIcon _binEmpty = new StockIcon(StockIconIdentifier.Recycler, StockIconSize.ShellSize, false, false);
		private StockIcon _binFulll = new StockIcon(StockIconIdentifier.RecyclerFull, StockIconSize.ShellSize, false, false);

		private RecycleBin _recycleBin;

		private Int32 _items = Int32.MinValue;
		private Int32 _size = Int32.MinValue;

		#endregion Fields;


		public RecyclePinForm()
		{
			InitializeComponent();

			_recycleBin = new RecycleBin();
			_recycleBin.RegisterEvents(Handle);
			RecycleBin.Changed += delegate
			{
				UpdateIcon();
				_thumb.InvalidatePreview();
			};
			RecycleBin.Emptied += delegate
			{
				UpdateIcon();
				_thumb.InvalidatePreview();
			};
		}


		#region Methods;

		/// <summary>
		/// Check if program is pinned;
		/// </summary>
		private void CheckPinned()
		{
			String path = Application.ExecutablePath; //@"C:\Users\jerone\Documents\Visual Studio 2008\Projects\RecyclePin\RecyclePin\bin\Debug\RecyclePin.exe";
			if (Windows7TaskbarHelper.IsPinnedToTaskbar(path))
			{
				_tbtnPin.Tooltip = "Unpin this program from taskbar";
				_tbtnPin.Icon = Properties.Resources.pin;
			}
			else
			{
				_tbtnPin.Tooltip = "Pin this program to taskbar";
				_tbtnPin.Icon = Properties.Resources.unpin;
			}
		}

		/// <summary>
		/// Update the form icon, thumbnail icon & overlay icon;
		/// </summary>
		private void UpdateIcon()
		{
			Int32 trash = RecycleBin.GetItems();
			if (trash > 0)
			{
				_tbtnOpen.Icon = _binFulll.Icon;
				_tbtnEmpty.Enabled = true;
				_thumb.SetWindowIcon(Icon = _binFulll.Icon);
				_taskbar.SetOverlayIcon(Handle, _binFulll.Icon, "");
			}
			else
			{
				_tbtnOpen.Icon = _binEmpty.Icon;
				_tbtnEmpty.Enabled = false;
				_thumb.SetWindowIcon(Icon = _binEmpty.Icon);
				_taskbar.SetOverlayIcon(Handle, _binEmpty.Icon, "null");
			}
		}

		/// <summary>
		/// Create a new thumbnail bitmap;
		/// </summary>
		/// <param name="items">Number of files/folders</param>
		/// <param name="size">Total size</param>
		/// <returns>A new thumbnail bitmap</returns>
		private static Bitmap CreateThumbnailBitmap(Int32 items, Int32 size)
		{
			Font fontHeader = new Font("Arial", 11, FontStyle.Bold, GraphicsUnit.Pixel);
			Font fontInfo = new Font("Consolas", 10, FontStyle.Regular, GraphicsUnit.Pixel);
			Font fontFooter = new Font("Arial", 11, FontStyle.Italic, GraphicsUnit.Pixel);
			SolidBrush brush = new SolidBrush(Color.Red);

			Bitmap bmp = new Bitmap(197, 119);

			Color c;
			WDM_COLORIZATION_PARAMS w = new WDM_COLORIZATION_PARAMS();
			DwmGetColorizationParameters(out w);
			c = Color.FromArgb((int)w.Color2);

			Graphics g = Graphics.FromImage(bmp);
			g.FillRectangle(new SolidBrush(Color.FromArgb(0, 200, 201, 202)), new Rectangle(0, 0, bmp.Width, bmp.Height));  // Transparent is actually light-gray;
			g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

			g.DrawString("Information:", fontHeader, brush, new PointF(5, 5));
			g.DrawString(String.Format("Number of items: {0}", items), fontInfo, brush, new PointF(5, 25));
			g.DrawString(String.Format("Total size: {0}", HumanizeBytes(size)), fontInfo, brush, new PointF(5, 40));
			g.DrawString("Click to open Recycle Bin!", fontFooter, brush, new PointF(5, 90));

			//g.Dispose();
			//bmp.MakeTransparent(Color.Black);
			return bmp;
		}

		/// <summary>
		/// Humanize bytes for strings;
		/// </summary>
		/// <param name="bytes">Number of bytes</param>
		/// <returns>A humanized bytes string</returns>
		private static String HumanizeBytes(Int32 bytes)
		{
			if (bytes >= (1024 * 1024 * 1024))
			{
				return String.Format("{0:N1} GB", bytes / (Double)(1024 * 1024 * 1024));
			}
			if (bytes >= (1024 * 1024))
			{
				return String.Format("{0:N1} MB", bytes / (Double)(1024 * 1024));
			}
			if (bytes >= 1024)
			{
				return String.Format("{0:N1} KB", bytes / (Double)1024);
			}
			return String.Format("{0} Bytes", bytes);
		}

		#endregion Methods;


		#region Internals/Overrides;

		/// <summary>
		/// Initialize all settings and components;
		/// </summary>
		private void InitializeComponent()
		{
			AllowDrop = true;
			AutoScaleDimensions = new SizeF(6F, 13F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = Size.Empty;
			FormBorderStyle = FormBorderStyle.None;
			Location = new Point(-20000, -20000);
			Name = "RecyclePinForm";
			Opacity = 90;
			Size = Size.Empty;
			StartPosition = FormStartPosition.Manual;
			Text = "";

			Shown += RecyclePinFormShown;
			Paint += RecyclePinFormPaint;

			//Microsoft.WindowsAPICodePack

			TaskbarManager.Instance.ApplicationId = "RecylcePin";

			_thumb = new TabbedThumbnail(Handle, Handle) { DisplayFrameAroundBitmap = false };
			//_thumb.TabbedThumbnailBitmapRequested += ThumbTabbedThumbnailBitmapRequested;
			//_thumb.InvalidatePreview();

			Bitmap image = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
			using (Graphics graphics = Graphics.FromImage(image))
			{
				graphics.Clear(Color.Transparent);
				graphics.FillRectangle(Brushes.Red, 0, 0, 10, 10);
				// Draw your stuff
			}

			DwmSetIconicThumbnail(Handle, image.GetHbitmap(), (uint)0);
			var nativePoint = new NativePoint(50, 50);
			DwmSetIconicLivePreviewBitmap(Handle, image.GetHbitmap(), ref nativePoint, (uint)0);

			_thumb.SetWindowIcon(Icon);
			_thumb.TabbedThumbnailActivated += ThumbTabbedThumbnailActivated;
			_thumb.TabbedThumbnailClosed += ThumbTabbedThumbnailClosed;
			_taskbar.TabbedThumbnail.AddThumbnailPreview(_thumb);

			_tbtnOpen = new ThumbnailToolBarButton(_binEmpty.Icon, "Open Recycle Bin");
			_tbtnOpen.Click += TbtnOpenClick;
			_tbtnEmpty = new ThumbnailToolBarButton(Properties.Resources.Empty, "Empty Recycle Bin");
			_tbtnEmpty.Click += TbtnEmptyClick;
			_tbtnSeperator = new ThumbnailToolBarSeperator();
			_tbtnPin = new ThumbnailToolBarButton(Properties.Resources.unpin, "Pin this program to taskbar");
			_tbtnPin.Click += TbtnPinClick;
			_tbtnClose = new ThumbnailToolBarButton(Properties.Resources.Close, "Close");
			_tbtnClose.Click += TbtnCloseClick;
			_taskbar.ThumbnailToolBars.AddButtons(Handle,
												  _tbtnOpen, _tbtnEmpty,
												  (ThumbnailToolBarButton)_tbtnSeperator,
												  _tbtnPin, _tbtnClose);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Dispose RecycleBin;
			_recycleBin.Dispose();
			_recycleBin = null;

			base.Dispose(disposing);
		}

		#endregion Internals/Overrides;


		#region Events;

		/// <summary>
		/// Form is shown;
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RecyclePinFormShown(object sender, EventArgs e)
		{
			// We want to have the corect icon; empty or full;
			UpdateIcon();
			// We want to have the correct status; unpinned or pinned;
			CheckPinned();
		}

		/// <summary>
		/// Form is (re)painted;
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void RecyclePinFormPaint(object sender, PaintEventArgs e)
		{
			CheckPinned();
		}

		/// <summary>
		/// Thumbnail button 'Open' clicked;
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TbtnOpenClick(object sender, ThumbnailButtonClickedEventArgs e)
		{
			RecycleBin.Open();
		}

		/// <summary>
		/// Thumbnail button 'Pin' clicked;
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TbtnPinClick(object sender, ThumbnailButtonClickedEventArgs e)
		{
			String path = Application.ExecutablePath; //@"C:\Users\jerone\Documents\Visual Studio 2008\Projects\RecyclePin\RecyclePin\bin\Debug\RecyclePin.exe";
			if (Windows7TaskbarHelper.IsPinnedToTaskbar(path))
			{
				Windows7TaskbarHelper.UnpinFromTaskbar(path);
				_tbtnPin.Tooltip = "Pin this program to taskbar";
				_tbtnPin.Icon = Properties.Resources.unpin;
			}
			else
			{
				Windows7TaskbarHelper.PinToTaskbar(path);
				_tbtnPin.Tooltip = "Unpin this program from taskbar";
				_tbtnPin.Icon = Properties.Resources.pin;
			}
		}

		/// <summary>
		/// Thumbnail button 'Empty' clicked;
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TbtnEmptyClick(object sender, ThumbnailButtonClickedEventArgs e)
		{
			RecycleBin.Empty();
			UpdateIcon();
		}

		/// <summary>
		/// Thumbnail button 'Close' clicked;
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TbtnCloseClick(object sender, ThumbnailButtonClickedEventArgs e)
		{
			Application.Exit();
		}

		/// <summary>
		/// Thumbnail activated;
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ThumbTabbedThumbnailActivated(object sender, TabbedThumbnailEventArgs e)
		{
			RecycleBin.Open();
		}











		private Boolean IconToAlphaBitmap(Bitmap bmp)
		{
			//Bitmap bmp = Bitmap.FromHbitmap(hbitmap);


			BitmapData bmData;
			Rectangle bmBounds = new Rectangle(0, 0, bmp.Width, bmp.Height);

			bmData = bmp.LockBits(bmBounds, ImageLockMode.ReadOnly, bmp.PixelFormat);

			Bitmap dstBitmap = new Bitmap(bmData.Width, bmData.Height, bmData.Stride, PixelFormat.Format32bppArgb, bmData.Scan0);

			bool IsAlphaBitmap = false;

			for (int y = 0; y <= bmData.Height - 1; y++)
			{
				for (int x = 0; x <= bmData.Width - 1; x++)
				{
					Color PixelColor = Color.FromArgb(Marshal.ReadInt32(bmData.Scan0, (bmData.Stride * y) + (4 * x)));
					if (PixelColor.A > 0 & PixelColor.A < 255)
					{
						IsAlphaBitmap = true;
						break;
					}
				}
				if (IsAlphaBitmap) break;
			}

			bmp.UnlockBits(bmData);

			return IsAlphaBitmap;

		}


		/// <summary>
		/// Thumbnail bitmap requested;
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ThumbTabbedThumbnailBitmapRequested(object sender, TabbedThumbnailBitmapRequestedEventArgs e)
		{
			Bitmap bmp2a = RecyclePin.Properties.Resources.test1;
			Boolean test3a = IconToAlphaBitmap(bmp2a);
			Boolean test2a = Bitmap.IsAlphaPixelFormat(bmp2a.PixelFormat);

			Bitmap bmp2b = RecyclePin.Properties.Resources.test2;
			Boolean test3b = IconToAlphaBitmap(bmp2b);
			Boolean test2b = Bitmap.IsAlphaPixelFormat(bmp2b.PixelFormat);

			Bitmap bmp2c = RecyclePin.Properties.Resources.test;
			Boolean testc3 = IconToAlphaBitmap(bmp2c);
			Boolean test2c = Bitmap.IsAlphaPixelFormat(bmp2c.PixelFormat);

			Bitmap image = new Bitmap(100, 100, PixelFormat.Format32bppArgb);
			using (Graphics graphics = Graphics.FromImage(image))
			{
				graphics.Clear(Color.Transparent);
				// Draw your stuff
				graphics.CompositingMode = CompositingMode.SourceCopy;

			}


			_thumb.SetImage(image);
			e.Handled = true;
			return;


			Int32 items = RecycleBin.GetItems();
			Int32 size = RecycleBin.GetSize();

			/*
			List<RecycleBin.RecycleBinItem> list = RecycleBin.GetItems2();
			Int32 items2 = list.Count;
			Int32 size2 = list.Sum(a => a.FileSize);
			*/

			// Only when the number of items of size is changed, do we create a new bitmap;
			if (!items.Equals(_items) || !size.Equals(_size))
			{
				_items = items;
				_size = size;

				Bitmap bmp = CreateThumbnailBitmap(items, size);
				_thumb.SetImage(bmp);

				e.Handled = true;
			}
			else
			{
				e.Handled = false;
			}

		}

		/// <summary>
		/// Thumbnail closed;
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ThumbTabbedThumbnailClosed(object sender, TabbedThumbnailEventArgs e)
		{
			Application.Exit();
		}

		#endregion Events;
	}
}