using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Kinect;

namespace _1___Color_Camera {
	public partial class MainWindow : Window {
		KinectSensor Sensor;
		ColorFrameReader FrameReader;
		WriteableBitmap BitmapToDisplay;

		public MainWindow() {
			Sensor = KinectSensor.GetDefault();
			Sensor.Open();

			FrameReader = Sensor.ColorFrameSource.OpenReader();

			BitmapToDisplay = new WriteableBitmap(
				FrameReader.ColorFrameSource.FrameDescription.Width,
				FrameReader.ColorFrameSource.FrameDescription.Height,
				96.0,
				96.0,
				PixelFormats.Bgra32,
				null );

			this.WindowStyle = System.Windows.WindowStyle.None;
			this.WindowState = System.Windows.WindowState.Maximized;

			Loaded += OpenKinect;
			Closing += CloseKinect;
			KeyDown += CheckForExit;

			InitializeComponent();
		}

		//  Let's me exit with the escape key
		private void CheckForExit( object sender, System.Windows.Input.KeyEventArgs e ) {
			if( e.Key == Key.Escape ) {
				App.Current.Shutdown();
			}
		}

		private void OpenKinect( object sender, RoutedEventArgs e ) {
			ScreenImage.Source = BitmapToDisplay;
			FrameReader.FrameArrived += ColorFrameArrived;
		}

		private void ColorFrameArrived( object sender, ColorFrameArrivedEventArgs e ) {
			if( null == e.FrameReference ) return;

			// If you do not dispose of the frame, you never get another one...
			using( ColorFrame _ColorFrame = e.FrameReference.AcquireFrame() ) {
				if( null == _ColorFrame ) return;

				BitmapToDisplay.Lock();
				_ColorFrame.CopyConvertedFrameDataToIntPtr( 
					BitmapToDisplay.BackBuffer, 
					Convert.ToUInt32( BitmapToDisplay.BackBufferStride * BitmapToDisplay.PixelHeight ), 
					ColorImageFormat.Bgra );
				BitmapToDisplay.AddDirtyRect( 
					new Int32Rect( 
						0, 
						0, 
						_ColorFrame.FrameDescription.Width, 
						_ColorFrame.FrameDescription.Height ) );
				BitmapToDisplay.Unlock();
			}
		}

		private void CloseKinect( object sender, CancelEventArgs e ) {
			if( null != FrameReader ) {
				FrameReader.FrameArrived -= ColorFrameArrived;
				FrameReader.Dispose();
				FrameReader = null;
			}

			if( null != Sensor ) {
				Sensor.Close();
				Sensor = null;
			}
		}
	}
}
