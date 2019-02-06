using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text; 
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;


namespace DesktopSnapshot
{
	public class WindowsWatcher
	{
		private delegate bool EnumedWindow(IntPtr handleWindow, ArrayList handles);
        
	    [DllImport("user32.dll", SetLastError = true)]
	    internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool EnumWindows(EnumedWindow lpEnumFunc, ArrayList lParam);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll")]
		static extern bool EnumDesktopWindows(IntPtr hDesktop,EnumDesktopWindowsDelegate lpfn, IntPtr lParam);
		private delegate bool EnumDesktopWindowsDelegate(IntPtr hWnd, int lParam);  

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWindowVisible(IntPtr hWnd);
		
		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetWindowRect(IntPtr hWnd, ref Rect lpRect);
		[StructLayout(LayoutKind.Sequential)]
		private struct Rect
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;

			public override string ToString()
			{
				return $"X:{Top}*{Bottom} Y:{Left}*{Right} ";
			}
		}

		[DllImport("user32.dll")] 
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

		private struct WINDOWPLACEMENT
		{
			public int length;
			public int flags;
			public int showCmd;
			public System.Drawing.Point ptMinPosition;
			public System.Drawing.Point ptMaxPosition;
			public System.Drawing.Rectangle rcNormalPosition;

			public override string ToString()
			{
				return $"{(CmdFlags)showCmd} - min {ptMinPosition} - max {ptMaxPosition} - @ {rcNormalPosition.ToString()}";
			}
		}

		enum CmdFlags
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
		SW_RESTORE = 9
		}

	    private const string KnownAppFileName = "KnownApps.json";
	    private const string IgnoreAppFileName = "IgnoredApps.json";

	    public KnownApps knownApps = new KnownApps();
		public KnownApps ignoredApps = new KnownApps();

		public WHandleApps scannedApps;

		public WindowsWatcher()
		{
			using (StreamReader file = File.OpenText(KnownAppFileName))
			{
				knownApps = JsonConvert.DeserializeObject<KnownApps>(file.ReadToEnd());
			}

			using (StreamReader file = File.OpenText(IgnoreAppFileName))
			{
				ignoredApps = JsonConvert.DeserializeObject<KnownApps>(file.ReadToEnd());
			}
		}

		public LocationApps GetWindows()
		{    
            scannedApps = new WHandleApps();

			var windowHandles = new ArrayList();
			EnumedWindow callBackPtr = GetWindowHandle;
			EnumWindows(callBackPtr, windowHandles);

			foreach (object v in windowHandles)
			{
				var className = new StringBuilder(1024);
				var captionTitle = new StringBuilder(1024);
				GetClassName((IntPtr) v, className, className.Capacity);
				GetWindowText((IntPtr)v, captionTitle, captionTitle.Capacity);

				var rct = new Rect();
				GetWindowRect((IntPtr)v, ref rct);

				var placement = new WINDOWPLACEMENT();
				placement.length = Marshal.SizeOf(placement);
				GetWindowPlacement((IntPtr)v, ref placement);

				if (string.IsNullOrWhiteSpace(captionTitle.ToString())) 
					continue;

				if (!IsWindowVisible((IntPtr) v)) 
					continue;

				//if(placement.rcNormalPosition.X ==0 &&
				//   placement.rcNormalPosition.Y ==0 && 
				//   placement.rcNormalPosition.Width==0
				//   && placement.rcNormalPosition.Height==0) 
				//	continue;

				////if (placement.ptMinPosition.X < -1 || placement.ptMinPosition.Y < -1) 
				////	continue;

				if (placement.rcNormalPosition.Height <= 0 && placement.rcNormalPosition.Width <= 0)
					continue;

				Console.WriteLine(className.ToString() + "-" + captionTitle.ToString() + "@" + rct.ToString() + "@@" + placement.ToString() );

			    var a = new WHandleApp(className.ToString(), captionTitle.ToString(), placement.rcNormalPosition);

				List<BaseApp> knownMatches = knownApps.Where(c => Regex.IsMatch(a.Class, c.Class, RegexOptions.IgnoreCase)).ToList();
				knownMatches = knownMatches.Where(c => Regex.IsMatch(a.Caption, c.Caption, RegexOptions.IgnoreCase)).ToList();

				if (knownMatches.Any())
				{
					BaseApp b = knownMatches.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Caption) &&
															!string.IsNullOrWhiteSpace(c.Class));

					if (b == null)
						b = knownMatches.First();

					a.FriendlyName = b.FriendlyName;
				}

			    a.WindowsHandle = (IntPtr)v;

			    List<BaseApp> ignoredMatches =ignoredApps.Where(c => Regex.IsMatch(a.Class, c.Class, RegexOptions.IgnoreCase)).ToList();
			    ignoredMatches = ignoredMatches.Where(c => Regex.IsMatch(a.Caption, c.Caption, RegexOptions.IgnoreCase)).ToList();

			    if (!ignoredMatches.Any())
			    {
			        scannedApps.Add(a);
			    } 
			} 

			return scannedApps.ToLocationApps();     
		}

	    public string SaveSnapshot(SnapShot newSnapshot)
	    {
	        if (!Directory.Exists("snapshots"))
	            Directory.CreateDirectory("snapshots");

	        string newSS = "snapshots\\"+ newSnapshot.Name + ".json";

            if(File.Exists(newSS))
	            File.Move(newSnapshot.Name, newSS+DateTime.Now.ToString("yyyyMMddTHHmmss"));

	        using (TextWriter tw = new StreamWriter(new FileStream(newSS, FileMode.CreateNew)))
	        {
	            JsonSerializer serializer = new JsonSerializer();
	            serializer.Serialize(tw, newSnapshot);
	        }

	        return newSS;
	    }

	    public ObservableCollection<string> GetPreviousSnapShotList()
	    {
	        return new ObservableCollection<string>(Directory.GetFiles("snapshots").ToList());
	    }

	    public void AddIgnoreApp(BaseApp a)
	    {
            ignoredApps.Add(a);
            Save();
	    }

	    public void RemoveIgnoredAdd(BaseApp a)
	    {
	        ignoredApps.Remove(a);
            Save();
	    }

	    public void RestoreSnapShot(string snapshotFile)
	    {
            var savedSnapshot = new SnapShot();
	        using (StreamReader file = File.OpenText(snapshotFile))
	        {
	            savedSnapshot = JsonConvert.DeserializeObject<SnapShot>(file.ReadToEnd());
	        }

	        foreach (LocationApp appToRestore in savedSnapshot.RestoreApps)
	        {
                List<WHandleApp> foundWindows = scannedApps
	                .Where(c => Regex.IsMatch(c.Caption, appToRestore.Caption, RegexOptions.IgnoreCase)).ToList();

	            foundWindows = foundWindows.Where(c => Regex.IsMatch(c.Class, appToRestore.Class, RegexOptions.IgnorePatternWhitespace)).ToList();

	            if (foundWindows.Any())
	            {
	                MoveWindow(foundWindows[0].WindowsHandle, appToRestore.Position.X, appToRestore.Position.Y,
	                    appToRestore.Position.Width, appToRestore.Position.Height, true);
	            }
	        }
	    }

	    private void Save()
	    {
	        if(File.Exists(KnownAppFileName))
                File.Move(KnownAppFileName, KnownAppFileName+DateTime.Now.ToString("yyyyMMddTHHmmss"));

	        using (TextWriter tw = new StreamWriter(new FileStream(KnownAppFileName, FileMode.CreateNew)))
	        {
	            JsonSerializer serializer = new JsonSerializer();
	            serializer.Serialize(tw, knownApps);
	        }
            
	        if(File.Exists(KnownAppFileName))
	            File.Move(KnownAppFileName, IgnoreAppFileName+DateTime.Now.ToString("yyyyMMddTHHmmss"));

	        using (TextWriter tw = new StreamWriter(new FileStream(IgnoreAppFileName, FileMode.CreateNew)))
	        {
	            JsonSerializer serializer = new JsonSerializer();
	            serializer.Serialize(tw, ignoredApps);
	        }
	    }

		private static bool GetWindowHandle(IntPtr windowHandle, ArrayList windowHandles)
		{
			windowHandles.Add(windowHandle);
			return true;
		}
	}
}
