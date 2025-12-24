using Games.Reefscape.Robots;
using MoSimCore.Enums;
using RobotFramework.Controllers.Drivetrain;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.GRR._340
{
    public class GRRAutoAlign : MonoBehaviour
    {

        [Header("Offsets")]
        [Tooltip("The offset in the X direction from the reef's center for the robot to target (Always positive)")]
        public float x = 1.3f;
        [Tooltip("The offset in the Y direction from the reef's center for the robot to target (Always positive)")]
        public float y = 0.17f;

        [Header("Translation")]
        [Tooltip("The configured maximum velocity of the robot, in ft/s")]
        public float maxVelocity = 16.0f;
        [Tooltip("The maximum deceleration of the auto-align controller, in ft/s/s")]
        public float maxDeceleration = 28.0f;
        [Tooltip("Arbitrary strength force for centering the robot on the targeted reef pole")]
        public float strength = 400f;
        [Tooltip("The tolerance at which the controller signals that the robot is in position")]
        public float positionTolerance = 0.08f;

        [Header("Rotation")]
        [Tooltip("Kp constant to apply to the robot's rotation")]
        public float rotateKp = 1.5f;

        private DriveController _driveController;
        private ReefscapeRobotBase _base;
        private Vector2 _reef;

        private bool _inPosition;
        private bool _fallingEdge;

        protected void Start()
        {
            _driveController = gameObject.GetComponent<DriveController>();
            _base = gameObject.GetComponent<ReefscapeRobotBase>();
            _reef = Vec3ToVec2(
                (_base.Alliance == Alliance.Blue ? GameObject.Find("BlueReef") : GameObject.Find("RedReef"))
                    .transform
                    .position
            );
        }

        protected void FixedUpdate()
        {
            var max_v = maxVelocity * FT_TO_M;
            var max_a = maxDeceleration * FT_TO_M;

            bool left = _base.AutoAlignLeftAction.IsPressed();
            bool right = _base.AutoAlignRightAction.IsPressed();

            var robot = Vec3ToVec2(_base.transform.position);
            var face = Rotate2((_reef - robot).normalized, SIXTH_PI);

            float rk_w = Mathf.Floor(Mathf.Atan2(face.y, face.x) / THIRD_PI) * THIRD_PI;
            var pole = Pole(x, rk_w, left);
            var error = pole - robot;

            _inPosition = error.magnitude < positionTolerance;
            _fallingEdge = (left || right) && (_fallingEdge || _inPosition);

            if (!_fallingEdge && (left || right))
            {
                float k = 1f;
                if (error.magnitude < max_v * max_v / (2f * max_a))
                {
                    k = Mathf.Sqrt(2f * max_a * error.magnitude) / max_v;
                }

                var force = error.normalized;

                var att = Pole(SL, rk_w, left) - pole;
                var proj = Rotate2(robot - pole, -Mathf.Atan2(att.y, att.x));
                if (proj.x > 0.0 && proj.x < SL - x)
                {
                    float t = Mathf.Abs(proj.y) / SW / 2f;
                    float m = -ST * t * t * Mathf.Sign(proj.y);
                    force += m * Rotate2(Vector2.down, rk_w);
                }

                float yaw = -Mathf.Deg2Rad * (_base.transform.rotation.eulerAngles.y - 90f);
                float alpha = rotateKp * ((rk_w - yaw + THREE_PI) % TWO_PI - Mathf.PI);

                _driveController.overideInput(force.normalized * k, alpha, DriveController.DriveMode.FieldOriented);
            }
        }

        private Vector2 Pole(float x, float angle, bool left)
        {
            return _reef + Rotate2(new Vector2(-x, y * (left ? 1f : -1f)), angle);
        }

        private Vector2 Rotate2(Vector2 v, float radians)
        {
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private Vector2 Vec3ToVec2(Vector3 vec3)
        {
            return new Vector2(vec3.x, vec3.z);
        }

        public bool InPosition()
        {
            return _inPosition;
        }

        private const float ST = 200f;
        private const float SL = 10f;
        private const float SW = 6f;
        private const float FT_TO_M = 0.3048f;
        private const float SIXTH_PI = Mathf.PI / 6f;
        private const float THIRD_PI = Mathf.PI / 3f;
        private const float TWO_PI = Mathf.PI * 2f;
        private const float THREE_PI = Mathf.PI * 3f;
    }
}