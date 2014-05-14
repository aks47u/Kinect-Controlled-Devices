using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NKH.MindSqualls;
using WindowsInput;

namespace WindowsFormsKinectNXT
{
    public class NXTBlock : KinectControlledDevice
    {
        public int ID;


        private NxtBrick brick;
        private NxtMotorSync motorPair;
        private int mPower = 50;
        private int direction = 0;
        private int forward = 1;
        private int reverse = -1;
        private int stopped = 0;
        private bool turning = false;
        private bool motorAisRunning = false;
        private bool motorCisRunning = false;

        public const VirtualKeyCode REVERSE = VirtualKeyCode.NUMPAD2;
        public const VirtualKeyCode FORWARD = VirtualKeyCode.NUMPAD8;
        public const VirtualKeyCode STOP = VirtualKeyCode.NUMPAD5;
        public const VirtualKeyCode TURN_RIGHT = VirtualKeyCode.NUMPAD6;
        public const VirtualKeyCode TURN_LEFT = VirtualKeyCode.NUMPAD4;
        // public const VirtualKeyCode INCREASE_SPEED = VirtualKeyCode.ADD;
        // public const VirtualKeyCode DECREASE_SPEED = VirtualKeyCode.SUBTRACT;

        /**
         * Create an instance of NXTBlock with the given id.
         * Creates a default motorpair on motors A and C
         */
        public NXTBlock(int id)
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

        /*
         * Connects to the NXTBrick with the given comport using bluetooth
         */
        public bool ConnectToDevice(byte comPort)
        {
            return this.ConnectToDevice(NxtCommLinkType.Bluetooth, comPort);
        }

        /*
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


        /*
         * returns true if the action was handled
         */
        public bool PerformKeyCodeAction(WindowsInput.VirtualKeyCode keycode)
        {
            switch (keycode)
            {
                case FORWARD:
                    DriveForward();
                    break;
                case REVERSE:
                    DriveBackward();
                    break;
                case TURN_LEFT:
                    TurnLeft();
                    break;
                case TURN_RIGHT:
                    TurnRight();
                    break;
                case STOP:
                    Yield();
                    break;
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
         * Perform a right turn.
         * When this method completed the flag turning is set to true.
         */
        private void TurnRight()
        {
            if (direction == forward)
            {
                this.brick.MotorA.Brake();
                motorAisRunning = false;
                this.brick.MotorC.Run((sbyte)(mPower * direction), 0);
                motorCisRunning = true;
            }
            else if (direction == reverse)
            {
                this.brick.MotorC.Brake();
                motorCisRunning = false;
                this.brick.MotorA.Run((sbyte)(mPower * direction), 0);
                motorAisRunning = true;
            }
            turning = true;
        }


        /**
         * Perform a left turn.
         * When this method completed the flag turning is set to true.
         */
        private void TurnLeft()
        {
            if (direction == forward)
            {
                this.brick.MotorC.Brake();
                motorCisRunning = false;
                this.brick.MotorA.Run((sbyte)(mPower * direction), 0);
                motorAisRunning = true;
            }
            else if (direction == reverse)
            {
                this.brick.MotorA.Brake();
                motorAisRunning = false;
                this.brick.MotorC.Run((sbyte)(mPower * direction), 0);
                motorCisRunning = true;
            }
            turning = true;
        }


        /**
         * Drives the car backwards.
         * If the car is already going backwards then calling this method does nothing,
         * However If its turning then the turn is interrupted and the car drives backward.
         * When this method completed the flag direction is set to reverse and turning is set to false.
         */
        private void DriveBackward()
        {
            if (direction != reverse || turning)
            {
                this.motorPair.Run((sbyte)(mPower * reverse), 0, 0);
                direction = reverse;
                turning = false;
                motorAisRunning = true;
                motorCisRunning = true;
            }
        }


        /**
         * Drives the car forward.
         * If the car is already going forward then calling this method does nothing,
         * However If its turning then the turn is interrupted and the car drives forward.
         * When this method completed the flag direction is set to forward and turning is set to false.
         */
        private void DriveForward()
        {
            if (direction != forward || turning)
            {
                this.motorPair.Run((sbyte)(mPower * forward), 0, 0);
                direction = forward;
                turning = false;
                motorAisRunning = true;
                motorCisRunning = true;
            }
        }



        /**
         * sets the speed of the NXTCar
         */
        public void setSpeed(int speed)
        {
            this.mPower = speed;

            if (motorAisRunning && motorCisRunning)
            {
                this.motorPair.Run((sbyte)(mPower * direction), 0, 0);
            }
            else if (motorAisRunning)
            {
                this.brick.MotorA.Run((sbyte)(mPower * direction), 0);
            }
            else if (motorCisRunning)
            {
                this.brick.MotorC.Run((sbyte)(mPower * direction), 0);
            }
        }


        // public void IncreaseSpeed()
        // {
        // if (this.mPower < 90)
        // this.setSpeed(mPower + 10);
        // }

        // public void DecreaseSpeed()
        // {
        // if (this.mPower > 10)
        // this.setSpeed(mPower - 10);
        // }

        public string GestureFileName()
        {
            return "gestures.xml";
        }

        public int CheckBatteryLevel()
        {
            return (int)this.brick.BatteryLevel;
        }
    }
}
