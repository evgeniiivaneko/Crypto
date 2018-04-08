using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;

namespace Crypto.Audio
{
    public class WavFile
    {
        #region Public Properties

        public string PathAudioFile { get; set; }
        public TimeSpan Duration { get { return duration; } }
        public const int SUBCHUNK_1_ID = 544501094;

        #endregion

        #region Private Properties

        private const int ticksInSecond = 10000000;
        private TimeSpan duration;

        #endregion

        #region AudioDate

        private List<float> floatAudioBuffer = new List<float>();
        private List<Int16> shortAudioBuffer = new List<short>();

        #endregion

        #region Ctor

        public WavFile(string _path)
        {
            PathAudioFile = _path;
            ReadWavFile(_path);
        }

        #endregion

        #region Public Methods

        public float[] GetFloatBuffer()
        {
            return floatAudioBuffer.ToArray();
        }

        #endregion

        #region Private Methods

        private void ReadWavFile(string filename)
        {
            try
            {
                using (FileStream fs = File.Open(filename, FileMode.Open))
                {
                    BinaryReader reader = new BinaryReader(fs);
                    //RIFF
                    int chunkID = reader.ReadInt32();
                    int fileSize = reader.ReadInt32();
                    int riffType = reader.ReadInt32();
                    //Format
                    int fmtID;
                    long _position = reader.BaseStream.Position;
                    while (_position != reader.BaseStream.Length - 1)
                    {
                        reader.BaseStream.Position = _position;
                        int _fmtID = reader.ReadInt32();
                        if (_fmtID == SUBCHUNK_1_ID)
                        {
                            fmtID = _fmtID;
                            break;
                        }
                        _position++;
                    }

                    int fmtSize = reader.ReadInt32();
                    int fmtCode = reader.ReadInt16();
                    int channels = reader.ReadInt16();
                    int sampleRate = reader.ReadInt32();
                    int byteRate = reader.ReadInt32();
                    int fmtBlockAlign = reader.ReadInt16();
                    int bitDepth = reader.ReadInt16();
                    if (fmtSize == 18)
                    {
                        int fmtExtraSize = reader.ReadInt16();
                        reader.ReadBytes(fmtExtraSize);
                    }

                    int dataID = reader.ReadInt32();
                    int dataSize = reader.ReadInt32();
                    byte[] byteArray = reader.ReadBytes(dataSize);
                    int bytesInSample = bitDepth / 8;
                    int sampleAmount = dataSize / bytesInSample;
                    short[] tmpArray = null;
                    switch (bitDepth)
                    {
                        case 16:
                            Int16[] shortArray = new short[sampleAmount];
                            System.Buffer.BlockCopy(byteArray, 0, shortArray, 0, dataSize);
                            IEnumerable<short> tempShort = from i in shortArray
                                                           select i;
                            tmpArray = tempShort.ToArray();
                            break;
                        default:
                            return;
                    }

                    floatAudioBuffer.AddRange(tmpArray.Select(x => x / (float)Int16.MaxValue));
                    shortAudioBuffer.AddRange(tmpArray);
                    DeterminateDurationTrack(channels, sampleRate);
                }
            }
            catch
            {
                Debug.WriteLine("FileError");
                return;
            }
        }

        private TimeSpan DeterminateDurationTrack(int channel, int sampleRate)
        {
            long _duration = (long)(((double)floatAudioBuffer.Count / sampleRate / channel) * ticksInSecond);
            return TimeSpan.FromTicks(_duration);
        }
        #endregion
    }
}
