using System;
using System.IO;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace sidplay
{
    public class MonoSidPlayer : IDisposable
    {
        private const int MULTIPLIER_SHIFT = 4;
        private const int MULTIPLIER_VALUE = 1 << MULTIPLIER_SHIFT;

        private const int frequency = 22000;
        private const int byteBufferSize = 2 * frequency;
        private const int shortBufferSize = byteBufferSize / 2;

        // Initialize to null so we can verify when it's set later on
        private DynamicSoundEffectInstance dynSound = null;

        private MemoryStream memBuffer;
        private short[] shortBuffer;
        private byte[] byteBuffer;

        private Thread playThread = null;

#if SILVERLIGHT
        private System.Windows.Threading.DispatcherTimer dispatchTimer;
#else
        private System.Timers.Timer dispatchTimer;
#endif

        private bool aborting = false;
        private bool aborted = true;

        private object lockObj = new object();

        private Player player;

        private float currentVolume = 1.0f;

        /// <summary>
        /// Create a new Instance of XnaSidPlayer
        /// </summary>
        /// <param name="ownDispatcher">true = player will call FrameworkDispatcher.Update() (needed if player is not part of a XNA application)</param>
        public MonoSidPlayer(bool ownDispatcher)
        {
            init(ownDispatcher);
        }
        /// <summary>
        /// only used for deserializing
        /// </summary>
        /// <param name="ownDispatcher">true = player will call FrameworkDispatcher.Update() (needed if player is not part of a XNA application)</param>
        /// <param name="reader">reader to deserialize objects from</param>
        public MonoSidPlayer(bool ownDispatcher, BinaryReader reader)
        {
            init(ownDispatcher);
            LoadFromReader(reader);
        }

        private void init(bool ownDispatcher)
        {
            memBuffer = new MemoryStream();
            shortBuffer = new short[shortBufferSize];
            byteBuffer = new byte[byteBufferSize];

            if (ownDispatcher)
            {
#if SILVERLIGHT
                dispatchTimer = new System.Windows.Threading.DispatcherTimer();
                dispatchTimer.Interval = TimeSpan.FromTicks(333333);//FromMilliseconds(30);
                dispatchTimer.Tick += delegate
#else
                dispatchTimer = new System.Timers.Timer(30);
                dispatchTimer.AutoReset = true;
                dispatchTimer.Elapsed += delegate
#endif
                {
                    FrameworkDispatcher.Update();
                };
            }
        }

        /// <summary>
        /// returns the current Status of the Player
        /// </summary>
        public SID2Types.sid2_player_t State
        {
            get
            {
                if (player != null)
                {
                    return player.State;
                }
                else
                {
                    return SID2Types.sid2_player_t.sid2_stopped;
                }
            }
        }


        /// <summary>
        ///  Start playing the tune with the default song
        /// </summary>
        /// <param name="tune">SidTune</param>
        public void Start(SidTune? tune)
        {
            Start(tune, 0);
        }
        /// <summary>
        /// Start playing the tune with the selected song
        /// </summary>
        /// <param name="tune">SidTune</param>
        /// <param name="songNumber">song id (1..count), 0 = default song</param>
        public void Start(SidTune tune, int songNumber)
        {
            if (stopping)
                return;

            if (dispatchTimer != null)
            {
                dispatchTimer.Start();
            }
            Thread.Sleep(1); // wait for dispatchTimer to start

            player = new Player();

            sid2_config_t config = player.config();

            config.frequency = frequency;
            config.playback = SID2Types.sid2_playback_t.sid2_mono;
            config.optimisation = SID2Types.SID2_DEFAULT_OPTIMISATION;
            config.sidModel = (SID2Types.sid2_model_t)tune.Info.sidModel;
            config.clockDefault = SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT;
            config.clockSpeed = SID2Types.sid2_clock_t.SID2_CLOCK_CORRECT;
            config.clockForced = false;
            config.environment = SID2Types.sid2_env_t.sid2_envR;
            config.forceDualSids = false;
            config.volume = 255;
            config.sampleFormat = SID2Types.sid2_sample_t.SID2_LITTLE_SIGNED;
            config.sidDefault = SID2Types.sid2_model_t.SID2_MODEL_CORRECT;
            config.sidSamples = true;
            config.precision = SID2Types.SID2_DEFAULT_PRECISION;
            player.config(config);

            tune.selectSong(songNumber);
            player.load(tune);

            dynSound = new DynamicSoundEffectInstance(frequency, AudioChannels.Stereo);
            dynSound.Volume = currentVolume;

            playThread = GetPlayThread(player, dynSound, tune.isStereo);

            aborting = false;
            Thread.Sleep(20); // wait for everything to initialize
            playThread.Start();
        }


        public void setVolume(float volume)
        {
            if (dynSound != null) 
            {
                dynSound.Volume = volume;
            }
            currentVolume = volume;
        }


        /// <summary>
        /// stop playing the current tune
        /// </summary>
        public void stop()
        {
            if (stopping)
                return;

            lock (lockObj)
            {
                aborting = true;
                try
                {
                    while (!aborted)
                    {
                        Thread.Sleep(1);
                    }

                    try
                    {
                        if (playThread != null)
                        {
                            //playThread.Abort();
                            playThread = null;
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        if (dynSound != null)
                        {
                            dynSound.Stop(true);
                            dynSound.Dispose();
                            dynSound = null;
                        }
                    }
                    catch
                    {
                    }

                    if (dispatchTimer != null)
                    {
                        dispatchTimer.Stop();
                    }

                    memBuffer.SetLength(0);

                    if (player != null)
                    {
                        player.stop();
                    }
                }
                finally
                {
                    aborting = false;
                }
            }
        }

        /// <summary>
        /// pause playing
        /// </summary>
        public void pause()
        {
            if (stopping)
                return;

            if (player != null && player.State == SID2Types.sid2_player_t.sid2_playing)
            {
                player.pause();
                while (player.inPlay)
                {
                    Thread.Sleep(1);
                }

                if (dispatchTimer != null)
                {
                    dispatchTimer.Stop();
                }
            }
        }
        /// <summary>
        /// resume playing
        /// </summary>
        public void resume()
        {
            if (stopping)
                return;

            if (dispatchTimer != null)
            {
                dispatchTimer.Start();
            }
            player.resume();
        }

        private Thread GetPlayThread(Player player, DynamicSoundEffectInstance dynSound, bool isStereo)
        {
            return new Thread(() =>
            {
                try
                {
                    player.start();

                    aborted = false;

                    while (!aborting)
                    {
                        if (player.State == SID2Types.sid2_player_t.sid2_paused)
                        {
                            Thread.Sleep(100);
                        }
                        else
                        {
                            // Update/fill buffer to be played next
                            player.play(shortBuffer, shortBufferSize);

                            if (player.State == SID2Types.sid2_player_t.sid2_playing)
                            {
                                int pos = shortBufferSize;
                                int idx = byteBufferSize;

                                if (isStereo)
                                {
                                    while (pos > 0)
                                    {

                                        int sl = (short)((short)(shortBuffer[--pos] << 8) | (shortBuffer[--pos]));
                                        int sr = (short)((short)(shortBuffer[--pos] << 8) | (shortBuffer[--pos]));
                                        sl = (int)(sl * MULTIPLIER_VALUE) >> MULTIPLIER_SHIFT;
                                        sr = (int)(sr * MULTIPLIER_VALUE) >> MULTIPLIER_SHIFT;

                                        byteBuffer[--idx] = (byte)(sl >> 8);
                                        byteBuffer[--idx] = (byte)(sl & 0xff);
                                        byteBuffer[--idx] = (byte)(sr >> 8);
                                        byteBuffer[--idx] = (byte)(sr & 0xff);
                                    }
                                }
                                else
                                {
                                    while (pos > 0)
                                    {
                                        int s = (short)((short)(shortBuffer[--pos] << 8) | (shortBuffer[--pos]));
                                        s = (int)(s * MULTIPLIER_VALUE) >> MULTIPLIER_SHIFT;
                                        byte sl = (byte)(s >> 8);
                                        byte sr = (byte)(s & 0xFF);
                                        byteBuffer[--idx] = sl;
                                        byteBuffer[--idx] = sr;
                                        byteBuffer[--idx] = sl;
                                        byteBuffer[--idx] = sr;
                                    }
                                }

                                if (!aborting && dynSound.State != SoundState.Playing)
                                {
                                    lock (lockObj)
                                    {
                                        dynSound.Play();
                                    }
                                }

                                while (!aborting && dynSound.PendingBufferCount > 1)
                                {
                                    Thread.Sleep(1);
                                    if (player.State == SID2Types.sid2_player_t.sid2_paused)
                                    {
                                        lock (lockObj)
                                        {
                                            dynSound.Stop();
                                        }
                                        break;
                                    }
                                }

                                if (!aborting && player.State == SID2Types.sid2_player_t.sid2_playing)
                                {
                                    lock (lockObj)
                                    {
                                        dynSound.SubmitBuffer(byteBuffer);
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    aborted = true;
                }

            });
        }

        /// <summary>
        /// is Player currently stopping?
        /// </summary>
        public bool stopping
        {
            get
            {
                return aborting && !aborted;
            }
        }

        public void Dispose()
        {
            stop();
            player = null;
        }

        /// <summary>
        /// Used for Serializing a running player
        /// </summary>
        /// <param name="writer"></param>
        public void SaveToWriter(BinaryWriter writer)
        {
            pause();
            player.SaveToWriter(writer);
        }
        /// <summary>
        /// Used for Deserializing a running player
        /// </summary>
        /// <param name="reader"></param>
        protected void LoadFromReader(BinaryReader reader)
        {
            player = new Player(reader);

            dynSound = new DynamicSoundEffectInstance(frequency, AudioChannels.Stereo);
            dynSound.Volume = currentVolume;

            playThread = GetPlayThread(player, dynSound, player.m_tune.isStereo);

            aborting = false;
            Thread.Sleep(21);
            playThread.Start();
        }

        
    }
}
