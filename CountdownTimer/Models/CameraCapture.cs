using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace CountdownTimer.Models
{
    public class CameraCapture
    {
        private static MediaFrameReader frameReader;
        private SoftwareBitmap backBuffer;
        private bool taskRunning = false;
        private System.Timers.Timer autoStopCamera;
        private List<SoftwareBitmap> frameQueue;

        // Create and initialze the MediaCapture object.

        public async Task CaptureImageAsync()
        {
            autoStopCamera = new System.Timers.Timer(500);
            autoStopCamera.Elapsed += AutoStopCamera;
            frameQueue = new List<SoftwareBitmap>( );
            var cameraName = "Surface Camera Front";
            var frameSourceGroup = await MediaFrameSourceGroup.FindAllAsync( );
            Debug.WriteLine($"frameSourceGroup = {frameSourceGroup}");
            var cameraGroup = frameSourceGroup.FirstOrDefault(fg => fg.DisplayName == cameraName);
            Debug.WriteLine($"cameraGroup = {cameraGroup}");
            var mediaCapture = new MediaCapture( );
            var settings = new MediaCaptureInitializationSettings( )
            {
                SourceGroup = cameraGroup,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video,
            };

            await mediaCapture.InitializeAsync(settings);

            var exposureSuccess = mediaCapture.VideoDeviceController.Exposure.TrySetAuto(true);
            var brightnessSuccess = mediaCapture.VideoDeviceController.Brightness.TrySetAuto(true);
            var currExposureSuccess = mediaCapture.VideoDeviceController.Exposure.TryGetValue( out double expValue );
            var currBrightnessSuccess = mediaCapture.VideoDeviceController.Brightness.TryGetValue( out double brightValue );
            
            Debug.WriteLine($"exposureSuccess: {exposureSuccess}");
            Debug.WriteLine($"brightnessSuccess: {brightnessSuccess}");
            Debug.WriteLine($"expValue: {expValue}");
            Debug.WriteLine($"brightValue: {brightValue}");

            var sourceInfoId = cameraGroup?.SourceInfos?.FirstOrDefault( )?.Id;

            var mediaFrameSource = (mediaCapture?.FrameSources?.ContainsKey(sourceInfoId) ?? false) ? mediaCapture.FrameSources[sourceInfoId] : null;
            var preferredFormat = mediaFrameSource.SupportedFormats.Where(format =>
            {
                return format.VideoFormat.Width >= 1080;
                //&& format.Subtype == "NV12";

            }).FirstOrDefault( );

            if ( preferredFormat == null )
            {
                // Our desired format is not supported
                return;
            }

            await mediaFrameSource.SetFormatAsync(preferredFormat);

            frameReader?.Dispose( );
            frameReader = await mediaCapture.CreateFrameReaderAsync(mediaFrameSource);

            frameReader.FrameArrived += FrameReaderOnFrameArrived;

            await frameReader.StartAsync( );
            autoStopCamera.Start( );
        }

        protected async void FrameReaderOnFrameArrived( MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            try
            {
                //MediaFrameReference mediaFrameReference = sender.TryAcquireLatestFrame( );

                //if (mediaFrameReference != null)
                //{
                //    var imageDataFrame = mediaFrameReference.BufferMediaFrame;
                //    var imageDataBuffer = imageDataFrame.Buffer;
                //    var imageData = imageDataBuffer.ToArray( );

                //    mediaFrameReference.Dispose( );
                //}

                using (var frame = sender.TryAcquireLatestFrame())
                {
                    if (frame != null)
                    {
                        frameQueue.Add(frame.VideoMediaFrame?.SoftwareBitmap);
                    }
                }


                //frameReader?.Dispose( );
            }

            catch (Exception e)
            {
                await frameReader.StopAsync( );
            }
        }

        private void ProcessFrame(SoftwareBitmap softwareBitmap)
        {
            //var videoMediaFrame = frame?.VideoMediaFrame;
            //var softwareBitmap = videoMediaFrame?.SoftwareBitmap;


            if ( softwareBitmap != null )
            {
                if ( softwareBitmap.BitmapPixelFormat != Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8 ||
                    softwareBitmap.BitmapAlphaMode != Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied )
                {
                    softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                }

                // Swap the processed frame to _backBuffer and dispose of the unused image.
                softwareBitmap = Interlocked.Exchange(ref backBuffer, softwareBitmap);
                softwareBitmap?.Dispose( );

                // Changes to XAML ImageElement must happen on UI thread through Dispatcher
                var task = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
#pragma warning disable VSTHRD101 // Avoid unsupported async delegates
                    async () =>
                    {
                // Don't let two copies of this task run at the same time.
                        if ( taskRunning )
                        {
                            return;
                        }
                        taskRunning = true;

                // Keep draining frames from the backbuffer until the backbuffer is empty.
                        SoftwareBitmap latestBitmap;
                        while ( (latestBitmap = Interlocked.Exchange(ref backBuffer, null)) != null )
                        {
                            //var imageSource = ( SoftwareBitmapSource ) imageElement.Source;
                            //await imageSource.SetBitmapAsync(latestBitmap);
                            //latestBitmap.Dispose( );
                            await SaveSoftwareBitmapToFileAsync(latestBitmap, "C:\\Users\\deanl\\Pictures\\StrangerThingsParty");
                        }

                        taskRunning = false;
                    }
#pragma warning restore VSTHRD101 // Avoid unsupported async delegates
                );
            }
        }

        public static async Task SaveSoftwareBitmapToFileAsync(SoftwareBitmap softwareBitmap, string pathname)
        {
            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync(pathname);
            StorageFile outputFile = await storageFolder.CreateFileAsync($"{DateTime.Now.ToFileTime()}.bmp",
                    Windows.Storage.CreationCollisionOption.ReplaceExisting);

            using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Set the software bitmap
                encoder.SetSoftwareBitmap(softwareBitmap);

                // Set additional encoding parameters, if needed
                //encoder.BitmapTransform.ScaledWidth = 320;
                //encoder.BitmapTransform.ScaledHeight = 240;
                //encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
                //encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.IsThumbnailGenerated = true;

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception err)
                {
                    const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                    switch (err.HResult)
                    {
                        case WINCODEC_ERR_UNSUPPORTEDOPERATION: 
                            // If the encoder does not support writing a thumbnail, then try again
                            // but disable thumbnail generation.
                            encoder.IsThumbnailGenerated = false;
                            break;
                        default:
                            throw;
                    }
                }

                if (encoder.IsThumbnailGenerated == false)
                {
                    await encoder.FlushAsync();
                }
            }
        }

        private void AutoStopCamera(object sender, ElapsedEventArgs args)
        {
            var vidFrame = frameQueue[frameQueue.Count - 1];
            ProcessFrame(vidFrame);
            _ = Task.Run(() => frameReader.StopAsync( ));
            frameQueue.Clear( );
            autoStopCamera.Stop( );
        }
    }
}
