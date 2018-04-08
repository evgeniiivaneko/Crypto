using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace Crypto.Audio
{
    public class AudioRecorder
    {
        #region Private Properties
        private const string DEFAULT_AUDIO_FILENAME = "noize";
        private string _fileName;
        private MediaCapture _mediaCapture;
        private InMemoryRandomAccessStream _memoryBuffer;
        #endregion

        #region Public Properties
        public bool IsRecording { get; set; }
        #endregion

        #region Public Methods
        public async void Record()
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Recording already in progress!");
            }

            //Init();
            //await

            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings
            {
                StreamingCaptureMode = StreamingCaptureMode.Audio
            };

            _mediaCapture = new MediaCapture();
            await _mediaCapture.InitializeAsync(settings);
            await _mediaCapture.StartRecordToStreamAsync(MediaEncodingProfile.CreateWav(AudioEncodingQuality.Auto), _memoryBuffer);
            IsRecording = true;
        }

        public async void StopRecoding()
        {
            await _mediaCapture.StopRecordAsync();



            IsRecording = false;
        }

        public async Task PlayFromDisk(CoreDispatcher dispatcher)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                MediaElement playbackMediaElement = new MediaElement();
                StorageFolder storageFolder = Package.Current.InstalledLocation;
                StorageFile storageFile = await storageFolder.GetFileAsync(this._fileName);
                IRandomAccessStream stream = await storageFile.OpenAsync(FileAccessMode.Read);

                playbackMediaElement.SetSource(stream, storageFile.FileType);
                playbackMediaElement.Play();
            });
        }

        public async Task<StorageFile> GetStorageFile(CoreDispatcher dispatcher)
        {
            StorageFolder storageFolder = Package.Current.InstalledLocation;
            StorageFile storageFile = await storageFolder.GetFileAsync(this._fileName);
            return storageFile;
        }

        public void Play()
        {
            MediaElement playbackMediaElement = new MediaElement();

            playbackMediaElement.SetSource(_memoryBuffer, "WAV");
            playbackMediaElement.Play();
        }
        #endregion

        #region Private Methods

        private void Initialized()
        {
            if (_memoryBuffer != null)
            {
                _memoryBuffer.Dispose();
            }

            _memoryBuffer = new InMemoryRandomAccessStream();

            if (_mediaCapture != null)
            {
                _mediaCapture.Dispose();
            }

            this._fileName = DEFAULT_AUDIO_FILENAME + DateTime.Now + ".wav";
        }

        private async void SaveAudioToFile()
        {
            try
            {
                IRandomAccessStream audioStream = _memoryBuffer.CloneStream();
                StorageFolder storageFolder = Package.Current.InstalledLocation;

                StorageFile storageFile = await storageFolder.CreateFileAsync(DEFAULT_AUDIO_FILENAME + DateTime.Now + ".wav", CreationCollisionOption.GenerateUniqueName);
                this._fileName = storageFile.Name;

                using (IRandomAccessStream fileStream = await storageFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await RandomAccessStream.CopyAndCloseAsync(audioStream.GetInputStreamAt(0), fileStream.GetOutputStreamAt(0));
                    await audioStream.FlushAsync();
                    audioStream.Dispose();
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task DeleteExistingFile()
        {
            try
            {
                StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFile existingFile = await storageFolder.GetFileAsync(this._fileName);
                await existingFile.DeleteAsync();
            }
            catch (FileNotFoundException)
            {

            }
        }

        #endregion
    }
}
