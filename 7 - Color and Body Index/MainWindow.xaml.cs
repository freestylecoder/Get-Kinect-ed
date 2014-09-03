using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace _7___Color_and_Body_Index {
	public partial class MainWindow : Window {
		KinectSensor Sensor;
		MultiSourceFrameReader FrameReader;
		WriteableBitmap BitmapToDisplay;

		public MainWindow() {
			Sensor = KinectSensor.GetDefault();
			Sensor.Open();

			FrameReader = Sensor.OpenMultiSourceFrameReader( FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.BodyIndex );

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
				using( DepthFrame _DepthFrame = _MultiSourceFrame.DepthFrameReference.AcquireFrame() ) {
					using( BodyIndexFrame _BodyIndexFrame = _MultiSourceFrame.BodyIndexFrameReference.AcquireFrame() ) {
						if( null == _ColorFrame ) return;
						if( null == _DepthFrame ) return;
						if( null == _BodyIndexFrame ) return;

						byte[] _BodyIndexValues = new byte[_BodyIndexFrame.FrameDescription.Width * _BodyIndexFrame.FrameDescription.Height * _BodyIndexFrame.FrameDescription.BytesPerPixel];
						_BodyIndexFrame.CopyFrameDataToArray( _BodyIndexValues );

						ushort[] _DepthValues = new ushort[_DepthFrame.FrameDescription.Width * _DepthFrame.FrameDescription.Height];
						_DepthFrame.CopyFrameDataToArray( _DepthValues );

						byte[] _Image = new byte[BitmapToDisplay.BackBufferStride * BitmapToDisplay.PixelHeight];
						_ColorFrame.CopyConvertedFrameDataToArray( _Image, ColorImageFormat.Bgra );

						DepthSpacePoint[] _DepthSpacePoints = new DepthSpacePoint[_ColorFrame.FrameDescription.LengthInPixels];
						Sensor.CoordinateMapper.MapColorFrameToDepthSpace( _DepthValues, _DepthSpacePoints );

						Task.Factory.StartNew( () => {
							Parallel.For( 0, _DepthSpacePoints.Length, ( _Index ) => {
								if( double.IsInfinity( _DepthSpacePoints[_Index].X ) || double.IsInfinity( _DepthSpacePoints[_Index].Y ) ) {
									_Image[_Index * 4 + 3] = 0;
									return;
								}

								int _DepthFrameX = Convert.ToInt32( Math.Round( _DepthSpacePoints[_Index].X ) );
								int _DepthFrameY = Convert.ToInt32( Math.Round( _DepthSpacePoints[_Index].Y ) );

								if( _BodyIndexValues[_DepthFrameY * Sensor.DepthFrameSource.FrameDescription.Width + _DepthFrameX] == 0xFF )
									_Image[_Index * 4 + 3] = 0;
								else
									_Image[_Index * 4 + 3] = 0xFF;
							} );

							Application.Current.Dispatcher.Invoke( () => {
								BitmapToDisplay.WritePixels(
									new Int32Rect( 0, 0, Sensor.ColorFrameSource.FrameDescription.Width, Sensor.ColorFrameSource.FrameDescription.Height ),
									_Image,
									BitmapToDisplay.BackBufferStride,
									0 );
							} );
						} );
					}
				}
			}
		}

		private void CloseKinect( object sender, CancelEventArgs e ) {
			if( null != FrameReader ) {
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
