using Godot;
using System;
using System.Collections.Generic;

public class IA : Node2D{
    private System.Threading.Thread thread;

    private int actualPlayer = -1; //block execution.

    //my units
    private List<Unit> myUnits;
    private int unitIndex = 0;

    //timer 
    private float time = 0; 
    private bool isTimerActive = false; 
    private float execAwaitMoment = 2f; 
    private float waitTime = 1f;

    //ref data 
    private WorldMap worldMap; 
    
    //emmits 
    [Signal] public delegate void endTurn(int myPlayer); //Orden completada ejecutados 


    //███████████████████████████████ METODOS 
    public override void _Ready() { 
        GD.Print("BASIC IA: atack closer"); 
        if (thread!=null &&thread.IsAlive)thread.Abort();
        isTimerActive = false;
    }

    public override void _Process(float delta){
        time+=delta;

        //my timer
        if(isTimerActive){
            if (time-execAwaitMoment>waitTime){
                isTimerActive = false;
                thread = new System.Threading.Thread(() => this.exec()); thread.Start();
            }
        }
    }

    public void execIA(int myPlayer, WorldMap worldMap){
        this.worldMap = worldMap;
        actualPlayer = myPlayer;

         //START: get my Units
        DataTerrain dt = worldMap.dataTerrain;
        myUnits = new List<Unit>();
        unitIndex = 0;

        for (int i = 0; i<dt.sizeMap; i++){
            for (int j = 0; j<dt.sizeMap; j++){
                Unit u = dt.units[j,i];
                if(u!=null){
                    if(u.owerPlayer == actualPlayer){
                        myUnits.Add(u);
                    } 
                }
            }
        }

        GD.Print("-BASIC IA "+actualPlayer+" READY, UNITS: "+myUnits.Count);

        awaitExec(actualPlayer);
    }

    //call back when worldmap.orderFinished
    public void awaitExec(int playerId){
        if(actualPlayer!= playerId) return;//actual threath control
        time = 0;
        execAwaitMoment = time;
        isTimerActive = true;
    }

    //exec in a thread
    public void exec(){
        
        //end control
        if(unitIndex>=myUnits.Count){
            GD.Print("BASIC IA: NO MORE UNITS. END TURN");
            EmitSignal("endTurn",actualPlayer);
            return;
        }

        //GO
        Unit myUnit = myUnits[unitIndex];
        DataTerrain dt = worldMap.dataTerrain;

        //get enemies
        List<Unit> othersUnits = new List<Unit>();
        for (int i = 0; i<dt.sizeMap; i++){
            for (int j = 0; j<dt.sizeMap; j++){
                Unit u = dt.units[j,i];
                if(u!=null){
                    if(u.owerPlayer != actualPlayer){
                        othersUnits.Add(u);
                    }
                }
            }
        }

        GD.Print("UNIT. ENEMIES: " + othersUnits.Count);

        //no Enemies
        if( othersUnits.Count<1){
            GD.Print("BASIC IA: NO ENEMIES. END TURN");
            EmitSignal("endTurn",actualPlayer);
            return;
        }

        //update unit subNavigation system
        worldMap.updateUnit(myUnit.wpX,myUnit.wpY);
        
        //1)get closed enemy, if can atack-> atack, else move close position.
        Unit closedU = getCloseUnit(myUnit.wpX,myUnit.wpY, othersUnits);
        float dist2 = getDist2(closedU.wpX,closedU.wpY, myUnit.wpX,myUnit.wpY);
        
        if (closedU == null){
            exec();//unit not valid. try again
            return;
        }

        //2) atack or approach
        GD.Print("UNIT. closed enemy at: " + closedU.wpX + " " + closedU.wpY + " dist2: " + dist2);

        if (myUnit.isValidAttack(myUnit.wpX,myUnit.wpY,closedU.wpX,closedU.wpY)){
            GD.Print("UNIT: atacking closed unit!");
            worldMap.execOrder(myUnit.wpX,myUnit.wpY,closedU.wpX,closedU.wpY,1);//move order
            return;//wait animation

        }else{ 
            //expensive call
            Vector2 closed = myUnit.atackMove(closedU.wpX,closedU.wpY);

            if (closed!=new Vector2(-100,-100)){
                if( myUnit.isValidMove((int)closed.x, (int)closed.y)){
                    GD.Print("UNIT: moving to closed unit to atack!");
                    worldMap.execOrder(myUnit.wpX,myUnit.wpY,(int)closed.x,(int)closed.y,0);//move order
                    return;//wait animation
                }
            }
        }

        GD.Print("UNIT.END, next unit.");
        unitIndex++;
        exec();
    }

    //UTILS
    private Unit getCloseUnit(int x, int y, List<Unit>units){
        Unit closedUnit = null;
        float mindist2 = float.MaxValue;

        foreach(Unit u in units){
            float dis2 = getDist2(u.wpX,u.wpY,x,y);
            if(dis2<mindist2){
                mindist2 = dis2;
                closedUnit = u;
            }
        }
        return closedUnit;
    }

    private float getDist2 (int intX, int initY, int finalX, int finalY ){
        int dirX = finalX - intX;
        int dirY = finalY - initY;
        return (dirX*dirX) + (dirY*dirY);
    }


}
