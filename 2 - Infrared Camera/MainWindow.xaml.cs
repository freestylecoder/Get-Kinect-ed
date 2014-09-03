using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Kinect;

namespace _2___Infrared_Camera {
	public partial class MainWindow : Window {
		KinectSensor Sensor;
		InfraredFrameReader FrameReader;
		WriteableBitmap BitmapToDisplay;

		public MainWindow() {
			Sensor = KinectSensor.GetDefault();
			Sensor.Open();

			FrameReader = Sensor.InfraredFrameSource.OpenReader();

			BitmapToDisplay = new WriteableBitmap(
				FrameReader.InfraredFrameSource.FrameDescription.Width,
				FrameReader.InfraredFrameSource.FrameDescription.Height,
				96.0,
				96.0,
				PixelFormats.Gray16,
				null );

			InitializeComponent();

			this.WindowStyle = System.Windows.WindowStyle.None;
			this.WindowState = System.Windows.WindowState.Maximized;

			Loaded += OpenKinect;
			Closing += CloseKinect;

			KeyDown += CheckForExit;
		}

		private void CheckForExit( object sender, System.Windows.Input.KeyEventArgs e ) {
			if( e.Key == Key.Escape ) {
				App.Current.Shutdown();
			}
		}

		private void OpenKinect( object sender, RoutedEventArgs e ) {
			ScreenImage.Source = BitmapToDisplay;
			FrameReader.FrameArrived += InfraredFrameArrived;
		}

		private void InfraredFrameArrived( object sender, InfraredFrameArrivedEventArgs e ) {
			if( null == e.FrameReference )
				return;

			// If you do not dispose of the frame, you never get another one...
			using( InfraredFrame _InfraredFrame = e.FrameReference.AcquireFrame() ) {
				if( null == _InfraredFrame ) return;

				BitmapToDisplay.Lock();
				_InfraredFrame.CopyFrameDataToIntPtr( 
					BitmapToDisplay.BackBuffer, 
					Convert.ToUInt32(BitmapToDisplay.BackBufferStride * BitmapToDisplay.PixelHeight) );
				BitmapToDisplay.AddDirtyRect( 
					new Int32Rect( 
						0, 
						0, 
						_InfraredFrame.FrameDescription.Width, 
						_InfraredFrame.FrameDescription.Height ) );
				BitmapToDisplay.Unlock();
			}
		}

		private void CloseKinect( object sender, CancelEventArgs e ) {
			if( null != FrameReader ) {
				FrameReader.FrameArrived -= InfraredFrameArrived;
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
