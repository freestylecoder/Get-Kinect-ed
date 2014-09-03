using System;
using System.ComponentModel;
using System.Threading.Tasks;
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
				PixelFormats.Bgr32,
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
			//FrameReader.FrameArrived += ColorFrameArrived;
			CompositionTarget.Rendering += CompositionTarget_Rendering;
		}

		void CompositionTarget_Rendering( object sender, EventArgs e ) {
			using( ColorFrame _ColorFrame = FrameReader.AcquireLatestFrame() ) {
				if( null == _ColorFrame )
					return;

				byte[] _InputImage = new byte[_ColorFrame.FrameDescription.LengthInPixels * _ColorFrame.FrameDescription.BytesPerPixel];
				byte[] _OutputImage = new byte[BitmapToDisplay.BackBufferStride * BitmapToDisplay.PixelHeight];
				_ColorFrame.CopyRawFrameDataToArray( _InputImage );

				ParallelOptions _ParallelOptions = new ParallelOptions();
				_ParallelOptions.MaxDegreeOfParallelism = 4;

				Parallel.For( 0, Sensor.ColorFrameSource.FrameDescription.LengthInPixels / 2, _ParallelOptions, ( _Index ) => {
					// See http://msdn.microsoft.com/en-us/library/windows/desktop/dd206750(v=vs.85).aspx#converting422yuvto444yuv
					int _Y0 = _InputImage[( _Index << 2 ) + 0] - 16;
					int _U  = _InputImage[( _Index << 2 ) + 1] - 128;
					int _Y1 = _InputImage[( _Index << 2 ) + 2] - 16;
					int _V  = _InputImage[( _Index << 2 ) + 3] - 128;

					byte _R = ClipToByte( ( 298 * _Y0 + 409 * _V + 128 ) >> 8 );
					byte _G = ClipToByte( ( 298 * _Y0 - 100 * _U - 208 * _V + 128 ) >> 8 );
					byte _B = ClipToByte( ( 298 * _Y0 + 516 * _U + 128 ) >> 8 );

					_OutputImage[( _Index << 3 ) + 0] = _B;
					_OutputImage[( _Index << 3 ) + 1] = _G;
					_OutputImage[( _Index << 3 ) + 2] = _R;
					_OutputImage[( _Index << 3 ) + 3] = 0xFF; // A

					_R = ClipToByte( ( 298 * _Y1 + 409 * _V + 128 ) >> 8 );
					_G = ClipToByte( ( 298 * _Y1 - 100 * _U - 208 * _V + 128 ) >> 8 );
					_B = ClipToByte( ( 298 * _Y1 + 516 * _U + 128 ) >> 8 );

					_OutputImage[( _Index << 3 ) + 4] = _B;
					_OutputImage[( _Index << 3 ) + 5] = _G;
					_OutputImage[( _Index << 3 ) + 6] = _R;
					_OutputImage[( _Index << 3 ) + 7] = 0xFF;
				} );

				BitmapToDisplay.WritePixels(
					new Int32Rect( 0, 0, Sensor.ColorFrameSource.FrameDescription.Width, Sensor.ColorFrameSource.FrameDescription.Height ),
					_OutputImage,
					BitmapToDisplay.BackBufferStride,
					0 );
			}
		}

		private byte ClipToByte( int p_ValueToClip ) {
			return Convert.ToByte( ( p_ValueToClip < byte.MinValue ) ? byte.MinValue : ( ( p_ValueToClip > byte.MaxValue ) ? byte.MaxValue : p_ValueToClip ) );
		}

		private void ColorFrameArrived( object sender, ColorFrameArrivedEventArgs e ) {
			if( null == e.FrameReference )
				return;

			// If you do not dispose of the frame, you never get another one...
			using( ColorFrame _ColorFrame = e.FrameReference.AcquireFrame() ) {
				if( null == _ColorFrame )
					return;

				byte[] _InputImage = new byte[_ColorFrame.FrameDescription.LengthInPixels * _ColorFrame.FrameDescription.BytesPerPixel];
				byte[] _OutputImage = new byte[BitmapToDisplay.BackBufferStride * BitmapToDisplay.PixelHeight];
				_ColorFrame.CopyRawFrameDataToArray( _InputImage );

				Task.Factory.StartNew( () => {
					ParallelOptions _ParallelOptions = new ParallelOptions();
					_ParallelOptions.MaxDegreeOfParallelism = 4;

					Parallel.For( 0, Sensor.ColorFrameSource.FrameDescription.LengthInPixels / 2, _ParallelOptions, ( _Index ) => {
						// See http://msdn.microsoft.com/en-us/library/windows/desktop/dd206750(v=vs.85).aspx
						int _Y0 = _InputImage[( _Index << 2 ) + 0] - 16;
						int _U  = _InputImage[( _Index << 2 ) + 1] - 128;
						int _Y1 = _InputImage[( _Index << 2 ) + 2] - 16;
						int _V  = _InputImage[( _Index << 2 ) + 3] - 128;

						byte _R = ClipToByte( ( 298 * _Y0 + 409 * _V + 128 ) >> 8 );
						byte _G = ClipToByte( ( 298 * _Y0 - 100 * _U - 208 * _V + 128 ) >> 8 );
						byte _B = ClipToByte( ( 298 * _Y0 + 516 * _U + 128 ) >> 8 );

						_OutputImage[( _Index << 3 ) + 0] = _B;
						_OutputImage[( _Index << 3 ) + 1] = _G;
						_OutputImage[( _Index << 3 ) + 2] = _R;
						_OutputImage[( _Index << 3 ) + 3] = 0xFF; // A

						_R = ClipToByte( ( 298 * _Y1 + 409 * _V + 128 ) >> 8 );
						_G = ClipToByte( ( 298 * _Y1 - 100 * _U - 208 * _V + 128 ) >> 8 );
						_B = ClipToByte( ( 298 * _Y1 + 516 * _U + 128 ) >> 8 );

						_OutputImage[( _Index << 3 ) + 4] = _B;
						_OutputImage[( _Index << 3 ) + 5] = _G;
						_OutputImage[( _Index << 3 ) + 6] = _R;
						_OutputImage[( _Index << 3 ) + 7] = 0xFF;
					} );

					Application.Current.Dispatcher.Invoke( () => {
						BitmapToDisplay.WritePixels(
							new Int32Rect( 0, 0, Sensor.ColorFrameSource.FrameDescription.Width, Sensor.ColorFrameSource.FrameDescription.Height ),
							_OutputImage,
							BitmapToDisplay.BackBufferStride,
							0 );
					} );
				} );
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
