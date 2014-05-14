using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NKH.MindSqualls;
using WindowsInput;

namespace WindowsFormsKinectNXT
{
    public class NXTCar : KinectControlledDevice
    {
        public int ID;


        private NxtBrick brick;
        private NxtMotorSync motorPair;
        // private int mPower = 0;
        private int mPower = 50;
        private int direction = 0;
        private int forward = 1;
        private int reverse = -1;
        private int stopped = 0;
        private bool turning = false;
        private bool motorAisRunning = false;
        private bool motorCisRunning = false;


        // public const VirtualKeyCode FORWARD_LEFT = VirtualKeyCode.NUMPAD7;
        public const VirtualKeyCode TURN_LEFT = VirtualKeyCode.NUMPAD4;
        // public const VirtualKeyCode REVERSE_LEFT = VirtualKeyCode.NUMPAD1;

        // public const VirtualKeyCode FORWARD_RIGHT = VirtualKeyCode.NUMPAD9;
        public const VirtualKeyCode TURN_RIGHT = VirtualKeyCode.NUMPAD6;
        // public const VirtualKeyCode REVERSE_RIGHT = VirtualKeyCode.NUMPAD3;

        public const VirtualKeyCode FORWARD_BOTH = VirtualKeyCode.NUMPAD8;
        public const VirtualKeyCode STOP_BOTH = VirtualKeyCode.NUMPAD5;
        public const VirtualKeyCode REVERSE_BOTH = VirtualKeyCode.NUMPAD2;

        // public const VirtualKeyCode INCREASE_SPEED = VirtualKeyCode.ADD;
        // public const VirtualKeyCode DECREASE_SPEED = VirtualKeyCode.SUBTRACT;

        /**
         * Create an instance of NXTBlock with the given id.
         * Creates a default motorpair on motors A and C
         */
        public NXTCar(int id)
        {
            this.ID = id;
        }

        public bool IsConnected()
        {
            if (this.brick == null)
            {
                return false;
            }
            return this.brick.IsConnected;
        }

        /**
         * Connects to the NXTBrick with the given comport using bluetooth
         */
        public bool ConnectToDevice(byte comPort)
        {
            return this.ConnectToDevice(NxtCommLinkType.Bluetooth, comPort);
        }

        /**
         * Connects to the NXTBrick with the given comport using the given connectiontype
         */
        public bool ConnectToDevice(NxtCommLinkType connectionType, byte comPort)
        {
            brick = new NxtBrick(connectionType, comPort);

            brick.MotorA = new NxtMotor();
            brick.MotorC = new NxtMotor();

            motorPair = new NxtMotorSync(brick.MotorA, brick.MotorC);

            try
            {
                brick.Connect();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /**
         * Stops the car and disconnects from the device.
         */
        public bool Disconnect()
        {
            Yield();

            if (brick != null && brick.IsConnected)
            {
                brick.Disconnect();
            }

            brick = null;
            motorPair = null;

            return true;
        }

        /**
         * returns true if the action was handled
         */
        public bool PerformKeyCodeAction(WindowsInput.VirtualKeyCode keycode)
        {
            switch (keycode)
            {
                case FORWARD_BOTH:
                    // DriveLeftMotor(this.forward);
                    // DriveRightMotor(this.forward);
                    this.motorPair.Run(50, 0, 0);
                    direction = forward;
                    turning = false;
                    motorAisRunning = true;
                    motorCisRunning = true;
                    break;
                case REVERSE_BOTH:
                    // DriveLeftMotor(this.reverse);
                    // DriveRightMotor(this.reverse);
                    this.motorPair.Run(-50, 0, 0);
                    direction = reverse;
                    turning = false;
                    motorAisRunning = true;
                    motorCisRunning = true;
                    break;
                case STOP_BOTH:
                    Yield();
                    break;
                // bcase FORWARD_LEFT:
                // DriveLeftMotor(this.forward);
                // break;
                case TURN_LEFT:
                    Yield();
                    break;
                // case REVERSE_LEFT:
                // DriveLeftMotor(this.reverse);
                // break;
                // case FORWARD_RIGHT:
                // DriveRightMotor(this.forward);
                // break;
                case TURN_RIGHT:
                    Yield();
                    break;
                // case REVERSE_RIGHT:
                // DriveRightMotor(this.reverse);
                // break;
                // case INCREASE_SPEED:
                // IncreaseSpeed();
                // break;
                // case DECREASE_SPEED:
                // DecreaseSpeed();
                // break;
                default:
                    return false;
            }
            return true;
        }

        /**
         * Perform a left turn.
         * When this method completed the flag turning is set to true.
         */
        private void Yield()
        {
            this.motorPair.Idle();
            direction = stopped;
            turning = false;
            motorAisRunning = false;
            motorCisRunning = false;
        }

        /**
         * Drives the car backwards.
         * If the car is already going backwards then calling this method does nothing,
         * However If its turning then the turn is interrupted and the car drives backward.
         * When this method completed the flag direction is set to reverse and turning is set to false.
         */
        private void DriveRightMotor(int direction)
        {
            this.brick.MotorC.Run((sbyte)(mPower * direction), 0);
        }

        private void DriveLeftMotor(int direction)
        {
            this.brick.MotorA.Run((sbyte)(mPower * direction), 0);
        }



        /**
         * sets the speed of the NXTCar
         */
        public void setSpeed(int speed)
        {
        this.mPower = speed;

        // if (motorAisRunning && motorCisRunning)
        // this.motorPair.Run((sbyte)(mPower * direction), 0, 0);
        // else if (motorAisRunning)
        // this.brick.MotorA.Run((sbyte)(mPower * direction), 0);
        // else if (motorCisRunning)
        // this.brick.MotorC.Run((sbyte)(mPower * direction), 0);
        }

        
        // public void IncreaseSpeed()
        // {
        // if (this.mPower < 90)
        // {
        // this.setSpeed(mPower + 10);
        // }
        // }

        // public void DecreaseSpeed()
        // {
        // if (this.mPower > 10)
        // {
        // this.setSpeed(mPower - 10);
        // }
        // }

        public string GestureFileName()
        {
            // return "generalkeyboardgestures.xml";
            return "gestures.xml";
        }

        public int CheckBatteryLevel()
        {
            return (int)this.brick.BatteryLevel;
        }
    }
}
