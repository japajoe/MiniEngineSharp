using System;
using MiniEngine.Core;
using OpenTK.Mathematics;
using static MiniAudioEx.Native.MiniAudioNative;

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
            get => ma_engine_listener_is_enabled(context.Engine, index) > 0;
            set => ma_engine_listener_set_enabled(context.Engine, index, value ? (UInt32)1 : 0);
        }

        public AudioListener(UInt32 index = 0) : base()
        {
            context = AudioContext.GetCurrent();

            if(context == null)
                throw new Exception("Failed to initialize AudioListener because there is no current AudioContext");

            if(index >= MA_ENGINE_MAX_LISTENERS)
                throw new Exception("Listener index should be less than " + MA_ENGINE_MAX_LISTENERS);

            this.index = index;

            Vector3 position = transform.position;
            Vector3 direction = transform.forward;
            Vector3 velocity = transform.velocity;

            ma_engine_listener_set_enabled(context.Engine, index, 1);
            ma_engine_listener_set_position(context.Engine, index, position.X, position.Y, position.Z);
            ma_engine_listener_set_direction(context.Engine, index, direction.X, direction.Y, direction.Z);
            ma_engine_listener_set_velocity(context.Engine, index, velocity.X, velocity.Y, velocity.Z);
            ma_engine_listener_set_world_up(context.Engine, index, 0.0f, 1.0f, 0.0f);
            ma_engine_listener_set_cone(context.Engine, index, 6.283185f, 6.283185f, 0.0f);

            context.Add(this);
        }

        internal void Update()
        {
            if(!Enabled)
                return;
            
            Vector3 position = transform.root.position;
            Vector3 direction = transform.root.forward;
            Vector3 velocity = transform.root.velocity;

            ma_engine_listener_set_position(context.Engine, index, position.X, position.Y, position.Z);
            ma_engine_listener_set_direction(context.Engine, index, direction.X, direction.Y, direction.Z);
            ma_engine_listener_set_velocity(context.Engine, index, velocity.X, velocity.Y, velocity.Z);
        }
    }
}