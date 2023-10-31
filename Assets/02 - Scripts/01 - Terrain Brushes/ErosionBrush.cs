using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ErosionBrush : TerrainBrush
{

    // parameters taken from the Sebastian Lague GitHub
    // https://github.com/SebLague/Hydraulic-Erosion/tree/master/Assets/Scripts
    public bool GlobalErosion = false; // whether to erode within the brush radius or on all the terrain
    [Range(2,8)]
    public int erosionRadius = 3;
    [Range(0, 1)]
    public float inertia = .05f; // An inertia of 0 and the water will slide on the terrain, 1 and it will stay in place
    public float sedimentCapacityFactor = 4; // Multiplier for how much sediment a droplet can carry
    public float minSedimentCapacity = .01f; // Used to prevent carry capacity getting too close to zero on flatter terrain
    [Range(0, 1)]
    public float erodeSpeed = .3f;
    [Range(0, 1)]
    public float depositSpeed = .3f;
    [Range(0, 1)]
    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public int maxDropletLifetime = 30;

    public float initialWaterVolume = 1;
    public float initialSpeed = 1;

    public float directionErosionFactor = 2;

    public int NumIterations = 1;

    System.Random rand = new System.Random();

    public override void draw(int x, int z)
    {

        for (int iter = 0; iter < NumIterations; iter++)
        {

            // generating random position within the square of side radius
            float posX;
            float posZ;
            if (GlobalErosion)
            {
                posX = rand.Next(0, (int)terrain.gridSize().x - 1);
                posZ = rand.Next(0, (int)terrain.gridSize().z - 1);
            } else {
                float signX = rand.Next(2);
                float signZ = rand.Next(2);
                posX = x + (float)(rand.NextDouble()) * radius * (2 * signX - 1);
                posZ = z + (float)(rand.NextDouble()) * radius * (2 * signZ - 1);
            }

            terrain.debug.text = "Droplet at " + new Vector2(posX, posZ).ToString();
            float dirX = 0;
            float dirZ = 0;
            float speed = initialSpeed;
            float water = initialWaterVolume;
            float sediment = 0;

            for (int lifetime = 0; lifetime < maxDropletLifetime; lifetime++)
            {
                // Coord of the nearest point on the grid
                int nodeX = (int)(posX);
                int nodeZ = (int)(posZ);

                // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
                float cellOffsetX = posX - nodeX;
                float cellOffsetZ = posZ - nodeZ;


                //terrain.debug.text = "Droplet at " + new Vector2(nodeX, nodeZ).ToString();
                //Computing the direction of the gradient with nearest point on the grid
                (float gx, float gz, float height) = getGradient(posX, posZ);
                //terrain.debug.text = "Gradient is " + grad.ToString();

                // Update the droplet's direction and position (move position 1 unit regardless of speed)
                dirX = (dirX * inertia - gx * (1 - inertia));
                dirZ = (dirZ * inertia - gz * (1 - inertia));
                //terrain.debug.text = "Direction is " + dirX.ToString() + dirZ.ToString();
                // Normalize direction
                float len = Mathf.Max(0.01f, Mathf.Sqrt(dirX * dirX + dirZ * dirZ));
                if (len != 0)
                {
                    dirX /= len;
                    dirZ /= len;
                }
                posX += dirX;
                posZ +=  dirZ;
                //terrain.debug.text = "dirX: " + dirX.ToString() + "\ndirZ: " + dirZ.ToString();
                //terrain.debug.text = "Ancient position: (" + (posX -dirX).ToString() + " ," + (posZ - dirZ).ToString() + ") \nNew position (" + posX.ToString() + " ," + posZ.ToString() + ")";

                if ((dirX == 0 && dirZ == 0) || (!isInMap(posX, posZ)))
                {
                    //bool t1 = (!(isInMap(nodeX, nodeZ)));
                    //terrain.debug.text = t1.ToString();
                    break;
                }

                // Find the droplet's new height and calculate the deltaHeight
                (float a, float b, float newHeight) = getGradient(posX, posZ);
                float deltaHeight = newHeight - height;

                // Calculate the droplet's sediment capacity (higher when moving fast down a slope and contains lots of water)
                float sedimentCapacity = Mathf.Max(-deltaHeight * speed * water * sedimentCapacityFactor, minSedimentCapacity);

                //terrain.debug.text = "Computed deltaH: " + deltaHeight.ToString() + "\nsedimentCapa: " + sedimentCapacity.ToString() + "\nsediment" + sediment.ToString();

                // If carrying more sediment than capacity, or if flowing uphill:
                if (sediment > sedimentCapacity || deltaHeight > 0)
                {
                    // If moving uphill (deltaHeight > 0) try fill up to the current height, otherwise deposit a fraction of the excess sediment
                    float amountToDeposit = (deltaHeight > 0) ? Mathf.Min(deltaHeight, sediment) : (sediment - sedimentCapacity) * depositSpeed;
                    sediment -= amountToDeposit;


                    // Add the sediment to the points arount the current node 
                    // Deposition is not distributed over a radius (like erosion) so that it can fill small pits
                    if(isInMap(nodeX, nodeZ))
                    {
                        terrain.set(nodeX, nodeZ, terrain.get(nodeX, nodeZ) + amountToDeposit * (1 - cellOffsetX) * (1 - cellOffsetZ));
                    }
                    if(isInMap(nodeX+1, nodeZ))
                    {
                        terrain.set(nodeX + 1, nodeZ, terrain.get(nodeX + 1, nodeZ) + amountToDeposit * (1 - cellOffsetZ) * cellOffsetX);
                    }   
                    if(isInMap(nodeX+1, nodeZ + 1))
                    {
                        terrain.set(nodeX + 1, nodeZ + 1, terrain.get(nodeX + 1, nodeZ + 1) + amountToDeposit * cellOffsetX * cellOffsetZ);
                    }
                    if(isInMap(nodeX, nodeZ + 1))
                    {
                        terrain.set(nodeX, nodeZ + 1, terrain.get(nodeX, nodeZ + 1) + amountToDeposit * cellOffsetZ * (1 - cellOffsetX));
                    }
                    
                    /* 
                     CODE THAT WAS USED BEFORE, WITH NODE-BASED APPROACHES RATHER THAN POSITION BASED
                   
                    for (int xi = nodeX - 2; xi <= nodeX + 2; xi ++)
                    {
                        for (int zi = nodeZ - 2; zi <= nodeZ + 2; zi ++)
                        {
                            if((xi == nodeX) && (zi == nodeZ))
                            {
                                terrain.set(xi, zi, newHeight - 1/25f*amountToDeposit);
                                //terrain.debug.text = "Deposed sediment at: " + new Vector2(xi, zi).ToString();
                            } else {
                                // we deposit on the edges around a lower amount of deposit, proportional to the distance between to current node
                                // we assume that O.5 is on the principal node

                                //terrain.set(xi, zi, terrain.get(xi,zi) + amountToDeposit * (0.5f - ((1/6f) * Mathf.Abs(nodeX - xi) + (1/6f) *Mathf.Abs(nodeZ - zi) )) );
                                terrain.set(xi, zi, terrain.get(xi, zi) + amountToDeposit * 1/25f);
                            }
                        }
                    }
                    */
                    //terrain.debug.text = "Deposed sediment: " + amountToDeposit.ToString(); 
                }
                else
                {

                    // Erode a fraction of the droplet's current carry capacity.
                    // Clamp the erosion to the change in height so that it doesn't dig a hole in the terrain behind the droplet
                    float amountToErode = Mathf.Min((sedimentCapacity - sediment) * erodeSpeed, -deltaHeight);

                    //terrain.debug.text = "Will erode: " + amountToErode.ToString() + " at center " + new Vector2(nodeX, nodeZ).ToString();
                    // Use erosion brush to erode all nodes inside the droplet's erosion radius

                    Vector2 dropletPos = new Vector2(nodeX , nodeZ);
                    // float normalizationFactor = 3f / (Mathf.PI * 1f * Mathf.Pow(erosionRadius, 2)); //volume of a cone of height 1
                    int cnt = 0;
                    var xList = new List<int>();
                    var zList = new List<int>();
                    var weightList = new List<float>();
                    float weightSum = 0f;

                    for (int i = nodeX - (erosionRadius ); i <= nodeX + erosionRadius ; i++)
                    {
                        for (int j = nodeZ - (erosionRadius ); j <= nodeZ + erosionRadius; j++)
                        {
                            if (isInMap(i, j))
                            {
                                // distance between droplet and vertice of coord (i, j)
                                float dist = (new Vector2(i, j) - dropletPos).magnitude;
                                if (dist <= erosionRadius)
                                {
                                    cnt += 1;
                                    xList.Add(i);
                                    zList.Add(j);

                                    // We must have a conic distribution of the sediments, so (1 - x/r)

                                    // We erode more in the dierction of flow of the droplet
                                    if (((i - posX) * dirX >= 0) && ((j - posZ) * dirZ >= 0))
                                    {
                                        float w = (1f - dist / erosionRadius)*directionErosionFactor;
                                        weightList.Add(w);
                                        weightSum += w;
                                    } else
                                    {
                                        float w = (1f - dist / erosionRadius);
                                        weightList.Add(w);
                                        weightSum += w;
                                    }
                                }

                            }
                        }

                    }

                    for (int c = 0; c < cnt; c++)
                    {
                        float xcoord = xList[c];
                        float zcoord = zList[c];
                        float weight = weightList[c];

                        float aroundNodeHeight = terrain.get(xcoord, zcoord); // height of the current node

                        float weighedErodeAmount = amountToErode * weight/weightSum;
                        float deltaSediment = (aroundNodeHeight < weighedErodeAmount) ? aroundNodeHeight : weighedErodeAmount;
                        terrain.set(xcoord, zcoord, aroundNodeHeight - deltaSediment);
                        //terrain.debug.text = "Delta sediment: " +  deltaSediment.ToString();
                        sediment += deltaSediment;
                    }
                }

                    /*
                    for (int i = -erosionRadius; i <= erosionRadius; i++)
                    {
                        for (int j = -erosionRadius; j <= erosionRadius; j++)
                        {
                            float dist = new Vector2(i, j).magnitude;
                            if ((dist <= erosionRadius) && (isInMap(nodeX + i, nodeZ + j)))
                            {
                                // We must have a conic distribution of the sediments, so (1 - x/r)
                                // We must deposit a volume amountToDeposit of sediments, and a cone has a volume of P*h*r*r/3
                                float aroundNodeHeight = terrain.get(nodeX + i, nodeZ + j); // height of the current node
                                float normalizationFactor = 3f / (Mathf.PI * 1f * Mathf.Pow(erosionRadius, 2)); //volume of a cone of height 1
                                float weighedErodeAmount = amountToErode * normalizationFactor * (1f - dist / erosionRadius);
                                float deltaSediment = (aroundNodeHeight < weighedErodeAmount) ? aroundNodeHeight : weighedErodeAmount;
                                terrain.set(nodeX + i, nodeZ + j, aroundNodeHeight - deltaSediment);
                                //terrain.debug.text = "Delta sediment: " +  deltaSediment.ToString();
                                sediment += deltaSediment;

                            }
                        }
                    }
                    */

                    //terrain.debug.text = "Eroded terrain: " + amountToErode.ToString();
                
                // Update droplet's speed and water content
                speed = Mathf.Sqrt(Mathf.Max(0, speed * speed + deltaHeight * gravity));
                water *= (1 - evaporateSpeed);
            }
        }
    }


    private (float, float, float) getGradient(float xpos, float zpos)
    {
        int xnode = (int)(xpos);
        int znode = (int)(zpos);

        float x = xpos - xnode;
        float z = zpos - znode;

        // Compute height of the nearby edges
        float heightSW = terrain.get(xnode, znode);
        float heightNW = terrain.get(xnode + 1, znode);
        float heightNE = terrain.get(xnode + 1, znode + 1);
        float heightSE = terrain.get(xnode, znode + 1);

        //terrain.debug.text ="Coord: " + new Vector2(xnode,znode).ToString() + "\nHeight of node SW: " + heightSW.ToString() + "\nHeight of node NW: " + heightNW.ToString() + "\nHeight of node NE: " + heightNE.ToString() + "\nHeight of node SE: " + heightSE.ToString();
        // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
        float gradientX = (heightNW - heightSW) * z + (heightNE - heightSE) * (1-z);
        float gradientZ = (heightSE - heightSW) * x + (heightNE - heightNW) *(1 - x);
        //terrain.debug.text = "GradX: " + gradientX.ToString() + "\nGradZ: " + gradientZ.ToString();
        // Calculate height with bilinear interpolation of the heights of the nodes of the cell
        float height = heightNE* x * z + heightSE * (1-x) * z + heightNW * x* (1-z) + heightSW * (1-x)*(1- z);
        //terrain.debug.text = "Height of node: " + heightSW.ToString() + "\nInterp height: " + height.ToString();
        return (gradientX, gradientZ, height);

    }

    private bool isInMap(float x, float z)
    {
        Vector3 gridSize = terrain.gridSize();
        bool xInRange = (x > erosionRadius) && (x < (float)(gridSize.x) - (float)(erosionRadius));
        //terrain.debug.text = xInRange.ToString();
        bool zInRange = (z > erosionRadius) && (z < (float)(gridSize.z) - (float)(erosionRadius));

        return xInRange && zInRange;
    }

    /*
         private Vector3 getGradientInt(int x, int z)
    {
        float currentHeight = terrain.get(x, z);
        Vector2 maxDropCoord = new Vector2(x, z);
        float maxDrop = 0f;

        for (int xi = x - 1; xi <= x + 1; xi ++)
        {
            for (int zi = z - 1; zi <= z + 1; zi ++)
            {
                float h = terrain.get(xi, zi);
                float drop = h - currentHeight;
                if ((drop < 0) && (Mathf.Abs(drop) >= maxDrop))
                {
                    maxDropCoord = new Vector2(xi, zi);
                    maxDrop = drop;
                }
            }
        }

        return new Vector3(maxDropCoord.x - x, maxDropCoord.y - z, maxDrop);
    }
    */
}
