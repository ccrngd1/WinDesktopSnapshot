using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopSnapshot
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
	    private const int scaleFactor = 50;
		private WindowsWatcher winWatcher; 
        
	    public event PropertyChangedEventHandler PropertyChanged;

	    protected void OnPropertyChanged(string propertyName)
	    {
	        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	    }

	    private LocationApps scannedApps;
	    public LocationApps ScannedApps
	    {
	        get { return scannedApps; }
	        set
	        { 
	            scannedApps = value;
	            OnPropertyChanged("ScannedApps"); 
	        }
	    } 

	    private KnownApps ignoredScannedApps;
	    public KnownApps IgnoredScannedApps
	    {
	        get { return ignoredScannedApps; }
	        set
	        { 
	            ignoredScannedApps = value;
	            OnPropertyChanged("IgnoredScannedApps"); 
	        }
	    }

	    private LocationApps restoreScannedApps;
	    public LocationApps RestoreScannedApps
	    {
	        get { return restoreScannedApps; }
	        set
	        { 
	            restoreScannedApps = value;
	            OnPropertyChanged("RestoreScannedApps"); 
	        }
	    }


	    private LocationApp selectedScannedApp;
	    public LocationApp SelectedScannedApp
	    {
	        get { return selectedScannedApp; }
	        set
	        { 
	            selectedScannedApp = value;
	            OnPropertyChanged("SelectedScannedApp"); 
	        }
	    }

	    private LocationApp selectedIgnoredApp;
	    public LocationApp SelectedIgnoredApp
	    {
	        get { return selectedIgnoredApp; }
	        set
	        { 
	            selectedIgnoredApp = value;
	            OnPropertyChanged("SelectedIgnoredApp"); 
	        }
	    }

	    private LocationApp selectedRestoreApp;
	    public LocationApp SelectedRestoreApp
	    {
	        get { return selectedRestoreApp; }
	        set
	        { 
	            selectedRestoreApp = value;
	            OnPropertyChanged("SelectedRestoreApp"); 
	        }
	    }



	    private ObservableCollection<string> previousSnapshots;
	    public ObservableCollection<string> PreviousSnapshots
	    {
	        get { return previousSnapshots; }
	        set
	        {
	            previousSnapshots = value;
	            OnPropertyChanged("PreviousSnapshots"); 
	        }
	    }

	    private string selectedPreviousSnapshot;
	    public string SelectedPreviousSnapshot
	    {
	        get { return selectedPreviousSnapshot; }
	        set
	        {
	            selectedPreviousSnapshot = value;
	            OnPropertyChanged("selectedPreviousSnapshot"); 
	        }
	    }

	    private List<System.Windows.Forms.Screen> screens;

	    public MainWindow()
		{
			InitializeComponent();
		    DataContext = this;

			winWatcher = new WindowsWatcher();

		    ScannedApps = winWatcher.GetWindows();

		    IgnoredScannedApps = winWatcher.ignoredApps;
            RestoreScannedApps = new LocationApps();
		    PreviousSnapshots = winWatcher.GetPreviousSnapShotList();

		    screens = System.Windows.Forms.Screen.AllScreens.ToList();

            RefreshDwgs();
		} 

        private void RefreshDwgs()
        {
            if (double.IsNaN(spCanvasHolder.ActualWidth) || double.IsNaN(spCanvasHolder.ActualHeight)) return;
            if (spCanvasHolder.ActualWidth<=0 || spCanvasHolder.ActualHeight<=0) return;

            var sumY = screens.Sum(c =>Math.Abs(c.Bounds.Height));
            var sumX = screens.Sum(c => Math.Abs(c.Bounds.Width));

            int minY = screens.Select(c => c.Bounds.Y).Min()/scaleFactor;
            int minX = screens.Select(c => c.Bounds.X).Min()/scaleFactor;
            
            var bm = new Bitmap((int)spCanvasHolder.ActualWidth, (int)spCanvasHolder.ActualHeight);
            using (Graphics gr = Graphics.FromImage(bm))
            {
                gr.SmoothingMode = SmoothingMode.AntiAlias;

                foreach (Screen sc in screens)
                {
                    var scaledX = sc.Bounds.X / scaleFactor;
                    var scaledY = sc.Bounds.Y / scaleFactor;
                    var scaledH = sc.Bounds.Height / scaleFactor;
                    var scaledW = sc.Bounds.Width / scaleFactor;

                    var rect = new Rectangle(scaledX - minX, scaledY - minY, scaledW, scaledH);
                    gr.FillRectangle(System.Drawing.Brushes.LightGreen, rect);
                    using (System.Drawing.Pen thick_pen = new System.Drawing.Pen(System.Drawing.Color.Blue, 5))
                    {
                        gr.DrawRectangle(thick_pen, rect);
                    }
                }

                cnvsMonitorDisplay.Source = BitmapToImageSource(bm);
            }
        }

        private BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

	    private void LbScanned_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	    {
            if(e.AddedItems.Count>0)
	            SelectedScannedApp = (LocationApp) e.AddedItems[0];
            else
                SelectedScannedApp = null;
	    }

	    private void LbSelected_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	    {
	        if(e.AddedItems.Count>0)
	            SelectedRestoreApp = (LocationApp) e.AddedItems[0];
	        else
	            SelectedRestoreApp = null;
	    }

	    private void LbIgnored_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	    {
	        if(e.AddedItems.Count>0)
	            SelectedIgnoredApp = (LocationApp) e.AddedItems[0];
	        else
	            SelectedIgnoredApp = null;
	    }

	    private void LbSnapshots_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	    {
	        if(e.AddedItems.Count>0)
	            SelectedPreviousSnapshot = e.AddedItems[0].ToString();
	        else
	            SelectedPreviousSnapshot = null;
	    }
        
	    private void btnSnapshot_OnClick(object sender, RoutedEventArgs e)
	    {
	        var a = new SnapShot(DateTime.Now.ToString("yyyyMMddTHHmmss"),restoreScannedApps, scannedApps);
	        var ssName = winWatcher.SaveSnapshot(a);

            PreviousSnapshots.Add(ssName);
	    }

	    private void btnSaveApp_OnClick(object sender, RoutedEventArgs e)
	    {
	        RestoreScannedApps.Add(SelectedScannedApp);
	    }

	    private void BtnIgnoreApp_OnClick(object sender, RoutedEventArgs e)
	    {
	        winWatcher.AddIgnoreApp(SelectedScannedApp);
	    }

	    private void btnRemoveSaveApp_OnClick(object sender, RoutedEventArgs e)
	    {
	        RestoreScannedApps.Remove(SelectedRestoreApp);
	        SelectedRestoreApp = null;
	    }

	    private void btnRemoveIgnoreApp_OnClick(object sender, RoutedEventArgs e)
	    {
	        winWatcher.RemoveIgnoredAdd(SelectedIgnoredApp);
	        SelectedIgnoredApp = null;
	    }

	    private void BtnRestoreSnapshot_OnClick(object sender, RoutedEventArgs e)
	    {
            winWatcher.RestoreSnapShot(SelectedPreviousSnapshot);
	    }

	    private void btnRescanApp_OnClick(object sender, RoutedEventArgs e)
	    {
	        ScannedApps = winWatcher.GetWindows();
	    }

	    private void MainWindow_OnLocationChanged(object sender, EventArgs e)
	    {
	        RefreshDwgs();
	    }

	    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
	    {
	        RefreshDwgs();
	    }
	}
}
