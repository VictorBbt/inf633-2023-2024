using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BoidHelper
{

    //// Number of rays that we throw for the vision
    //const int numViewDirections = 50;
    //public static readonly Vector3[] directions;

    //static BoidHelper()
    //{
    //    directions = new Vector3[BoidHelper.numViewDirections];
    //    float startAngle = -25f;
    //    for (int i = 0; i < numViewDirections; i++)
    //    {
    //        float t = (float)i / numViewDirections;
    //        float inclination = Mathf.Acos(1 - 2 * t);
    //        float azimuth = angleIncrement * i;

    //        float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
    //        float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
    //        float z = Mathf.Cos(inclination);
    //        directions[i] = new Vector3(x, y, z);
    //    }
    //}

}