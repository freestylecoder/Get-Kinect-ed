using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace _5___Color_And_Bodies {
	public partial class MainWindow : Window {
		KinectSensor Sensor;
		MultiSourceFrameReader FrameReader;
		WriteableBitmap BitmapToDisplay;

		public MainWindow() {
			Sensor = KinectSensor.GetDefault();
			Sensor.Open();

			FrameReader = Sensor.OpenMultiSourceFrameReader( FrameSourceTypes.Color | FrameSourceTypes.Body );

			BitmapToDisplay = new WriteableBitmap(
				Sensor.ColorFrameSource.FrameDescription.Width,
				Sensor.ColorFrameSource.FrameDescription.Height,
				96.0,
				96.0,
				PixelFormats.Bgra32,
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
			FrameReader.MultiSourceFrameArrived += FramesArrived;
		}

		private void FramesArrived( object sender, MultiSourceFrameArrivedEventArgs e ) {
			if( null == e.FrameReference )
				return;

			// Remove all the old points and clipping lines
			for( int _index = 0; _index < KinectCanvas.Children.Count; ++_index ) {
				if( KinectCanvas.Children[_index] is Image )
					continue;

				KinectCanvas.Children.RemoveAt( _index-- );
			}

			// If you do not dispose of the frame, you never get another one...
			MultiSourceFrame _MultiSourceFrame = e.FrameReference.AcquireFrame();
			using( ColorFrame _ColorFrame = _MultiSourceFrame.ColorFrameReference.AcquireFrame() ) {
				using( BodyFrame _BodyFrame = _MultiSourceFrame.BodyFrameReference.AcquireFrame() ) {
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

					if( null == _BodyFrame ) return;

					Body[] _Bodies = new Body[_BodyFrame.BodyFrameSource.BodyCount];
					_BodyFrame.GetAndRefreshBodyData( _Bodies );

					foreach( Body _Body in _Bodies )
						if( _Body.IsTracked ) {
							foreach( Joint _Joint in _Body.Joints.Values )
								if( TrackingState.Tracked == _Joint.TrackingState ) {
									Ellipse _Ellipse = new Ellipse();
									_Ellipse.Stroke = Brushes.Green;
									_Ellipse.Fill = Brushes.Green;
									_Ellipse.Width = 20;
									_Ellipse.Height = 20;

									ColorSpacePoint _ColorSpacePoint = Sensor.CoordinateMapper.MapCameraPointToColorSpace( _Joint.Position );
									Canvas.SetLeft( _Ellipse, _ColorSpacePoint.X );
									Canvas.SetTop( _Ellipse, _ColorSpacePoint.Y );
									KinectCanvas.Children.Add( _Ellipse );
								}

							if( FrameEdges.Top == ( FrameEdges.Top & _Body.ClippedEdges ) )
								CreateClippingLine( 0, 0, KinectCanvas.ActualWidth, 0 );

							if( FrameEdges.Left == ( FrameEdges.Left & _Body.ClippedEdges ) )
								CreateClippingLine( 0, 0, 0, KinectCanvas.ActualHeight );

							if( FrameEdges.Bottom == ( FrameEdges.Bottom & _Body.ClippedEdges ) )
								CreateClippingLine( 0, KinectCanvas.ActualHeight, KinectCanvas.ActualWidth, KinectCanvas.ActualHeight );

							if( FrameEdges.Right == ( FrameEdges.Right & _Body.ClippedEdges ) )
								CreateClippingLine( KinectCanvas.ActualWidth, 0, KinectCanvas.ActualWidth, KinectCanvas.ActualHeight );
						}
				}

			}
		}

		private void CreateClippingLine( double X1, double Y1, double X2, double Y2 ) {
			Line _Line = new Line();
			_Line.Stroke = Brushes.Red;
			_Line.StrokeThickness = 5;
			_Line.X1 = X1;
			_Line.Y1 = Y1;
			_Line.X2 = X2;
			_Line.Y2 = Y2;

			KinectCanvas.Children.Add( _Line );
		}

		private void CloseKinect( object sender, CancelEventArgs e ) {
			if( null != FrameReader ) {
				FrameReader.MultiSourceFrameArrived -= FramesArrived;
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
