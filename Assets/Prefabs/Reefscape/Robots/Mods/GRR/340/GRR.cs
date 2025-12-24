using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.FieldScripts;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using MoSimLib;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.GRR._340
{
    public class GRR : ReefscapeRobotBase
    {
        [Header("Robot Components")] [SerializeField]
        private GenericJoint wristJoint;

        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint climber;
        [SerializeField] private GenericRoller[] funnelRollers;

        [Header("PID Constants")] [SerializeField]
        private PidConstants wristPIDMI;

        [SerializeField] private PidConstants climberPIDMI;

        [Header("Setpoints")] [SerializeField]
        private GRRSetpoint coralIntake;

        [SerializeField] private GRRSetpoint stow;
        [SerializeField] private GRRSetpoint coralStow;
        [SerializeField] private GRRSetpoint l1Left;
        [SerializeField] private GRRSetpoint l1Right;
        [SerializeField] private GRRSetpoint l2;
        [SerializeField] private GRRSetpoint l2Place;
        [SerializeField] private GRRSetpoint l3;
        [SerializeField] private GRRSetpoint l3Place;
        [SerializeField] private GRRSetpoint l4;
        [SerializeField] private GRRSetpoint l4Place;
        [SerializeField] private GRRSetpoint lowAlgae;
        [SerializeField] private GRRSetpoint highAlgae;
        [SerializeField] private GRRSetpoint climb;
        [SerializeField] private GRRSetpoint climbed;

        [Header("Game Piece Intakes")] [SerializeField]
        private ReefscapeGamePieceIntake coralIntakeComponent;

        [Header("Game Piece States")] [SerializeField]
        private string currentState;

        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralStowState;

        [Header("Target Positions")] [SerializeField]
        private float targetWristAngle;

        [SerializeField] private float targetElevatorDistance;
        [SerializeField] private float targetClimberAngle;

        [Header("Robot Audio")] [SerializeField]
        private AudioSource rollerSource;

        [Header("Intake Wheels")] [SerializeField]
        private GenericAnimationJoint[] intakeWheels;
        
        [SerializeField] private float intakeWheelSpeed = 300f;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode coralController;

        private ReefscapeSetpoints _previousSetpoint;
        private GRRAutoAlign _autoAlign;
        private bool alreadyPlaced;
        private bool _isPlacing;

        protected override void Start()
        {
            base.Start();

            wristJoint.SetPid(wristPIDMI);
            climber.SetPid(climberPIDMI);

            targetWristAngle = coralStow.wristTarget;
            targetElevatorDistance = 0;
            targetClimberAngle = stow.climberTarget;
            
            _previousSetpoint = ReefscapeSetpoints.Stow;

            RobotGamePieceController.SetPreload(coralStowState);
            coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());

            coralController.gamePieceStates = new[] { coralIntakeState, coralStowState };
            coralController.intakes.Add(coralIntakeComponent);

            _autoAlign = gameObject.GetComponent<GRRAutoAlign>();

            alreadyPlaced = false;
        }

        private void LateUpdate()
        {
            wristJoint.UpdatePid(wristPIDMI);
            climber.UpdatePid(climberPIDMI);
        }

        private void FixedUpdate()
        {
            bool autoPlace = _autoAlign.InPosition();
            if (autoPlace)
            {
                SetState(ReefscapeSetpoints.Place);
            }

            bool hasCoral = coralController.HasPiece();
            
            if (hasCoral)
            {
                foreach (var roller in funnelRollers)
                {
                    roller.flipVelocity();
                }
            }

            var readState = coralController.GetCurrentState();
            if (readState != null)
            {
                currentState = readState.name;
            }

            if (CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                alreadyPlaced = false;
            }

            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                return;
            }

            UpdateIntakeAnimation();
            UpdateOuttakeAnimation();

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (hasCoral)
                    {
                        SetSetpoint(coralStow);
                        SetRobotMode(ReefscapeRobotMode.Coral);
                    }
                    else
                    {
                        SetSetpoint(stow);
                    }

                    coralController.SetTargetState(coralStowState);
                    break;
                case ReefscapeSetpoints.Intake:
                    SetSetpoint(coralIntake);

                    if (!hasCoral)
                    {
                        coralController.SetTargetState(coralIntakeState);
                        coralController.RequestIntake(coralIntakeComponent, CurrentRobotMode == ReefscapeRobotMode.Coral && !hasCoral);
                    } else
                    {
                        coralController.SetTargetState(coralStowState);
                    }
            
                    break;
                case ReefscapeSetpoints.Place:
                    if (!alreadyPlaced && (OuttakeAction.triggered || autoPlace))
                    {
                        StartCoroutine(PlacePiece());
                    }
                    alreadyPlaced = true;
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(_autoAlign.Left() ? l1Left : l1Right);
                    coralController.RequestIntake(coralIntakeComponent, IntakeAction.IsPressed());
                    break;
                case ReefscapeSetpoints.Stack:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.L2:
                    // If we don't have coral, redirect to algae setpoints
                    if (!coralController.HasPiece())
                    {
                        SetState(ReefscapeSetpoints.LowAlgae);
                    }
                    else
                    {
                        SetSetpoint(l2);
                        coralController.RequestIntake(coralIntakeComponent, IntakeAction.IsPressed());
                    }
                    break;
                case ReefscapeSetpoints.L3:
                    // If we don't have coral, redirect to algae setpoints
                    if (!coralController.HasPiece())
                    {
                        SetState(ReefscapeSetpoints.HighAlgae);
                    }
                    else
                    {
                        SetSetpoint(l3);
                        coralController.RequestIntake(coralIntakeComponent, IntakeAction.IsPressed());
                    }
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4);
                    coralController.RequestIntake(coralIntakeComponent, IntakeAction.IsPressed());
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(lowAlgae);
                    targetClimberAngle = lowAlgae.climberTarget;
                    if (coralController.HasPiece())
                    {
                        coralController.RequestIntake(coralIntakeComponent, false);
                    }
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(highAlgae);
                    targetClimberAngle = highAlgae.climberTarget;
                    if (coralController.HasPiece())
                    {
                        coralController.RequestIntake(coralIntakeComponent, false);
                    }
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climb);
                    targetClimberAngle = climb.climberTarget;
                    coralController.RequestIntake(coralIntakeComponent, false);
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbed);
                    targetClimberAngle = climbed.climberTarget;
                    coralController.SetTargetState(coralStowState);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            _previousSetpoint = CurrentSetpoint;
            UpdateSetpoints();
        }

        private void SetSetpoint(GRRSetpoint setpoint)
        {
            targetWristAngle = setpoint.wristTarget;
            targetElevatorDistance = setpoint.elevatorDistance;
            targetClimberAngle = setpoint.climberTarget;
        }

        private void UpdateSetpoints()
        {
            wristJoint.SetTargetAngle(targetWristAngle).withAxis(JointAxis.X).flipDirection();
            elevator.SetTarget(targetElevatorDistance);
            climber.SetTargetAngle(targetClimberAngle).withAxis(JointAxis.Z).flipDirection();
        }

        private void UpdateIntakeAnimation()
        {
            if (_isPlacing || CurrentSetpoint == ReefscapeSetpoints.Place)
            {
                // Don't run intake animation while placing/scoring
                foreach (var intakeWheel in intakeWheels)
                {
                    intakeWheel.VelocityRoller(0);
                }
                return;
            }

            // Algae setpoints: rollers run continuously until another setpoint is set
            bool isAlgaeSetpoint = CurrentSetpoint == ReefscapeSetpoints.LowAlgae || 
                                   CurrentSetpoint == ReefscapeSetpoints.HighAlgae;

            if (isAlgaeSetpoint)
            {
                float direction = (CurrentRobotMode == ReefscapeRobotMode.Algae) ? 1f : -1f;
                foreach (var intakeWheel in intakeWheels)
                {
                    intakeWheel.VelocityRoller(intakeWheelSpeed * direction);
                }
                return;
            }

            // Intake wheels only run when:
            // 1. At Intake setpoint
            // 2. Requesting intake (IntakeAction pressed and in Coral mode)
            // 3. Don't have coral yet (stops once you get coral)
            bool shouldIntake = CurrentSetpoint == ReefscapeSetpoints.Intake && 
                                !coralController.HasPiece() && 
                                CurrentRobotMode == ReefscapeRobotMode.Coral &&
                                IntakeAction.IsPressed();

            if (shouldIntake)
            {
                float direction = (CurrentRobotMode == ReefscapeRobotMode.Algae) ? 1f : -1f;
                foreach (var intakeWheel in intakeWheels)
                {
                    intakeWheel.VelocityRoller(intakeWheelSpeed * direction);
                }
            }
            else
            {
                // Stop intake wheels when conditions aren't met (unless we're in algae setpoint)
                foreach (var intakeWheel in intakeWheels)
                {
                    intakeWheel.VelocityRoller(0);
                }
            }
        }

        private void UpdateOuttakeAnimation()
        {
            // Outtake animation is handled in PlacePiece coroutine
            // This method is here for consistency but outtake is handled in the coroutine
        }

        private IEnumerator PlacePiece()
        {
            if (alreadyPlaced) yield break;
            
            _isPlacing = true;
            
            switch (LastSetpoint)
            {
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4Place);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3Place);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2Place);
                    break;
            }

            //default mode is impulse
            if (CurrentRobotMode == ReefscapeRobotMode.Coral && coralController.HasPiece())
            {
                var time = 0.35f;
                Vector3 force = new Vector3(0, 0, 4f);
                var maxSpeed = 0.5f;

                if (LastSetpoint == ReefscapeSetpoints.L4 || LastSetpoint == ReefscapeSetpoints.Barge)
                {
                    time = 0.5f;
                    force = new Vector3(0, 0.45f, 5f);
                }
                else if (LastSetpoint == ReefscapeSetpoints.L1)
                {
                    time = 0.4f;
                    force = new Vector3(0, 0, 3f);
                    maxSpeed = 0.25f;
                }

                coralController.ReleaseGamePieceWithContinuedForce(force, time, maxSpeed);

                // Outtake animation - reverse direction from intake
                float currentIntakeDirection = (CurrentRobotMode == ReefscapeRobotMode.Algae) ? 1f : -1f;
                float outtakeSpeed = -intakeWheelSpeed * currentIntakeDirection;

                float timer = 0;
                while (timer < time)
                {
                    foreach (var wheel in intakeWheels)
                    {
                        wheel.VelocityRoller(outtakeSpeed);
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }

                foreach (var wheel in intakeWheels)
                {
                    wheel.VelocityRoller(0);
                }
            }
            
            _isPlacing = false;
            yield break;
        }
    }
}

