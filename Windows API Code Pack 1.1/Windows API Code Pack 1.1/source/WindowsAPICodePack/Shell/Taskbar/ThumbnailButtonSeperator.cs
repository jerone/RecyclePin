//Copyright (c) jerone.  All rights reserved.


using System;

namespace Microsoft.WindowsAPICodePack.Taskbar
{

	/// <summary>
	/// Represents a taskbar thumbnail button seperator in the thumbnail toolbar.
	/// </summary>
	public sealed class ThumbnailToolBarSeperator : IDisposable
	{
		private static ThumbnailToolBarButton _ttbb;

		///<summary>
		/// Initializes an instance of this class
		///</summary>
		public ThumbnailToolBarSeperator()
		{
			_ttbb = new ThumbnailToolBarButton(null, "");
			_ttbb.Flags |= ThumbButtonOptions.NoBackground;
			_ttbb.UpdateThumbnailButton();
		}

		/// <summary>
		/// Returns as a ThumbnailToolBarButton type;
		/// </summary>
		/// <param name="undefined"></param>
		/// <returns></returns>
		public static explicit operator ThumbnailToolBarButton(ThumbnailToolBarSeperator undefined)
		{
			return _ttbb;
		}


		#region IDisposable Members

		/// <summary>
		/// 
		/// </summary>
		~ThumbnailToolBarSeperator()
		{
			Dispose(false);
		}

		/// <summary>
		/// Release the native objects.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Release the native objects.
		/// </summary>
		/// <param name="disposing"></param>
		public void Dispose(bool disposing)
		{
			if (disposing)
			{
				// Dispose managed resources
				_ttbb.Dispose(true);
			}
		}

		#endregion

	}
}
