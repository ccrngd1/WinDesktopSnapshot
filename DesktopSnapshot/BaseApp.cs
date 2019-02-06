using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;

namespace DesktopSnapshot
{
	public class BaseApp : INotifyPropertyChanged
	{
	    private string friendly;
	    public string FriendlyName
	    {
	        get { return friendly; }
	        set
	        {
	            if (value != friendly)
	            {
	                friendly = value;
	                OnPropertyChanged("FriendlyName");
	            }
	        }
	    }

        private string _class;
	    public string Class
	    {
	        get { return _class; }
	        set
	        {
	            if (value != _class)
	            {
	                _class = value;
	                OnPropertyChanged("Class");
	            }
	        }
	    }

	    private string caption;
	    public string Caption
	    {
	        get { return caption; }
	        set
	        {
	            if (value != caption)
	            {
	                caption = value;
	                OnPropertyChanged("Caption");
	            }
	        }
	    }
        
	    public event PropertyChangedEventHandler PropertyChanged;

	    protected void OnPropertyChanged(string propertyName)
	    {
	        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	    }

	    public override string ToString()
	    {
	        var sb = new StringBuilder(FriendlyName?.Length??0 + Class.Length + Caption.Length + 10);

	        if (!string.IsNullOrWhiteSpace(FriendlyName))
	        {
	            sb.Append(FriendlyName);
	        }

	        sb.Append("[").Append(Caption).Append("]").Append("(").Append(Class).Append(")");

	        return sb.ToString();
	    }

	    public BaseApp(){}

		public BaseApp(string className, string captionString)
		{
			Class = className;
			Caption = captionString;
		}

		public BaseApp(string className, string captionString, string friendlyname)
		:this(className, captionString)
		{
			FriendlyName = friendlyname;
		}
	}

    public class WHandleApp : LocationApp
    {
        public IntPtr WindowsHandle;

        public WHandleApp(){}

        public WHandleApp(string className, string captionString, Rectangle windowPosition)
            :base(className, captionString, windowPosition)
        {
            Position = windowPosition;
        }

        public WHandleApp(string className, string captionString, string friendlyname, Rectangle windowPosition)
            :base(className, captionString, friendlyname, windowPosition)
        {
            Position = windowPosition;
        }
    }

	public class LocationApp : BaseApp
	{
	    private Rectangle position;
	    public Rectangle Position
	    {
	        get { return position; }
	        set
	        {
	            if (value != position)
	            {
	                position = value;
	                OnPropertyChanged("Position");
	            }
	        }
	    } 

		public LocationApp(){}

		public LocationApp(string className, string captionString, Rectangle windowPosition)
		:base(className, captionString)
		{
			Position = windowPosition;
		}

		public LocationApp(string className, string captionString, string friendlyname, Rectangle windowPosition)
			:base(className, captionString, friendlyname)
		{
			Position = windowPosition;
		}
	}

    public class LocationApps : ObservableCollection<LocationApp>
    {
        public LocationApps(){}

        public LocationApps(List<LocationApp> apps)
        :base(apps)
        {
            
        }
    }

	public class KnownApps : ObservableCollection<BaseApp>{ }

    public class WHandleApps : ObservableCollection<WHandleApp>
    {
        public LocationApps ToLocationApps()
        {
            return new LocationApps(this.Select(c => (LocationApp) c).ToList());
        }
    }
    

    public class SnapShot
    {
        public List<System.Windows.Forms.Screen> Monitors;
        public LocationApps ScannedApps;
        public LocationApps RestoreApps; 
        public string Name;

        public SnapShot()
        {
            Monitors = System.Windows.Forms.Screen.AllScreens.ToList();
        }

        public SnapShot(string name, LocationApps toRestore, LocationApps currentScanned)
        {
            Name = name;
            ScannedApps = currentScanned;
            RestoreApps = toRestore;
        }
    }

    public class Snapshots : ObservableCollection<SnapShot>{ }
}
