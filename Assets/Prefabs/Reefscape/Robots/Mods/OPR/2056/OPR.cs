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

namespace Prefabs.Reefscape.Robots.Mods.OPR._2056
{
    public class OPR : ReefscapeRobotBase
    {
        [Header("Robot Components")]
        [SerializeField] private GenericJoint armJoint;
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint intakeJoint;
        [SerializeField] private GenericJoint climberJoint;
        [SerializeField] private GenericRoller[] intakeRollers;
        [SerializeField] private GenericAnimationJoint[] intakeWheels;

        [Header("PID Constants")]
        [SerializeField] private PidConstants armPidConstants;
        [SerializeField] private PidConstants intakePidConstants;
        [SerializeField] private PidConstants climberPidConstants;

        [Header("Setpoints")]
        [SerializeField] private OPRSetpoint coralIntakeSetpoint;
        [SerializeField] public OPRSetpoint algaeGroundIntakeSetpoint;
        [SerializeField] private OPRSetpoint stowSetpoint;
        [SerializeField] private OPRSetpoint coralStowSetpoint;
        [SerializeField] private OPRSetpoint algaeStowSetpoint;
        [SerializeField] private OPRSetpoint l1Setpoint;
        [SerializeField] private OPRSetpoint l2Setpoint;
        [SerializeField] private OPRSetpoint l2PlaceSetpoint;
        [SerializeField] public OPRSetpoint lowAlgaeSetpoint;
        [SerializeField] private OPRSetpoint l3Setpoint;
        [SerializeField] private OPRSetpoint l3PlaceSetpoint;
        [SerializeField] public OPRSetpoint highAlgaeSetpoint;
        [SerializeField] private OPRSetpoint l4Setpoint;
        [SerializeField] private OPRSetpoint l4PlaceSetpoint;
        [SerializeField] private OPRSetpoint bargeSetpoint;
        [SerializeField] private OPRSetpoint backBargeSetpoint;
        [SerializeField] private OPRSetpoint processorSetpoint;
        [SerializeField] private OPRSetpoint stackAlgaeIntakeSetpoint;
        [SerializeField] private OPRSetpoint climbSetpoint;
        [SerializeField] private OPRSetpoint climbedSetpoint;

        [Header("Game Piece Intakes")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [Header("Game Piece States")]
        [SerializeField] private string currentState;
        [SerializeField] private GamePieceState coralIntakeState;
        [SerializeField] private GamePieceState coralSecondSetpointState;
        [SerializeField] private GamePieceState coralThirdSetpointState;
        [SerializeField] private GamePieceState coralFourthSetpointState;
        [SerializeField] private GamePieceState coralChassisStowState;
        [SerializeField] private GamePieceState coralArmStowState;
        [SerializeField] private GamePieceState algaeStowState;
        [SerializeField] private GamePieceState algaeIntakeState;

        [Header("Intake Wheels")]
        [SerializeField] private float intakeWheelSpeed = 300f;

        [Header("Target Positions")]
        [SerializeField] private float targetArmAngle;
        [SerializeField] private float targetElevatorHeight;
        [SerializeField] private float targetIntakeAngle;
        [SerializeField] private float targetClimberAngle;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode _algaeController;

        private bool _intakeSequenceRunning;
        private bool _disruptable;
        private bool _wasCoral;
        private bool _isPlacingCoral;
        private bool _alreadyPlaced;
        private float _delay;
        private ReefscapeSetpoints? _bufferedSetpoint;
        private bool _bufferAlgaeState;
        private bool _facingBarge;

        protected override void Start()
        {
            base.Start();

            superCycler = true;

            armJoint.SetPid(armPidConstants);
            intakeJoint.SetPid(intakePidConstants);
            climberJoint.SetPid(climberPidConstants);

            targetArmAngle = coralStowSetpoint.armAngle;
            targetElevatorHeight = 0;
            targetClimberAngle = stowSetpoint.climberAngle;

            RobotGamePieceController.SetPreload(coralArmStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[]
            {
                coralIntakeState, coralSecondSetpointState, coralThirdSetpointState, coralChassisStowState,
                coralArmStowState, coralFourthSetpointState
            };
            _coralController.intakes.Add(coralIntake);

            _algaeController.gamePieceStates = new[] { algaeStowState, algaeIntakeState };
            _algaeController.intakes.Add(algaeIntake);

            _disruptable = true;
            _intakeSequenceRunning = false;
            _wasCoral = false;
            _isPlacingCoral = false;
            _alreadyPlaced = false;
            _bufferedSetpoint = null;
            _bufferAlgaeState = false;
            _delay = 200; //its backwards I dont know why
        }

        private void LateUpdate()
        {
            armJoint.UpdatePid(armPidConstants);
            intakeJoint.UpdatePid(intakePidConstants);
            climberJoint.UpdatePid(climberPidConstants);
        }

        private void FixedUpdate()
        {
            if (_coralController.HasPiece())
            {
                foreach (var roller in intakeRollers)
                {
                    roller.flipVelocity();
                }
            }

            _algaeController.SetTargetState(algaeStowState);

            if (_algaeController.HasPiece() || CurrentSetpoint == ReefscapeSetpoints.Barge)
            {
                if (CurrentRobotMode == ReefscapeRobotMode.Coral)
                {
                    _wasCoral = true;
                }

                SetRobotMode(ReefscapeRobotMode.Algae);
            }
            else if (_coralController.HasPiece() && CurrentSetpoint == ReefscapeSetpoints.Place)
            {
                SetRobotMode(ReefscapeRobotMode.Coral);
            }

            if (_disruptable && _bufferAlgaeState)
            {
                SetRobotMode(ReefscapeRobotMode.Algae);
                _bufferAlgaeState = false;
            }

            if (_intakeSequenceRunning || _coralController.HasPiece())
            {
                if (CurrentSetpoint != ReefscapeSetpoints.Stow && CurrentSetpoint != ReefscapeSetpoints.Intake)
                {
                    _bufferedSetpoint = CurrentSetpoint;
                }
            }

            if (((_coralController.currentStateNum != coralArmStowState.stateNum && !_disruptable) &&
                 !_coralController.atTarget) || _intakeSequenceRunning)
            {
                if (!_disruptable && CurrentRobotMode != ReefscapeRobotMode.Coral && _intakeSequenceRunning &&
                    !_algaeController.HasPiece())
                {
                    _bufferAlgaeState = true;
                    SetRobotMode(ReefscapeRobotMode.Coral);
                }
                else if (_disruptable && CurrentRobotMode != ReefscapeRobotMode.Coral)
                {
                }
                else
                {
                    SetState(ReefscapeSetpoints.Stow);
                }
            }

            if ((!_intakeSequenceRunning && CurrentSetpoint != ReefscapeSetpoints.Intake) && _bufferedSetpoint != null)
            {
                SetState(_bufferedSetpoint.Value);
                _bufferedSetpoint = null;
            }

            bool coralAtEE = _coralController.currentStateNum == coralArmStowState.stateNum && _coralController.atTarget;

            if (coralAtEE && CurrentRobotMode != ReefscapeRobotMode.Coral)
            {
                SetRobotMode(ReefscapeRobotMode.Coral);
            }

            if (CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                _alreadyPlaced = false;
            }

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    if (!_intakeSequenceRunning || _coralController.HasPiece())
                    {
                        SetSetpoint(_algaeController.HasPiece()
                            ? algaeStowSetpoint
                            : coralStowSetpoint);
                    }

                    targetClimberAngle = stowSetpoint.climberAngle;
                    break;
                case ReefscapeSetpoints.Intake:
                    if (CurrentRobotMode == ReefscapeRobotMode.Algae && !_algaeController.HasPiece())
                    {
                        SetSetpoint(algaeGroundIntakeSetpoint);
                        _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !coralAtEE);
                        _algaeController.SetTargetState(algaeStowState);
                    }
                    break;
                case ReefscapeSetpoints.Place:
                    if (LastSetpoint == ReefscapeSetpoints.Stow && coralAtEE)
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        break;
                    }

                    if (_algaeController.HasPiece())
                    {
                        _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0.25f, 0));
                        if (_wasCoral)
                        {
                            SetRobotMode(ReefscapeRobotMode.Coral);
                            _wasCoral = false;
                        }
                    }
                    else if (CurrentRobotMode == ReefscapeRobotMode.Coral && _coralController.HasPiece())
                    {
                        // Set place setpoint immediately when in Place mode
                        switch (LastSetpoint)
                        {
                            case ReefscapeSetpoints.L4:
                                SetSetpoint(l4PlaceSetpoint);
                                break;
                            case ReefscapeSetpoints.L3:
                                SetSetpoint(l3PlaceSetpoint);
                                break;
                            case ReefscapeSetpoints.L2:
                                SetSetpoint(l2PlaceSetpoint);
                                break;
                        }

                        if (!_alreadyPlaced && OuttakeAction.triggered)
                        {
                            StartCoroutine(PlaceCoral());
                        }
                    }
                    _alreadyPlaced = true;
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1Setpoint);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2Setpoint);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(lowAlgaeSetpoint);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !coralAtEE);
                    _algaeController.SetTargetState(algaeStowState);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3Setpoint);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(highAlgaeSetpoint);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !coralAtEE);
                    _algaeController.SetTargetState(algaeStowState);
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4Setpoint);
                    break;
                case ReefscapeSetpoints.Barge:
                    CheckFacingBarge();
                    SetSetpoint(_facingBarge ? backBargeSetpoint : bargeSetpoint);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    break;
                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climbSetpoint);
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbedSetpoint);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(stackAlgaeIntakeSetpoint);
                    _algaeController.RequestIntake(algaeIntake, IntakeAction.IsPressed() && !coralAtEE);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(processorSetpoint);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

            OPIntakeSequence();
            UpdateSetpoints();
        }

        private IEnumerator PlaceCoral()
        {
            _isPlacingCoral = true;

            // Set place setpoint (like GRR does in coroutine)
            switch (LastSetpoint)
            {
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4PlaceSetpoint);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3PlaceSetpoint);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2PlaceSetpoint);
                    break;
            }

            yield return new WaitForSeconds(0.05f);

            if (CurrentRobotMode == ReefscapeRobotMode.Coral && _coralController.HasPiece())
            {
                if (LastSetpoint != ReefscapeSetpoints.L1)
                {
                    _coralController.ReleaseGamePieceWithForce(FacingReef
                        ? new Vector3(0, -1.5f, 2.5f)
                        : new Vector3(0, 1.5f, -2.5f));
                }
                else
                {
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, -2f, 0));
                }
            }
            
            _isPlacingCoral = false;
            yield break;
        }

        private void CheckFacingBarge()
        {
            var toZAxisXY = new Vector3(-transform.position.x, -transform.position.y, 0f).normalized;
            var forwardXY = new Vector3(transform.forward.x, transform.forward.y, 0f).normalized;
            var dot = Vector3.Dot(forwardXY, toZAxisXY);
            _facingBarge = dot > 0.0f;
        }

        private void OPIntakeSequence()
        {
            if (!IntakeAction.IsPressed())
            {
                _intakeSequenceRunning = false;
                if (!_coralController.HasPiece())
                {
                    _disruptable = true;
                    // Reset placing flag when intake is released and no coral
                    if (_isPlacingCoral)
                    {
                        _isPlacingCoral = false;
                    }
                }
            }

            if (CurrentRobotMode == ReefscapeRobotMode.Coral ||
                (_algaeController.HasPiece() && CurrentRobotMode == ReefscapeRobotMode.Algae))
            {
                if (CurrentSetpoint != ReefscapeSetpoints.HighAlgae && CurrentSetpoint != ReefscapeSetpoints.LowAlgae &&
                    CurrentSetpoint != ReefscapeSetpoints.Barge && CurrentSetpoint != ReefscapeSetpoints.Place)
                {
                    bool hasAlgae = _algaeController.HasPiece();
                    _coralController.RequestIntake(coralIntake, IntakeAction.IsPressed());

                    if (IntakeAction.IsPressed() ||
                        (_coralController.HasPiece() && _coralController.currentStateNum != coralArmStowState.stateNum))
                    {
                        _disruptable = false;
                        _intakeSequenceRunning = true;

                        targetArmAngle = hasAlgae ? targetArmAngle : coralIntakeSetpoint.armAngle;
                        targetElevatorHeight = hasAlgae ? targetElevatorHeight : stowSetpoint.elevatorHeight;
                        targetIntakeAngle = coralIntakeSetpoint.intakeAngle;

                        _coralController.SetTargetState(_coralController.currentStateNum switch
                        {
                            0 => coralIntakeState,
                            1 => coralSecondSetpointState,
                            2 => coralThirdSetpointState,
                            3 => coralFourthSetpointState,
                            4 => coralChassisStowState,
                            _ => _coralController.GetCurrentState() ?? coralIntakeState
                        });

                        if (BaseGameManager.Instance.RobotState == RobotState.Enabled &&
                            Mathf.Approximately(targetIntakeAngle, coralIntakeSetpoint.intakeAngle))
                        {
                            foreach (var wheel in intakeWheels)
                            {
                                wheel.VelocityRoller(intakeWheelSpeed).useAxis(JointAxis.X);
                            }
                        }

                        bool atChassisStow = _coralController.atTarget &&
                                            _coralController.currentStateNum == coralChassisStowState.stateNum;
                        if (atChassisStow)
                        {
                            targetArmAngle = hasAlgae ? targetArmAngle : coralIntakeSetpoint.armAngle;
                            targetElevatorHeight = hasAlgae ? targetElevatorHeight : coralIntakeSetpoint.elevatorHeight;
                            targetIntakeAngle = stowSetpoint.intakeAngle;
                            _disruptable = true;

                            // Second/elevator check: require elevator to be within tolerance of coral grab height
                            float elev = elevator.GetElevatorHeight();
                            bool elevatorAtCoralGrab = Mathf.Abs(elev - coralIntakeSetpoint.elevatorHeight) <= 0.05f;

                            if (elevatorAtCoralGrab && _coralController.atTarget &&
                                Mathf.Approximately(targetElevatorHeight, coralIntakeSetpoint.elevatorHeight))
                            {
                                _coralController.SetTargetState(coralArmStowState);
                            }
                        }

                        bool atArmStow = _coralController.atTarget &&
                                         _coralController.currentStateNum == coralArmStowState.stateNum;
                        if (atArmStow)
                        {
                            SetState(ReefscapeSetpoints.Stow);
                            _intakeSequenceRunning = false;
                        }
                    }
                    else if ((_coralController.currentStateNum == coralArmStowState.stateNum && _coralController.atTarget) &&
                             _intakeSequenceRunning)
                    {
                        SetState(ReefscapeSetpoints.Stow);
                        _intakeSequenceRunning = false;
                    }
                }
            }
        }

        private void SetSetpoint(OPRSetpoint setpoint)
        {
            targetArmAngle = setpoint.armAngle;
            targetElevatorHeight = setpoint.elevatorHeight;
            targetIntakeAngle = setpoint.intakeAngle;
            targetClimberAngle = setpoint.climberAngle;
        }

        private void UpdateSetpoints()
        {
            if (LastSetpoint == ReefscapeSetpoints.Barge && _facingBarge == false)
            {
                armJoint.SetTargetAngle(targetArmAngle).withAxis(JointAxis.X).noWrap(210);
            }
            else
            {
                armJoint.SetTargetAngle(targetArmAngle).withAxis(JointAxis.X);
            }

            elevator.SetTarget(targetElevatorHeight);

            intakeJoint.SetTargetAngle(targetIntakeAngle).withAxis(JointAxis.X).flipDirection();

            climberJoint.SetTargetAngle(targetClimberAngle).withAxis(JointAxis.Z).useAutomaticStartingOffset()
                .noWrap(180f);
        }
    }
}
