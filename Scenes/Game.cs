using Godot;
using System;
using System.Collections.Generic;

public class Game : Node2D{

    //players and turns
    public Player[] players; 
    public int maxPlayers = 2; 
    public int turn = -1; //-1 -> turn countdown off. 
    public int actualPlayer; 

    //REF
    private WorldMap worldmap; 
    private MainCamera camera; 
    private MainGui gui; 
    private IA ia; 

    //TIMES
    private float gameTime; 
    private float turnTime; 
 
    private float maxTurnTime = 22f; //20 + 2 animation transition offset time 

    //SIGNALS
    [Signal] public delegate void nextPlayer(int playerIdActual, bool isLocal, int turnNum); //to GUI and WorldMap
    [Signal] public delegate void sendTimes(float gameTime, float turnTime); //to GUI and WorldMap
    [Signal] public delegate void sendPlayerData(int idPlayer,string data); // to GUI data player and victory conditions


    // █████████████████████████████████████████ METODOS ████████████████████████████████
    public override void _Ready(){
        //get
        worldmap = GetNode("WorldMap") as WorldMap;
        camera = GetNode("MainCamera") as MainCamera;
        gui = GetNode("MainGui") as MainGui;
        ia = GetNode("IA") as IA;
        
        //gui init
        gui.setGameStyle(2);//menu de creacion/union
        
    }

    public override void _Process(float delta){
        gameTime+=delta;
        turnTime-=delta;

        //time end turn control:
        if (turnTime<=0 && turn>-1){
            turnTime = 0;
            endTurn(actualPlayer);
        }

        EmitSignal("sendTimes",gameTime,turnTime);//update gui values
    }

    public void startGame(string filename, int [] playersDatas){
        
        //get Data: i*5 datas: dont work signal multidimensional array
        List<Player> lPlayer = new List<Player>();

        for(int i = 0; i<8;i++){
            int row = i*5;
            Player p = new Player(i);
            p.playerType = (Player.TYPEPLAYER) playersDatas[row+0];
            p.playerteam = playersDatas[row+1];
            p.unitsCount = playersDatas[row+2];
            p.resources = playersDatas[row+3];
            p.incoming = playersDatas[row+4];

            if(p.unitsCount>0){
                lPlayer.Add(p);
            } 
        }

        //set data
        players = lPlayer.ToArray();
        maxPlayers = players.Length;

        //turno actual
        actualPlayer = -1;

        //config terrain
        worldmap.isGame = true;

        //terrain: create o load
        worldmap.loadData(filename);

        GD.Print("GAME INIT");

        gameTime = 0;

        endTurn(actualPlayer);//init game
    }

    public void endTurn(int orderPlayer){

        if (orderPlayer!=actualPlayer)return;//no late orders

        turn++;
        actualPlayer++;
        if (actualPlayer>=maxPlayers)actualPlayer = 0;

        //victory?
        int codVic = isVictory();
        if(codVic >-1 ){
            GD.Print("!!!!!!!!!!!!!!!!!!!!!END GAME !!!!!!!!!!!!!!!!!!\n Annihilation. Player "+codVic+" win.Turns: "+turn+".");
            GD.Print("PRESS ESC");
            turn=-1;//stop timer
            //END GAME! VICTORY
            return;
        }

        //player active?
        if (players[actualPlayer].unitsCount ==0 ){
            endTurn(actualPlayer);
            return;
        }

        //playerturn , is local, numTurn
        EmitSignal("nextPlayer",actualPlayer,(players[actualPlayer].playerType == Player.TYPEPLAYER.LOCAL),turn); //signal next turn datas for gui and worldmap

        //change for observer pattern?
        if(players[actualPlayer].playerType == Player.TYPEPLAYER.IA){
            ia.execIA(actualPlayer,worldmap);
        }

        //turn time reset
        turnTime = maxTurnTime;

    }

    //callback from GAME when a unit dies
    public void unitKill(int playerId){
        players[playerId].unitsCount--;
        updateGuiPlayers(playerId);
    }

    private void updateGuiPlayers(int idPlayer){
 
        //send player actives to gui player barr
        Player p = players[idPlayer];
        string datasPlayer = String.Format("{0};{1};{2};{3};{4}",
                p.id, (int)p.playerType, p.resources, p.incoming ,p.unitsCount);//id, type, resource, incoming, numUnits

        //update gui
        EmitSignal("sendPlayerData",idPlayer,datasPlayer);
    }

    private int isVictory(){
        int codeWin = -1;

        //annihilation win
        int idPlayerHasUnits = -1;
        int hasUnitsCount = players.Length;
        for (int i = 0;i<players.Length;i++){
            if (players[i].unitsCount<1){
                hasUnitsCount--;
            }else{
                idPlayerHasUnits = i;
            } 
        }

        //solo uno 
        if (hasUnitsCount<2){
            return idPlayerHasUnits;
        }

        return codeWin;
    }

}
    //████████████████████████████████ PLAYER ███████████████████████████████████████

    public class Player{
    public readonly int id;

    public int playerteam = 0;

    public int unitsCount = 0;

    public int resources = 0;

    public int incoming = 0;

    public Player (int id){
        this.id = id;
    }

    public enum TYPEPLAYER{LOCAL,IA,ONLINE}
    public TYPEPLAYER playerType = TYPEPLAYER.LOCAL;

}

