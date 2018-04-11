using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//===================== Basic functions that are needed ====================//

namespace CusMaths
{

    public class Functions
    {

        //calculates a vector 3 speed that is distance input -> aim divided by float time
        public static Vector3 calculateSmoothStep(Vector3 input, Vector3 aim, float time)
        {
            Vector3 output = Vector3.zero;

            //check if time is equal to 0 if so set to 1
            if (time == 0)
            {
                time = 1;
            }

            //calculate output calculateSmoothStep
            output.x = calculateSmoothStep(input.x, aim.x, time);
            output.y = calculateSmoothStep(input.y, aim.y, time);
            output.z = calculateSmoothStep(input.z, aim.z, time);

            return output;
        }

        //calculates distance so that distance * time = input
        public static float calculateSmoothStep(float input, float aim, float time)
        {

            //divide total distance by time to find per second value
            return (input - aim) / time;

        }

        //probably not needed
        //takes vector 3 as input and aim and mathf smoothsteps between them based on float speed
        public static Vector3 vec3SmoothStep(Vector3 input, Vector3 aim, float speed)
        {
            Vector3 output = input;

            //apply Mathf smoothstep to each vec variable and return output
            output.x = Mathf.SmoothStep(output.x, aim.x, Time.deltaTime * speed);
            output.y = Mathf.SmoothStep(output.y, aim.y, Time.deltaTime * speed);
            output.z = Mathf.SmoothStep(output.z, aim.z, Time.deltaTime * speed);

            return output;
        }

        //multiplies two vector 3s together
        public static Vector3 vec3Times(Vector3 one, Vector3 two)
        {
            return new Vector3(one.x * two.x, one.y * two.y, one.z * two.z);
        }

        //divides vector3 one by vector3 two
        public static Vector3 vec3Divide(Vector3 one, Vector3 two)
        {
            return new Vector3(one.x / two.x, one.y / two.y, one.z / two.z);
        }

        //takes float input and checks if its within bounds minMax
        public static bool checkWithin(float input, Vector2 minMax)
        {

            //check if minMax.x is greater than y
            if (minMax.x > minMax.y)
            {
                //check if input is less than x and larger than y
                if (input > minMax.x || input < minMax.y)
                {
                    return true;
                }
                else
                {
                    //check if input is closer to x or y
                    if (minMax.y - input < input - minMax.x)
                    {
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }

            }
            else
            {
                //check if input is less than minMax.x
                if (input <= minMax.x)
                {
                    return false;
                }
                //or input is less than minMax.y
                else if (input > minMax.y)
                {
                    return false;
                }
            }
            return true;
        }

        //checks if vector3 input variables are within vector2 within bounds based on bool inputs
        public static Vector3 forceWithin(Vector3 input, Vector2 within, bool x = false, bool y = false, bool z = false)
        {
            Vector3 output = input;

            //take bool inputs and check if variables in vector are within bounds
            if (x)
            {
                output.x = forceWithin(output.x, within);
            }
            else if (y)
            {
                output.y = forceWithin(output.y, within);
            }
            else if (z)
            {
                output.z = forceWithin(output.z, within);
            }

            return output;
        }

        //takes float input and checks if its within bounds minMax
        public static float forceWithin(float input, Vector2 minMax)
        {

            //check if minMax.x is greater than y
            if (minMax.x > minMax.y)
            {
                //check if input is less than x and larger than y
                if (input > minMax.x || input < minMax.y)
                {
                    return input;
                }
                else
                {
                    //check if input is closer to x or y
                    if (minMax.y - input < input - minMax.x)
                    {
                        return minMax.x;
                    }
                    else
                    {
                        return minMax.y;
                    }
                }

            }
            else
            {
                //check if input is less than minMax.x
                if (input <= minMax.x)
                {
                    return minMax.x;
                }
                //or input is less than minMax.y
                else if (input > minMax.y)
                {
                    return minMax.y;
                }
            }
            return input;
        }

        //check if input is within speed distance to aim
        public static Vector3 checkResetMargin(Vector3 input, Vector3 aim, Vector3 speed)
        {
            Vector3 output = input;

            output.x = checkResetMargin(input.x, aim.x, speed.x);
            output.y = checkResetMargin(input.y, aim.y, speed.y);
            output.z = checkResetMargin(input.z, aim.z, speed.z);

            return output;
        }

        //check if input is within speed distance to aim
        public static float checkResetMargin(float input, float aim, float speed)
        {

            //check if input is within speed of aim
            if (aim - input > -speed && aim - input < speed)
            {
                return aim;
            }
            //return original input
            return input;
        }

        //checks if input is within bounds if not return closest bound value
        public static float lockFloat(float input, Vector2 bounds)
        {
            float output = input;

            if (output < bounds.x)
            {
                output = bounds.x;
            }

            if (output > bounds.y)
            {
                output = bounds.y;
            }

            return output;
        }

    }

}