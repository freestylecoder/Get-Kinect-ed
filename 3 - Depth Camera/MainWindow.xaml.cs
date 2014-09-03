using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Kinect;

namespace _3___Depth_Camera {
	public partial class MainWindow : Window {
		KinectSensor Sensor;
		DepthFrameReader FrameReader;
		WriteableBitmap BitmapToDisplay;

		public MainWindow() {
			Sensor = KinectSensor.GetDefault();
			Sensor.Open();

			FrameReader = Sensor.DepthFrameSource.OpenReader();

			BitmapToDisplay = new WriteableBitmap(
				FrameReader.DepthFrameSource.FrameDescription.Width,
				FrameReader.DepthFrameSource.FrameDescription.Height,
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
			CameraImage.Source = BitmapToDisplay;
			FrameReader.FrameArrived += DepthFrameArrived;
		}

		private void DepthFrameArrived( object sender, DepthFrameArrivedEventArgs e ) {
			if( null == e.FrameReference )
				return;

			// If you do not dispose of the frame, you never get another one...
			using( DepthFrame _DepthFrame = e.FrameReference.AcquireFrame() ) {
				if( null == _DepthFrame ) return;

				BitmapToDisplay.Lock();
				_DepthFrame.CopyFrameDataToIntPtr( 
					BitmapToDisplay.BackBuffer, 
					Convert.ToUInt32(BitmapToDisplay.BackBufferStride * BitmapToDisplay.PixelHeight) );
				BitmapToDisplay.AddDirtyRect( 
					new Int32Rect( 
						0, 
						0, 
						_DepthFrame.FrameDescription.Width, 
						_DepthFrame.FrameDescription.Height ) );
				BitmapToDisplay.Unlock();
			}
		}

		private void CloseKinect( object sender, CancelEventArgs e ) {
			if( null != FrameReader ) {
				FrameReader.FrameArrived -= DepthFrameArrived;
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
