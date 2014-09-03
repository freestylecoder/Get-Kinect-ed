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
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;

namespace _8___Speech {
	public partial class MainWindow : Window {
		KinectSensor Sensor;
		AudioBeamFrameReader AudioReader;
		MultiSourceFrameReader FrameReader;
		WriteableBitmap BitmapToDisplay;
		SpeechRecognitionEngine SpeechEngine;
		SolidColorBrush JointColor = Brushes.Green;
		KinectAudioStream ConvertedStream = null;

		public MainWindow() {
			Sensor = KinectSensor.GetDefault();
			Sensor.Open();

			FrameReader = Sensor.OpenMultiSourceFrameReader( FrameSourceTypes.Color | FrameSourceTypes.Body );
			AudioReader = Sensor.AudioSource.OpenReader();

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
			// Find the speech recognizer that is installed with the Kinect
			foreach( RecognizerInfo _Recognizer in SpeechRecognitionEngine.InstalledRecognizers() ) {
				if( _Recognizer.AdditionalInfo.Keys.Contains( "Kinect" ) )
					if( _Recognizer.AdditionalInfo["Kinect"].Equals( "True", StringComparison.OrdinalIgnoreCase ) )
						if( _Recognizer.Culture.Name.Equals( "en-US", StringComparison.OrdinalIgnoreCase ) )
							SpeechEngine = new SpeechRecognitionEngine( _Recognizer.Id );
			}

			// Create my vocabulary
			if( SpeechEngine != null ) {
                Choices _Colors = new Choices();
				_Colors.Add( new SemanticResultValue( "red", "RED" ) );
				_Colors.Add( new SemanticResultValue( "green", "GREEN" ) );
				_Colors.Add( new SemanticResultValue( "blue", "BLUE" ) );
				_Colors.Add( new SemanticResultValue( "yellow", "YELLOW" ) );
				_Colors.Add( new SemanticResultValue( "pink", "PINK" ) );
				_Colors.Add( new SemanticResultValue( "fuschia", "PINK" ) );
				_Colors.Add( new SemanticResultValue( "exit", "EXIT" ) );
				_Colors.Add( new SemanticResultValue( "quit", "EXIT" ) );

				GrammarBuilder _GrammarBuilder = new GrammarBuilder();
				_GrammarBuilder.Append( _Colors );

				// If you use the xml file, this is where you would pass in the XML
				// Grammar _Grammar = new Grammar( PATH_TO_XML_FILE );
				Grammar _Grammar = new Grammar( _GrammarBuilder );
				SpeechEngine.LoadGrammar( _Grammar );

				SpeechEngine.SpeechRecognized += SpeechRecognized;

				// The input stream is a mono 32-bit IEEE floating point PCM stream sampled at 16 kHz. Typical PCM values will be between -1 and +1.
				ConvertedStream = new KinectAudioStream( Sensor.AudioSource.AudioBeams[0].OpenInputStream() );
				ConvertedStream.SpeechActive = true;

				SpeechEngine.SetInputToAudioStream( ConvertedStream, new SpeechAudioFormatInfo( EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null ) );
				SpeechEngine.RecognizeAsync( RecognizeMode.Multiple );
			}

			CameraImage.Source = BitmapToDisplay;
			FrameReader.MultiSourceFrameArrived += FramesArrived;
		}

		private void SpeechRecognized( object sender, SpeechRecognizedEventArgs e ) {
			// Speech utterance confidence below which we treat speech as if it hadn't been heard
			const double ConfidenceThreshold = 0.3;
			if( e.Result.Confidence >= ConfidenceThreshold ) {
				switch( e.Result.Semantics.Value.ToString() ) {
					case "BLUE":
						JointColor = Brushes.Blue;
						break;

					case "RED":
						JointColor = Brushes.Red;
						break;

					case "GREEN":
						JointColor = Brushes.Green;
						break;

					case "YELLOW":
						JointColor = Brushes.Yellow;
						break;

					case "PINK":
						JointColor = Brushes.Fuchsia;
						break;

					case "EXIT":
						Application.Current.Shutdown();
						break;
				}
			}
		}

		private byte ClipToByte( int p_ValueToClip ) {
			return Convert.ToByte( ( p_ValueToClip < byte.MinValue ) ? byte.MinValue : ( ( p_ValueToClip > byte.MaxValue ) ? byte.MaxValue : p_ValueToClip ) );
		}

		private void FramesArrived( object sender, MultiSourceFrameArrivedEventArgs e ) {
			if( null == e.FrameReference ) return;

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

					if( null == _BodyFrame ) return;

					Body[] _Bodies = new Body[_BodyFrame.BodyFrameSource.BodyCount];
					_BodyFrame.GetAndRefreshBodyData( _Bodies );

					foreach( Body _Body in _Bodies )
						if( _Body.IsTracked ) {
							foreach( Joint _Joint in _Body.Joints.Values )
								if( TrackingState.Tracked == _Joint.TrackingState ) {
									Ellipse _Ellipse = new Ellipse();
									_Ellipse.Stroke = JointColor;
									_Ellipse.Fill = JointColor;
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
			ConvertedStream.SpeechActive = true;
			SpeechEngine.RecognizeAsyncStop();

			if( null != AudioReader ) {
				AudioReader.Dispose();
				AudioReader = null;
			}

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
