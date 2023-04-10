using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EulerianFluidSimulator;

//Most basic fluid simulator
//Based on: "How to write an Eulerian Fluid Simulator with 200 lines of code" https://matthias-research.github.io/pages/tenMinutePhysics/
//Eulerian means we simulate the fluid in a grid - not by using particles (Lagrangian). One can also use a combination of both methods
//Can simulate both liquids and gas
//Assume incompressible fluid with zero viscosity (inviscid) which are good approximations for water and gas
//To figure out:
// - Why no gravity in the wind tunnel simulation? Because we use water density and simulate air because air pressure is negligible when the height of the simulation is 1m? So why are we using water density then???
// - Figure out the wall situation during the different simulations. In the wind tunnel, figure out how wind is added. If we add wind next to a wall, then its never added because there's a wall to the left... A book also said that if we add inflow, we also have to add outflow, or it will be difficult to make the fluid incompressible. The source also say that viscosity takes cares of no outflow
// - The purpose of the sin function when we paint with obstacle
// - Why Integrate() is not ignoring the last column in x
public class FluidSimController : MonoBehaviour
{
    //Public
    public Material fluidMaterial;


    //Private
    private FluidScene scene;

    private FluidUI fluidUI;



    private void Start()
    {    
        scene = new FluidScene(fluidMaterial);

        fluidUI = new FluidUI(this);

        //The size of the plane we run the simulation on so we can convert from world space to simulation space
        scene.simPlaneWidth = 2f;
        scene.simPlaneHeight = 1f;

        //SetupScene(FluidScene.SceneNr.WindTunnel);

        SetupScene(FluidScene.SceneNr.Tank);


        //Test converting between spaces
        //Vector2 test = scene.WorldToSim(-1f, 7f);

        //test = scene.SimToWorld(test.x, test.y);

        //Debug.Log(test);
    }



    private void Update()
    {
        //Display the fluid
        //DisplayFluid.TestDraw(scene);

        DisplayFluid.Draw(scene);
    }



    private void LateUpdate()
    {
        //Interactions such as moving obstacles with mouse and pause
        fluidUI.Interaction(scene);
    }



    private void FixedUpdate()
    {
        Simulate();
    }



    private void OnGUI()
    {
        fluidUI.DisplayUI(scene);
    }



    //Simulate the fluid
    //Needs to be accessed from the UI so we can simulate step by step by pressing a key
    public void Simulate()
    {
        if (!scene.isPaused)
        {
            scene.fluid.Simulate(scene.dt, scene.gravity, scene.numIters, scene.overRelaxation);

            scene.frameNr++;
        }
    }



    //Init the simulation after a button has been pressed
    public void SetupScene(FluidScene.SceneNr sceneNr = FluidScene.SceneNr.Tank)
    {
        scene.sceneNr = sceneNr;
        scene.obstacleRadius = 0.15f;
        scene.overRelaxation = 1.9f;

        scene.SetTimeStep(1f / 60f);
        scene.numIters = 40;

        //How detailed the simulation is in height (y) direction
        //Default was 100 in the tutorial but it's slow as molasses in Unity
        int res = 50;

        if (sceneNr == FluidScene.SceneNr.Tank)
        {
            res = 50;
        }
        else if (sceneNr == FluidScene.SceneNr.HighResWindTunnel)
        {
            res = 200;
        }


        //The height of the simulation is 1 m (in the video)
        //But the guy is also setting simHeight = 1.1 and domainHeight = 1 so Im not sure the difference between them
        float simHeight = 1f;

        //The size of a cell
        float h = simHeight / res;

        //How many cells do we have
        //y is up
        int numY = res;
        //The texture we use here is twice as wide as high
        int numX = 2 * numY;

        //Density of the fluid (water)
        float density = 1000f;

        //Create a new fluid simulator
        FluidSim f = scene.fluid = new FluidSim(density, numX, numY, h);

        //Init the different simulations
        if (sceneNr == FluidScene.SceneNr.Tank)
        {
            SetupTank(f);
        }
        else if (sceneNr == FluidScene.SceneNr.WindTunnel || sceneNr == FluidScene.SceneNr.HighResWindTunnel)
        {
            SetupWindTunnel(f, sceneNr);
        }
        else if (sceneNr == FluidScene.SceneNr.Paint)
        {
            SetupPaint();
        }
    }



    private void SetupTank(FluidSim f)
    {
        //int n = f.numY;

        //Add a solid border
        for (int i = 0; i < f.numX; i++)
        {
            for (int j = 0; j < f.numY; j++)
            {
                //Fluid
                float s = 1f;

                //i == 0 (left wall)
                //j == 0 (bottom wall)
                //i == f.numX - 1 (right wall)
                //Why no top wall???
                if (i == 0 || i == f.numX - 1 || j == 0)
                {
                    //0 means solid
                    s = 0f;
                }

                f.s[f.To1D(i, j)] = s;
            }
        }

        scene.gravity = -9.81f;
        scene.showPressure = true;
        scene.showSmoke = false;
        scene.showStreamlines = false;
        scene.showVelocities = false;
    }



    private void SetupWindTunnel(FluidSim f, FluidScene.SceneNr sceneNr)
    {
        //int n = f.numY;

        //Wind velocity
        float inVel = 2f;

        for (int i = 0; i < f.numX; i++)
        {
            for (int j = 0; j < f.numY; j++)
            {
                //Fluid
                float s = 1f;

                //Left wall, bottom wall, top wall
                //Why no right wall, but left wall???
                //The smoke seems to disappear when it reaches the right border
                if (i == 0 || j == 0 || j == f.numY - 1)
                {
                    //0 means solid
                    s = 0f;
                }

                f.s[f.To1D(i, j)] = s;

                //Add right velocity to the fluid in the second column
                //Don't we need a velocity on the right border as well?
                //Wont disappear after first update because there's a wall to the left?
                if (i == 1)
                {
                    f.u[f.To1D(i, j)] = inVel;
                }
            }
        }

        //Add smoke
        float pipeH = 0.1f * f.numY;

        //In the middle of the simulation
        int minJ = Mathf.FloorToInt(0.5f * f.numY - 0.5f * pipeH);
        int maxJ = Mathf.FloorToInt(0.5f * f.numY + 0.5f * pipeH);

        for (int j = minJ; j < maxJ; j++)
        {
            //0 means max smoke in the first column (i = 0): f.m[0 * n + j] = f.m[j]
            //f.m[j] = 0f;

            int i = 0;

            f.m[f.To1D(i, j)] = 0f;
        }


        //Position the obstacle
        //The obstacle in the demo is only reset if we click on wind tunnel button
        //Otherwise it has the same position as last scene
        SetObstacle(0.4f, 0.5f, true);


        scene.gravity = 0f; //Why no gravity???
        scene.showPressure = false;
        scene.showSmoke = true;
        scene.showStreamlines = false;
        scene.showVelocities = false;

        if (sceneNr == FluidScene.SceneNr.HighResWindTunnel)
        {
            scene.SetTimeStep(1f / 120f);
            scene.numIters = 100;
            scene.showPressure = true;
        }
    }



    private void SetupPaint()
    {
        scene.gravity = 0f;
        scene.overRelaxation = 1f;
        scene.showPressure = false;
        scene.showSmoke = true;
        scene.showStreamlines = false;
        scene.showVelocities = false;
        scene.obstacleRadius = 0.1f;
    }



    //Position an obstacle in the fluid and make it interact with the fluid if it has a velocity
    //x,y are in simulation space - NOT world space
    public void SetObstacle(float x, float y, bool reset)
    {
        //To give the fluid a velocity by moving around the obstacle
        float vx = 0f;
        float vy = 0f;

        if (!reset)
        {
            vx = (x - scene.obstacleX) / scene.dt;
            vy = (y - scene.obstacleY) / scene.dt;
        }

        scene.obstacleX = x;
        scene.obstacleY = y;

        float r = scene.obstacleRadius;
        
        FluidSim f = scene.fluid;
        
        //int n = f.numY;
        
        //float cd = Mathf.Sqrt(2f) * f.h;

        //Ignore border
        for (int i = 1; i < f.numX - 2; i++)
        {
            for (int j = 1; j < f.numY - 2; j++)
            {
                //Start by setting all cells to fluids (= 1)
                f.s[f.To1D(i, j)] = 1f;

                //Distance from circle center to cell center
                float dx = (i + 0.5f) * f.h - x;
                float dy = (j + 0.5f) * f.h - y;

                //Is the cell within the obstacle?
                //Using the square is faster than actual Pythagoras Sqrt(dx * dx + dy * dy) < Sqrt(r^2) but gives the same result 
                if (dx * dx + dy * dy < r * r)
                {
                    //0 means obstacle 
                    f.s[f.To1D(i, j)] = 0f;

                    if (scene.sceneNr == FluidScene.SceneNr.Paint)
                    {
                        //Add/remove smoke because of the sinus this loops 0 -> 1 -> 0
                        f.m[f.To1D(i, j)] = 0.5f + 0.5f * Mathf.Sin(0.1f * scene.frameNr);
                    }
                    else
                    {
                        //1 means no smoke
                        f.m[f.To1D(i, j)] = 1f;
                    }

                    //Give the fluid a velocity if we have moved it
                    //These are the 4 velocities belonging to this cell
                    f.u[f.To1D(i, j)] = vx; //Left
                    f.u[f.To1D(i + 1, j)] = vx; //Right
                    f.v[f.To1D(i, j)] = vy; //Bottom
                    f.v[f.To1D(i, j + 1)] = vy; //Top
                }
            }
        }

        scene.showObstacle = true;
    }
}
