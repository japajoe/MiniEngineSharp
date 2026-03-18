using System;
using MiniAudioEx.Native;
using MiniEngine.Core;
using OpenTK.Mathematics;

namespace MiniEngine.AudioManagement
{
    /// <summary>
    /// This class represents a point in the 3D space where audio is perceived or heard.
    /// </summary>
    public sealed class AudioListener : Entity
    {
        private AudioContext context;
        private UInt32 index;

        /// <summary>
        /// If true, then spatialization is enabled for this listener.
        /// </summary>
        /// <value></value>
        public bool Enabled
        {
            get => MiniAudioNative.ma_engine_listener_is_enabled(context.Engine, index) > 0;
            set => MiniAudioNative.ma_engine_listener_set_enabled(context.Engine, index, value ? (UInt32)1 : 0);
        }

        public AudioListener(UInt32 index = 0) : base()
        {
            context = AudioContext.GetCurrent();

            if(context == null)
                throw new Exception("Failed to initialize AudioListener because there is no current AudioContext");

            if(index >= MiniAudioNative.MA_ENGINE_MAX_LISTENERS)
                throw new Exception("Listener index should be less than " + MiniAudioNative.MA_ENGINE_MAX_LISTENERS);

            this.index = index;

            Vector3 position = transform.position;
            Vector3 forward = transform.forward;
            Vector3 velocity = transform.velocity;

            MiniAudioNative.ma_engine_listener_set_position(context.Engine, index, position.X, position.Y, position.Z);
            MiniAudioNative.ma_engine_listener_set_direction(context.Engine, index, forward.X, forward.Y, forward.Z);
            MiniAudioNative.ma_engine_listener_set_velocity(context.Engine, index, velocity.X, velocity.Y, velocity.Z);
            
            Enabled = true;

            context.Add(this);
        }

        internal void Update()
        {
            if(!Enabled)
                return;
            
            Vector3 position = transform.position;
            Vector3 forward = transform.forward;
            Vector3 velocity = transform.velocity;

            MiniAudioNative.ma_engine_listener_set_position(context.Engine, index, position.X, position.Y, position.Z);
            MiniAudioNative.ma_engine_listener_set_direction(context.Engine, index, forward.X, forward.Y, forward.Z);
            MiniAudioNative.ma_engine_listener_set_velocity(context.Engine, index, velocity.X, velocity.Y, velocity.Z);
        }

        private void ApplySpatialSettings()
        {
            
        }
    }
}