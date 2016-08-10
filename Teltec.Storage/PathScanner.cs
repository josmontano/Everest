using System;

namespace Teltec.Storage
{
	public delegate void PathScannerFileAddedHandler<T>(object sender, T file) where T : class;
	public delegate void PathScannerEntryScanFailedHandler(object sender, string path, string message, Exception ex);

	public interface IPathScanner<T> where T : class
	{
		PathScanResults<T> Results { get; }
		void Scan();

		// Event handlers
		PathScannerFileAddedHandler<T> FileAdded { get; set; }
		PathScannerEntryScanFailedHandler EntryScanFailed { get; set; }
	}

	public abstract class PathScanner<T> : IPathScanner<T> where T : class
	{
		public PathScanResults<T> Results { get; protected set; }
		public abstract void Scan();

		// Event handlers
		public PathScannerFileAddedHandler<T> FileAdded { get; set; }
		public PathScannerEntryScanFailedHandler EntryScanFailed { get; set; }
	}
}
