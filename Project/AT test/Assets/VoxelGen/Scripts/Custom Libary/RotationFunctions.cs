using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//===================== Basic functions that affect rotations ====================//

namespace CusMaths
{

    public class RotationFunctions
    {

        //calculates a vector 3 speed that is distance input -> aim divided by float time
        public static Vector3 calculateSmoothStepAngle(Vector3 input, Vector3 aim, float time)
        {
            Vector3 output = Vector3.zero;

            //check if time is equal to 0 if so set to 1
            if (time == 0)
            {
                time = 1;
            }

            //calculate output calculateSmoothStep
            output.x = calculateSmoothStepAngle(input.x, aim.x, time);
            output.y = calculateSmoothStepAngle(input.y, aim.y, time);
            output.z = calculateSmoothStepAngle(input.z, aim.z, time);

            return output;
        }

        //calculate smooth step specifically for angles with a max 0-360 range
        public static float calculateSmoothStepAngle(float input, float aim, float time)
        {

            if (aim == 0 || aim == 360)
            {
                if (input < 180)
                {
                    return input / time;
                }
                else
                {
                    return (360 - input) / time;
                }
            }

            return Functions.calculateSmoothStep(input, aim, time);
        }

        //finds the fastest rotation from vector3 current to vector3 aim
        public static Vector3 findFastestRotation(Vector3 current, Vector3 aim)
        {
            Vector3 returnVal = aim;

            //find fastest rotation for each variable in vector
            returnVal.x = findFastestRotation(current.x, returnVal.x);
            returnVal.y = findFastestRotation(current.y, returnVal.y);
            returnVal.z = findFastestRotation(current.z, returnVal.z);

            return returnVal;
        }

        //find fastest rotation from current to aim
        public static float findFastestRotation(float current, float aim)
        {
            float returnVal = aim;

            //check if aim is less than current
            if (aim < current)
            {
                //if aim is less than current check if distance to aim is more than 180
                if (current - aim > 180)
                {
                    //return 361 so that rotation will go to 360 and reset to 0
                    returnVal = 361;
                }
            }
            else
            {
                //if aim is greater than current check if distance to aim is more than 180
                if (aim - current > 180)
                {
                    //return -1 so that rotation will go to 0 and reset to 360
                    returnVal = -1;
                }
            }
            return returnVal;
        }

        //checks all variables in vector3 input to see if they are within 360
        public static Vector3 checkWithin360(Vector3 input)
        {
            Vector3 output = input;

            //check if input is within 0-360 for each variable in vector
            output.x = checkWithin360(output.x);
            output.y = checkWithin360(output.y);
            output.z = checkWithin360(output.z);

            return output;
        }

        //checks if input is within 0-360
        public static float checkWithin360(float input)
        {
            float output = input;

            //check if input is less than 0 and set to 360 and vice-versa
            if (output < 0)
            {
                output = 360;
            }
            else if (output >= 360)
            {
                output = 0;
            }

            return output;
        }

        //checks whether speed is going in the right direction to make input equal aim
        public static Vector3 checkResetDir(Vector3 input, Vector3 aim, Vector3 speed)
        {
            Vector3 currentSpeed = speed;

            currentSpeed.x = checkResetDir(input.x, aim.x, speed.x);
            currentSpeed.y = checkResetDir(input.y, aim.y, speed.y);
            currentSpeed.z = checkResetDir(input.z, aim.z, speed.z);

            return currentSpeed;
        }

        //checks whether speed is going in the right direction to make input equal aim
        public static float checkResetDir(float input, float aim, float speed)
        {
            //tests to find fastest rotation
            float fastest = findFastestRotation(input, aim);

            //checks if fastest is not equal to aim
            if (fastest != aim)
            {

                //checks if fastest is smaller than input
                if (fastest < input)
                {

                    //checks if speed is going in the wrong direction
                    if (speed > 0)
                    {
                        //invert speed
                        return speed * -1;
                    }
                }
                else
                {

                    //checks if speed is going in the wrong direction
                    if (speed < 0)
                    {
                        //invert speed
                        return speed * -1;
                    }
                }
            }
            //check if fastests is smaller than input
            if (fastest < input)
            {

                //checks if speed is going in the wrong direction
                if (speed > 0)
                {
                    //invert speed
                    return speed * -1;
                }
            }
            else
            {

                //checks if speed is going in the wrong direction
                if (speed < 0)
                {
                    //invert speed
                    return speed * -1;
                }
            }

            //otherwise return original speed
            return speed;
        }

        //returns apply rotationChange with default variable due to vector2 no default param
        public static float applyRotationChange(float input, float change, bool add)
        {
            return applyRotationChange(input, change, add, new Vector2(0, 0), false);
        }

        //returns rotation change based on inputs and possible limit bounds
        public static float applyRotationChange(float input, float change, bool add, Vector2 minMax, bool enableLimit)
        {
            float output = input;

            if (add)
            {
                output += change;
            }
            else
            {
                output = change;
            }

            //check if limit is enabled
            if (enableLimit)
            {
                //limit output
                output = Functions.forceWithin(output, minMax);
            }

            return output;
        }

        //check if input is within speed distance to aim
        public static Vector3 checkResetMarginAngle(Vector3 input, Vector3 aim, Vector3 speed)
        {
            Vector3 output = input;

            output.x = checkResetMarginAngle(input.x, aim.x, speed.x);
            output.y = checkResetMarginAngle(input.y, aim.y, speed.y);
            output.z = checkResetMarginAngle(input.z, aim.z, speed.z);

            return output;
        }

        public static float checkResetMarginAngle(float input, float aim, float speed)
        {
            //check if aim is equal to 0 or 360 as they are the same thing
            if (aim == 0 || aim == 360)
            {
                //check if input is within speed to 0 or 360
                if (input < speed || input > 360 - speed)
                {
                    //return destination
                    return aim;
                }
            }

            return Functions.checkResetMargin(input, aim, speed);
        }

    }

}