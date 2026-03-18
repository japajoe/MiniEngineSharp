using System;
using OpenTK.Mathematics;

namespace MiniEngine.Core
{
    public sealed class CameraController
    {
        private Transform target;
        private float inputVertical;
        private float inputHorizontal;
        private float inputPanning;
        private float speed;
        private float rotationSpeed;
        private Vector3 currentRotation;
        private bool isInitialized;

        public CameraController()
        {
            target = null;
            speed = 5.0f;
            rotationSpeed = 0.012f;
            currentRotation = new Vector3(0, 0, 0);
            isInitialized = false;
        }

        public void Initialize(Camera camera)
        {
            if(camera == null)
                return;
            this.target = camera.transform;
        }

        public void OnLateUpdate()
        {
            if(target == null)
                return;

            if(!isInitialized)
            {
                //Prevents camera from suddenly rotating
                Vector3 euler = target.rotation.ToEulerAngles();
                currentRotation.X = euler.Y;
                currentRotation.Y = euler.X;
                isInitialized = true;
            }

            inputVertical = Input.GetAxis("Vertical");
            inputHorizontal = Input.GetAxis("Horizontal");
            inputPanning = Input.GetAxis("Panning");

            Vector3 UnitY = new Vector3(0, 1, 0);

            Vector3 direction = target.forward * inputVertical +
                                target.right * inputHorizontal +
                                UnitY * inputPanning;
            
            float deltaTime = Time.DeltaTime;

            if(direction.LengthSquared > 0)
            {
                direction.Normalize();
                Move(direction, speed * deltaTime);
            }

            Rotate();
        }

        private void Move(Vector3 direction, float movementSpeed)
        {
            Vector3 pos = target.position;
            pos += direction * movementSpeed;
            target.position = pos;
        }

        private void Rotate()
        {
            if(!Input.GetButton(ButtonCode.Right))
                return;

            Vector2 mouseDelta = Input.GetMouseDelta();

            currentRotation.Y += -mouseDelta.X * rotationSpeed;
            currentRotation.X += -mouseDelta.Y * rotationSpeed;

            currentRotation.X = Math.Clamp(currentRotation.X, MathHelper.DegreesToRadians(-89.9f), MathHelper.DegreesToRadians(89.9f));

            Quaternion rotationY = Quaternion.FromAxisAngle(new Vector3(0.0f, 1.0f, 0.0f), currentRotation.Y);
            Quaternion rotationX = Quaternion.FromAxisAngle(new Vector3(1.0f, 0.0f, 0.0f), currentRotation.X);
            Quaternion rotation = rotationY * rotationX;
            
            target.rotation = rotation;
        }
    }
}