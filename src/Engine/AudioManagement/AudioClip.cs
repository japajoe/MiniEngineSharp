using System;
using System.Runtime.InteropServices;
using MiniAudioEx.Native;
using static MiniAudioEx.Native.MiniAudioNative;

namespace MiniEngine.AudioManagement
{
    public sealed class AudioClip : IDisposable
    {
        private AudioContext context;
        private ma_sound_ptr sound;
        private string filePath;
        private IntPtr dataHandle;
        private UInt64 dataLength;
        private bool streamFromDisk;
        private UInt64 pcmLength;
        private UInt64 hashCode;
        public ma_sound_ptr Sound => sound;

        /// <summary>
        /// Gets the length of the audio clip in PCM samples.
        /// </summary>
        /// <value></value>
        public UInt64 LengthSamples => pcmLength;
        public UInt64 HashCode => hashCode;

        public AudioClip()
        {
            sound = new ma_sound_ptr(true);
            dataHandle = IntPtr.Zero;
            dataLength = 0;
            streamFromDisk = false;
            pcmLength = 0;
            hashCode = 0;
        }

        public AudioClip(string filePath, bool streamFromDisk = true)
        {
            context = AudioContext.GetCurrent();

            if(context == null)
            {
                Dispose();
                throw new Exception("Failed to initialize AudioClip because AudioContext is null");
            }

            if(!System.IO.File.Exists(filePath))
            {
                Dispose();
                throw new Exception("Failed to initialize AudioClip because the file does not exist");
            }

            this.filePath = filePath;
            this.streamFromDisk = streamFromDisk;
            dataHandle = IntPtr.Zero;
            dataLength = 0;
            
            sound = new ma_sound_ptr(true);

            ma_sound_flags flags = streamFromDisk ? ma_sound_flags.stream : ma_sound_flags.decode;
            ma_result result = ma_result.success;
            
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                result = ma_sound_init_from_file_w(context.Engine, filePath, flags, new ma_sound_group_ptr(IntPtr.Zero), new ma_fence_ptr(IntPtr.Zero), sound);
            else
                result = ma_sound_init_from_file(context.Engine, filePath, flags, new ma_sound_group_ptr(IntPtr.Zero), new ma_fence_ptr(IntPtr.Zero), sound);

            if (result != ma_result.success)
            {
                Dispose();
                throw new Exception("Failed to initialize AudioClip: " + result);
            }

            ma_sound_get_length_in_pcm_frames(sound, out pcmLength);

            hashCode = (UInt64)filePath.GetHashCode();

            context.Add(this);
        }

        public AudioClip(byte[] data)
        {
            context = AudioContext.GetCurrent();

            if(context == null)
            {
                Dispose();
                throw new Exception("Failed to initialize AudioClip because AudioContext is null");
            }

            if(data == null)
            {
                Dispose();
                throw new Exception("Failed to initialize AudioClip because the given data is null");
            }

            sound = new ma_sound_ptr(true);
            streamFromDisk = false;
            dataHandle = Marshal.AllocHGlobal(data.Length);
            dataLength = (UInt64)data.Length;

            if(dataHandle == IntPtr.Zero)
            {
                Dispose();
                throw new Exception("Failed to initialize AudioClip because data could not be allocated");
            }

            Marshal.Copy(data, 0, dataHandle, data.Length);

            ma_sound_flags flags = ma_sound_flags.decode;
            ma_result result = ma_sound_init_from_memory(context.Engine, dataHandle, dataLength, flags, new ma_sound_group_ptr(IntPtr.Zero), new ma_fence_ptr(IntPtr.Zero), sound);

            if (result != ma_result.success)
            {
                Dispose();
                throw new Exception("Failed to initialize AudioClip: " + result);
            }

            ma_sound_get_length_in_pcm_frames(sound, out pcmLength);

            hashCode = GetHashCode(data, data.Length);

            context.Add(this);
        }

        internal bool CopyTo(AudioClip other, ma_sound_group_ptr group = default)
        {
            if(context == null)
            {
                Console.WriteLine("Failed to Copy AudioClip because AudioContext is null");
                return false;
            }

            if(sound.pointer == IntPtr.Zero)
            {
                Console.WriteLine("Failed to Copy AudioClip because sound is null");
                return false;
            }
            
            if(other.sound.pointer == IntPtr.Zero)
            {
                Console.WriteLine("Failed to Copy AudioClip because other.sound is null");
                return false;
            }

            ma_sound_stop(other.sound);
            ma_sound_uninit(other.sound);

            other.streamFromDisk = streamFromDisk;
            other.dataLength = dataLength;
            other.filePath = filePath;
            other.pcmLength = pcmLength;
            other.hashCode = hashCode;
            other.context = context;

            ma_sound_flags flags = streamFromDisk ? ma_sound_flags.stream : ma_sound_flags.decode;
            ma_result result = ma_result.success;

            if(flags == ma_sound_flags.stream)
            {
                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    result = ma_sound_init_from_file_w(context.Engine, filePath, flags, group, new ma_fence_ptr(IntPtr.Zero), other.sound);
                else
                    result = ma_sound_init_from_file(context.Engine, filePath, flags, group, new ma_fence_ptr(IntPtr.Zero), other.sound);
            }
            else
            {
                if(dataHandle == IntPtr.Zero)
                    result = ma_sound_init_copy(context.Engine, sound, flags, group, other.sound);
                else
                    result = ma_sound_init_from_memory(context.Engine, dataHandle, dataLength, flags, group, new ma_fence_ptr(IntPtr.Zero), other.sound);
            }
            
            return result == ma_result.success;
        }

        public void Dispose()
        {
            if(context != null)
                context.Remove(this);

            if(sound.pointer != IntPtr.Zero)
            {
                ma_sound_uninit(sound);
                sound.Free();
            }

            if(dataHandle != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(dataHandle);
                dataHandle = IntPtr.Zero;
                dataLength = 0;
            }
        }

        private UInt64 GetHashCode(byte[] data, int size)
        {
            UInt64 hash = 0;

            for(int i = 0; i < size; i++) 
            {
                hash = data[i] + (hash << 6) + (hash << 16) - hash;
            }

            return hash;            
        }
    }
}