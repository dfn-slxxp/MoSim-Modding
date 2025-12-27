using System;
using Games.Reefscape.Enums;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using JetBrains.Annotations;
using RobotFramework.Components;
using RobotFramework.Controllers.Drivetrain;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods.NYPowerhousePack._694
{
    public class StuyPulse: ReefscapeRobotBase
    {
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint eeArm;
        [SerializeField] private GenericJoint froggy;
        [SerializeField] private GenericJoint climbPivot1;
        [SerializeField] private GenericJoint climbPivot2;

        
        [Header("PID Constants")]
        [SerializeField] private PidConstants eeArmPid;
        [SerializeField] private PidConstants froggyPid;
        [SerializeField] private PidConstants climbPivotsPid;

        
        [Header("Setpoints")]
        [SerializeField] private StuyPulseSetpoint stow;
        [SerializeField] private StuyPulseSetpoint intakeFunnel;
        [SerializeField] private StuyPulseSetpoint eeL1;
        [SerializeField] private StuyPulseSetpoint frontL2;
        [SerializeField] private StuyPulseSetpoint backL2;
        [SerializeField] private StuyPulseSetpoint frontL3;
        [SerializeField] private StuyPulseSetpoint backL3;
        [SerializeField] private StuyPulseSetpoint frontL4;
        [SerializeField] private StuyPulseSetpoint backL4;
        [SerializeField] private StuyPulseSetpoint backL4Scored;

        [SerializeField] private StuyPulseSetpoint lollipopIntake;
        [SerializeField] private StuyPulseSetpoint frontLowAlgae;
        [SerializeField] private StuyPulseSetpoint frontHighAlgae;
        [SerializeField] private StuyPulseSetpoint backLowAlgae;
        [SerializeField] private StuyPulseSetpoint backHighAlgae;
        [SerializeField] private StuyPulseSetpoint bargePrep;
        [SerializeField] private StuyPulseSetpoint bargePlace;
        [SerializeField] private StuyPulseSetpoint process;

        [SerializeField] private StuyPulseSetpoint froggyCoral;
        [SerializeField] private StuyPulseSetpoint froggyAlgae;
        [SerializeField] private StuyPulseSetpoint froggyCoralPlace;
        [SerializeField] private StuyPulseSetpoint froggyAlgaeProcess;

        [SerializeField] private StuyPulseSetpoint climbStow;
        [SerializeField] private StuyPulseSetpoint climbPrep;
        [SerializeField] private StuyPulseSetpoint climbClimb;

        [Header("Intakes and Stows")] 
        [SerializeField] private ReefscapeGamePieceIntake funnelCoralIntake;
        [SerializeField] private ReefscapeGamePieceIntake shooterAlgaeIntake;
        [SerializeField] private ReefscapeGamePieceIntake froggyCoralIntake;
        [SerializeField] private ReefscapeGamePieceIntake froggyAlgaeIntake;

        [SerializeField] private GamePieceState shooterCoralStowState;
        [SerializeField] private GamePieceState shooterAlgaeStowState;
        
        [SerializeField] private GamePieceState froggyCoralStowState;
        [SerializeField] private GamePieceState froggyAlgaeStowState;

        [Header("Auto Align Offsets")] 
        [SerializeField] private AutoAlignOffset frontLeftOffset;
        [SerializeField] private AutoAlignOffset frontRightOffset;
        [SerializeField] private AutoAlignOffset backLeftOffset;
        [SerializeField] private AutoAlignOffset backRightOffset;
        [SerializeField] private AutoAlignOffset frontLeftL4Offset;
        [SerializeField] private AutoAlignOffset frontRightL4Offset;
        [SerializeField] private AutoAlignOffset backLeftL4Offset;
        [SerializeField] private AutoAlignOffset backRightL4Offset;
        
        [SerializeField] private AutoAlignOffset L1FroggyOffset;

        [Header("Froggy Shit")]
        [SerializeField] private Transform forggyCoralTarget;
        [SerializeField] private Transform frogyCoralSlid;

        [Header("Random Release Shit")] 
        [SerializeField] private Vector3 shooterProcRelease;
        [SerializeField] private Vector3 shooterNetRelease;
        [SerializeField] private Vector3 ForgyProcRelease;
        
        [SerializeField] private Vector3 forgCorlaScor;
        
        [Header("Rollers n Other Stuff ig")]
        [SerializeField] private GenericRoller[] froggyRollers;
        
        
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode
            _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode
            _algaeController;

        private float _elevatorTargetHeight;
        private float _eeArmTargetAngle;
        private float _froggyTargetAngle;
        private float _climbPivot1TargetAngle;
        private float _climbPivot2TargetAngle;

        private ReefscapeAutoAlign align;
        
        protected override void Start()
        {
            base.Start();
            SetRobotMode(ReefscapeRobotMode.Coral);
            
            eeArm.SetPid(eeArmPid);
            froggy.SetPid(froggyPid);
            climbPivot1.SetPid(climbPivotsPid);
            climbPivot2.SetPid(climbPivotsPid);

            _elevatorTargetHeight = 0;
            _eeArmTargetAngle = 0;
            _froggyTargetAngle = 0;
            _climbPivot1TargetAngle = 0;
            _climbPivot2TargetAngle = 0;
            
            RobotGamePieceController.SetPreload(shooterCoralStowState);
            _coralController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController = RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[]
            {
                shooterCoralStowState, 
                froggyCoralStowState
            };
            _coralController.intakes.Add(funnelCoralIntake);
            _coralController.intakes.Add(froggyCoralIntake);

            _algaeController.gamePieceStates = new[]
            {
                shooterAlgaeStowState,
                froggyAlgaeStowState
            };
            _algaeController.intakes.Add(shooterAlgaeIntake);
            _algaeController.intakes.Add(froggyAlgaeIntake);
            
            align = gameObject.GetComponent<ReefscapeAutoAlign>();
        }

        private void SetSetpoint(StuyPulseSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _eeArmTargetAngle = setpoint.eeArmAngle;
            _froggyTargetAngle = setpoint.froggyAngle;
            _climbPivot1TargetAngle = setpoint.climbPivot1Angle;
            _climbPivot2TargetAngle = setpoint.climbPivot2Angle;
        }

        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            eeArm.SetTargetAngle(_eeArmTargetAngle).withAxis(JointAxis.X).noWrap(20);
            froggy.SetTargetAngle(_froggyTargetAngle).withAxis(JointAxis.X);
            climbPivot1.SetTargetAngle(_climbPivot1TargetAngle).withAxis(JointAxis.X).noWrap(140);
            climbPivot2.SetTargetAngle(-1 * _climbPivot2TargetAngle).withAxis(JointAxis.X).noWrap(-140);
        }

        private void LateUpdate()
        {
            eeArm.UpdatePid(eeArmPid);
            froggy.UpdatePid(froggyPid);
            climbPivot1.UpdatePid(climbPivotsPid);
            climbPivot2.UpdatePid(climbPivotsPid);
        }

        private void SetAlignOffsets(AutoAlignOffset alignment)
        {
            align.offset = new Vector3(alignment.xOffset, alignment.yOffset, alignment.zOffset);
            align.rotation = alignment.Rotation;
        }

        private void AutoAlignnnn()
        {
            // if (CurrentIntakeMode == ReefscapeIntakeMode.L1 && _coralController.currentStateNum == froggyCoralStowState.stateNum && _coralController.atTarget)
            // {
            //     SetAlignOffsets(L1FroggyOffset);
            // }
            if (AutoAlignLeftAction.IsPressed() && FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? frontLeftL4Offset : frontLeftOffset);
            }
            else if (AutoAlignRightAction.IsPressed() && FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? frontRightL4Offset : frontRightOffset);
            }
            else if (AutoAlignLeftAction.IsPressed() && !FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? backLeftL4Offset : backLeftOffset);
            }
            else if (AutoAlignRightAction.IsPressed() && !FacingReef && CurrentSetpoint !=  ReefscapeSetpoints.Place)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? backRightL4Offset : backRightOffset);
            }
        }

        private void PlacePiece()
        {
            if (LastSetpoint != ReefscapeSetpoints.L2 && LastSetpoint != ReefscapeSetpoints.L3 && LastSetpoint != ReefscapeSetpoints.L4 && _coralController.HasPiece() && !(_coralController.currentStateNum == shooterCoralStowState.stateNum && _coralController.atTarget))
            {
                _coralController.ReleaseGamePieceWithForce(forgCorlaScor);
            }
            else if (_algaeController.HasPiece())
            {
                if (_algaeController.currentStateNum == shooterAlgaeStowState.stateNum && LastSetpoint == ReefscapeSetpoints.Barge)
                {
                _algaeController.ReleaseGamePieceWithForce(shooterNetRelease);
                } else if (_algaeController.currentStateNum == shooterAlgaeStowState.stateNum && LastSetpoint == ReefscapeSetpoints.Processor)
                {
                    _algaeController.ReleaseGamePieceWithForce(shooterProcRelease);
                }
                else
                {
                    _algaeController.ReleaseGamePieceWithForce(ForgyProcRelease);
                }
            }
            else if (LastSetpoint == ReefscapeSetpoints.L4)
            {
                _coralController.ReleaseGamePieceWithForce(FacingReef
                                                            ? new Vector3(0, 0, -6)
                                                            : new Vector3(0, 0, 5));
            }
            else if (LastSetpoint == ReefscapeSetpoints.L1 && CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, 3.5f), 0.2f, .9f);
            }
            else
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 5));
            }
        }
        
        private void FixedUpdate()
        {
            bool hasAlgae = _algaeController.HasPiece();
            bool hasCoral = _coralController.HasPiece();
            
            bool shooterHasAlgae = (_algaeController.currentStateNum == shooterAlgaeStowState.stateNum && _algaeController.atTarget);
            bool shooterHasCoral = (_coralController.currentStateNum == shooterCoralStowState.stateNum && _coralController.atTarget);
            
            if (froggyCoralIntake.GamePiece != null)
            {
                var localSliderSpaceZ = forggyCoralTarget.transform.InverseTransformPoint(froggyCoralIntake.GamePiece.transform.position).z;
                frogyCoralSlid.localPosition = new Vector3(0, 0, localSliderSpaceZ);
            }

            if (hasCoral && !shooterHasCoral && CurrentIntakeMode == ReefscapeIntakeMode.L1 && CurrentSetpoint == ReefscapeSetpoints.Place)
            {
                foreach (var roller in froggyRollers)
                {
                    roller.flipVelocity();
                }
            }
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(stow);
                    _coralController.RequestIntake(funnelCoralIntake, CurrentIntakeMode != ReefscapeIntakeMode.L1);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.Intake:

                    if (CurrentIntakeMode == ReefscapeIntakeMode.L1 && !hasCoral && CurrentRobotMode == ReefscapeRobotMode.Coral)
                    {
                        SetSetpoint(froggyCoral);
                        _coralController.SetTargetState(froggyCoralStowState);
                        _coralController.RequestIntake(froggyCoralIntake);
                        _coralController.RequestIntake(funnelCoralIntake, false);
                    }
                    else if (CurrentRobotMode == ReefscapeRobotMode.Coral && !hasCoral)
                    {
                        SetSetpoint(intakeFunnel);
                        _coralController.SetTargetState(shooterCoralStowState);
                        _coralController.RequestIntake(funnelCoralIntake, !shooterHasAlgae);
                        _coralController.RequestIntake(froggyCoralIntake, false);
                    }
                    else if (CurrentRobotMode == ReefscapeRobotMode.Algae && (CurrentSetpoint == ReefscapeSetpoints.HighAlgae || CurrentSetpoint != ReefscapeSetpoints.LowAlgae) && !hasAlgae && !shooterHasCoral)
                    {
                        _algaeController.SetTargetState(shooterAlgaeStowState);
                        _algaeController.RequestIntake(shooterAlgaeIntake);
                        _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    }
                    if (CurrentRobotMode == ReefscapeRobotMode.Algae && !hasAlgae && (hasCoral && !shooterHasCoral))
                    {
                        SetSetpoint(froggyAlgae);
                        _algaeController.SetTargetState(froggyAlgaeStowState);
                        _algaeController.RequestIntake(froggyAlgaeIntake);
                        _algaeController.RequestIntake(shooterAlgaeIntake, false);
                    }
                    
                    break;
                case ReefscapeSetpoints.Place:
                    if (LastSetpoint == ReefscapeSetpoints.Barge)
                    {
                        SetSetpoint(bargePlace);
                    }
                    else if (LastSetpoint == ReefscapeSetpoints.L4)
                    {
                        SetSetpoint(FacingReef ? frontL4 : backL4Scored);
                    }
                    PlacePiece();
            break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(shooterHasCoral? eeL1 : froggyCoralPlace);

                    _algaeController.RequestIntake(funnelCoralIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(lollipopIntake);
                    _algaeController.SetTargetState(shooterAlgaeStowState);
                    _algaeController.RequestIntake(shooterAlgaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                    break;
                case ReefscapeSetpoints.L2:
                    if (shooterHasCoral)
                    {
                        SetSetpoint(FacingReef ? frontL2 : backL2);
                    }

                    _algaeController.RequestIntake(funnelCoralIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(FacingReef ? frontLowAlgae : backLowAlgae);
                    _algaeController.SetTargetState(shooterAlgaeStowState);
                    _algaeController.RequestIntake(shooterAlgaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                    break;
                case ReefscapeSetpoints.L3:
                    if (shooterHasCoral)
                    {
                        SetSetpoint(FacingReef ? frontL3 : backL3);
                    }

                    _algaeController.RequestIntake(funnelCoralIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(FacingReef ? frontHighAlgae : backHighAlgae);
                    _algaeController.SetTargetState(shooterAlgaeStowState);
                    _algaeController.RequestIntake(shooterAlgaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                    break;
                case ReefscapeSetpoints.L4:
                    _algaeController.RequestIntake(funnelCoralIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    if (shooterHasCoral)
                    {
                        SetSetpoint(FacingReef ? frontL4 : backL4);
                    }

                    AutoAlignnnn();
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(shooterHasAlgae ? process : froggyAlgaeProcess);
                    _algaeController.RequestIntake(shooterAlgaeIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.Barge:
                    SetSetpoint(bargePrep);
                    _algaeController.RequestIntake(shooterAlgaeIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    break;
                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climbPrep);
                    _algaeController.RequestIntake(shooterAlgaeIntake, false);
                    _coralController.RequestIntake(froggyCoralIntake, false);
                    _coralController.RequestIntake(shooterAlgaeIntake, false);
                    _algaeController.RequestIntake(froggyAlgaeIntake, false);
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbClimb);
                    break;
            }
            
            UpdateSetpoints();
            AutoAlignnnn();
        }


    }

}