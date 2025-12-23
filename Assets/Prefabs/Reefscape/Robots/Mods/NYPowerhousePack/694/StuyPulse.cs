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

        [SerializeField] private GamePieceState shooterCoralHalfwayState;
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
                shooterCoralHalfwayState,
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
            _climbPivot1TargetAngle = setpoint.climbPivotsAngle;
            _climbPivot2TargetAngle = setpoint.climbPivotsAngle;
        }

        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            eeArm.SetTargetAngle(_eeArmTargetAngle).withAxis(JointAxis.X).noWrap(20);
            froggy.SetTargetAngle(_froggyTargetAngle).withAxis(JointAxis.X);
            climbPivot1.SetTargetAngle(_climbPivot1TargetAngle).withAxis(JointAxis.X);
            climbPivot2.SetTargetAngle(_climbPivot2TargetAngle).withAxis(JointAxis.X);
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
        }

        private void AutoAlignnnn()
        {
            if (AutoAlignLeftAction.triggered && FacingReef)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? frontLeftL4Offset : frontLeftOffset);
            }
            else if (AutoAlignRightAction.triggered && FacingReef)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? frontRightL4Offset : frontRightOffset);
            }
            else if (AutoAlignLeftAction.triggered && !FacingReef)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? backLeftL4Offset : backLeftOffset);
            }
            else if (AutoAlignRightAction.triggered && !FacingReef)
            {
                SetAlignOffsets(CurrentSetpoint == ReefscapeSetpoints.L4 ? backRightL4Offset : backRightOffset);
            }
        }

        private void PlacePiece()
        {
            if (CurrentIntakeMode == ReefscapeIntakeMode.L1 && _coralController.HasPiece())
            {
                _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 2));
            }
            else if (_algaeController.HasPiece())
            {
                _algaeController.ReleaseGamePieceWithForce(CurrentIntakeMode == ReefscapeIntakeMode.L1 ? new Vector3(0, 2, 0) : new Vector3(0, 0, 4));
            }
            else if (LastSetpoint == ReefscapeSetpoints.L4)
            {
                _coralController.ReleaseGamePieceWithForce(FacingReef
                                                            ? new Vector3(0, 0, -6)
                                                            : new Vector3(0, 0, 5));
            }
            else if (LastSetpoint == ReefscapeSetpoints.L1 && CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            {
                _coralController.ReleaseGamePieceWithContinuedForce(new Vector3(0, 0, 3), 0.4f, 0.8f);
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
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(stow);
                    break;
                case ReefscapeSetpoints.Intake:
                    if (CurrentIntakeMode == ReefscapeIntakeMode.L1 && CurrentRobotMode == ReefscapeRobotMode.Algae && _algaeController.currentStateNum == 0 && !hasCoral)
                    {
                        SetSetpoint(froggyAlgae);
                        _algaeController.RequestIntake(froggyAlgaeIntake);
                    }
                    else
                    {
                        if (CurrentIntakeMode == ReefscapeIntakeMode.Normal)
                        {
                            SetSetpoint(intakeFunnel);
                            _coralController.RequestIntake(funnelCoralIntake, (CurrentRobotMode == ReefscapeRobotMode.Coral || hasAlgae) && !hasCoral);
                            _coralController.RequestIntake(froggyCoralIntake, false);
                            
                        }
                        else
                        {
                            SetSetpoint(froggyCoral);
                            _coralController.RequestIntake(froggyCoralIntake, (CurrentRobotMode == ReefscapeRobotMode.Coral || hasAlgae) && !hasCoral);
                            _coralController.RequestIntake(funnelCoralIntake, false);
                        }

                        if (hasCoral)
                        {
                            SetRobotMode(ReefscapeRobotMode.Coral);
                        }
                        
                    }
                    
                    _algaeController.RequestIntake(shooterAlgaeIntake, CurrentRobotMode == ReefscapeRobotMode.Algae && _algaeController.currentStateNum == 0);
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
                    AutoAlignnnn();
            break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(eeL1);
                    AutoAlignnnn();
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(lollipopIntake);
                    _algaeController.RequestIntake(shooterAlgaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                    AutoAlignnnn();
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(FacingReef ? frontL2 : backL2);
                    _coralController.SetTargetState(shooterCoralStowState);
                    AutoAlignnnn();
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(FacingReef ? frontLowAlgae : backLowAlgae);
                    _algaeController.RequestIntake(shooterAlgaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                    AutoAlignnnn();
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(FacingReef ? frontL3 : backL3);
                    _coralController.SetTargetState(shooterCoralStowState);
                    AutoAlignnnn();
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(FacingReef ? frontHighAlgae : backHighAlgae);
                    _algaeController.RequestIntake(shooterAlgaeIntake, IntakeAction.IsPressed() && !hasAlgae);
                    AutoAlignnnn();
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(FacingReef ? frontL4 : backL4);
                    AutoAlignnnn();
                    _coralController.SetTargetState(shooterCoralStowState);
                    AutoAlignnnn();
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(process);
                    break;
                case ReefscapeSetpoints.Barge:
                    SetSetpoint(bargePrep);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    
                    break;
                case ReefscapeSetpoints.Climb:
                    break;
                case ReefscapeSetpoints.Climbed:
                    break;
            }
            
            UpdateSetpoints();
            AutoAlignnnn();
        }


    }

}