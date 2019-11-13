using Godot;
using System;

public class MainGui : Node{

    //get views
    private Label lblTerrain, lblTerrainDetail;
    private Panel pTaps, pIndiEdit, pGenEdit, pUnitEdit, pSaveData;
    private Label lblTerrainEditorSelected, lblUnitEditorSelected;//main terrain
    private TextEdit txtSeed,txtSizeMap,txtOctaves,txtPeriod,txtLacunarity,txtPersistence;//noise
    private HSlider slA1,slA2,slA3,slA4,slA5,slA6,slA7;//alturas
    private HSlider slR;//rios

    //edicion unidades
    private HSlider slPlayerId;
    private Label lblPlayerId;

    //seleccion unidad
    private Panel pUnitInfo;
    private Sprite[] imagesTerrain;    

    //info units selected
    private Label lblUnit,lblUnitDetail;
    private Node2D[] unitsViews;

    //save data / load
    private LineEdit txtNameData;
    private SubPanelLoadTerrainData plLoadData;
    private string actualLoadFile;

    //TURNS
    private int actualIdPlayer = -1;
    private bool isLocalPlayerNow = true;
    private Panel pTurn;
    private Label lblTurnNum;
    private Label lblActualPlayer;
    private Label lblTime;
    private ProgressBar pbTurnTime;

    //Animation change Turn
    private Label lblAnimTurnNum;
    private Label lblAnimPlayerName;
    private AnimationPlayer animTurn; 

    //unitsControl
    private Panel pUnitsControl;
    
    //player list
    private Panel pPlayers;
    private HBoxContainer playersListView;
    private PackedScene plvElementPrefab;

    //EnterGame
    private Panel pEnterGame;
    private Label lblEnterGameMap;
    private LineEdit leIP;

    //Lobby
    private Panel pLobby;
    private VBoxContainer vbcPlayerUnitsCounts;
    private VBoxContainer vbcPlayerTypeChange;
    private Label lblTerrainDataSize;
    private VBoxContainer vbcTerrainDatas;

    private int maxPlayerOnMap = 0;
    private Player[] players;

    
    //exit (fix multiples loads)
    private bool exiting = false;
    

    //ediciones signals
    [Signal] public delegate void editTerrainData(int typeEdition);
    [Signal] public delegate void playerIdEditonChange(int PlayerId);
    [Signal] public delegate void generateTerrain(float[] data);
    [Signal] public delegate void saveTerrain(string name);
    [Signal] public delegate void loadTerrain(string name);
    [Signal] public delegate void guiFocus(bool active);
    [Signal] public delegate void endTurn(int localPlayer);
    [Signal] public delegate void startGame(string fileName, int [,] playersDatas);

    //////////////////////////////////////////////////////////////////////////  METODOS

    public override void _Ready() {
        
        //get view references mini panels        
        lblTerrain = GetNode("MainGroup/pDataTerrain/hbc/vbc/hbc/lblTerrain") as Label;
        lblTerrainDetail = GetNode("MainGroup/pDataTerrain/hbc/vbc/lblTerrainDetail") as Label;

        pUnitInfo = GetNode("MainGroup/pDataUnit") as Panel;
        lblUnit = GetNode("MainGroup/pDataUnit/hbc/vbc/lblUnit") as Label;
        lblUnitDetail = GetNode("MainGroup/pDataUnit/hbc/vbc/lblUnitDetail") as Label;

        //get view tabs
        pTaps =  GetNode("MainGroup/pTabs") as Panel;
        pGenEdit =  GetNode("MainGroup/pGenEdit") as Panel;
        pIndiEdit =  GetNode("MainGroup/pIndiEdit") as Panel;
        pUnitEdit =  GetNode("MainGroup/pUnitEdit") as Panel;
        pSaveData =  GetNode("MainGroup/pSaveData") as Panel;
        plLoadData = GetNode("MainGroup/SubPanelLoadTerrainData") as SubPanelLoadTerrainData;
        
        //txtName file save data
        txtNameData = GetNode("MainGroup/pSaveData/panel/vbc/vbc/txtNameData") as LineEdit;

        //slider idPlayerUnit
        slPlayerId = GetNode("MainGroup/pUnitEdit/vbc/hbc/slPlayerId") as HSlider;
        lblPlayerId = GetNode("MainGroup/pUnitEdit/vbc/hbc/lblPlayerId") as Label;

        //get view tab indi edition
        lblTerrainEditorSelected = GetNode("MainGroup/pIndiEdit/vbc/lblActualSelected") as Label;
        lblUnitEditorSelected = GetNode("MainGroup/pUnitEdit/vbc/lblActualSelectedUnit") as Label;

        //get view tab generation
        txtSeed = GetNode("MainGroup/pGenEdit/vbc/vbc2/lineG1/txtSeed") as TextEdit;
        txtSizeMap = GetNode("MainGroup/pGenEdit/vbc/vbc2/lineG2/txtSizeMap") as TextEdit;

        txtOctaves = GetNode("MainGroup/pGenEdit/vbc/vbc3/lineN1/txtOctaves") as TextEdit;
        txtPeriod= GetNode("MainGroup/pGenEdit/vbc/vbc3/lineN2/txtPeriod") as TextEdit;
        txtLacunarity= GetNode("MainGroup/pGenEdit/vbc/vbc3/lineN3/txtLacunarity") as TextEdit;
        txtPersistence= GetNode("MainGroup/pGenEdit/vbc/vbc3/lineN4/txtPersistence") as TextEdit;
        
        slA1= GetNode("MainGroup/pGenEdit/vbc/vbc4/lineA1/slA1") as HSlider;
        slA2= GetNode("MainGroup/pGenEdit/vbc/vbc4/lineA2/slA2") as HSlider;
        slA3= GetNode("MainGroup/pGenEdit/vbc/vbc4/lineA3/slA3") as HSlider;
        slA4= GetNode("MainGroup/pGenEdit/vbc/vbc4/lineA4/slA4") as HSlider;
        slA5= GetNode("MainGroup/pGenEdit/vbc/vbc4/lineA5/slA5") as HSlider;
        slA6= GetNode("MainGroup/pGenEdit/vbc/vbc4/lineA6/slA6") as HSlider;
        slA7= GetNode("MainGroup/pGenEdit/vbc/vbc4/lineA7/slA7") as HSlider;

        slR= GetNode("MainGroup/pGenEdit/vbc/lineR/slR") as HSlider;

        //get terrain views
        Node parentNode = GetNode("MainGroup/pDataTerrain/hbc/vbc/hbc/imsTerrain");
        int count = parentNode.GetChildCount();
        imagesTerrain = new Sprite[count];

        for (int i = 0; i < count ;i++){
            imagesTerrain[i] = parentNode.GetChild<Sprite>(i);//get images array
        }

        //get units control views
        parentNode = GetNode("MainGroup/pDataUnit/Control");
        count = parentNode.GetChildCount();
        unitsViews = new Node2D[count];

        for (int i = 0; i < count ;i++){
            unitsViews[i] = parentNode.GetChild<Node2D>(i);//get images array
        }

        //get panel TURN
        pTurn = GetNode("MainGroup/pTurn") as Panel;
        lblTurnNum = GetNode("MainGroup/pTurn/hbc/vbc/lblTurnNum") as Label;
        lblActualPlayer = GetNode("MainGroup/pTurn/hbc/vbc/lblActualPlayer") as Label;
        lblTime = GetNode("MainGroup/pTurn/hbc/vbc/lblTime") as Label;
        pbTurnTime = GetNode("MainGroup/pTurn/hbc/vbc/pbTurnTime") as ProgressBar;

        //Animation change turn 
        animTurn = GetNode("MainGroup/pChangeTurn/animTurn") as AnimationPlayer; 
        lblAnimTurnNum = GetNode("MainGroup/pChangeTurn/lblTurnNum") as Label; 
        lblAnimPlayerName = GetNode("MainGroup/pChangeTurn/lblTurnPlayerName") as Label; 

        //player list
        pPlayers =  GetNode("MainGroup/pPlayers") as Panel;
        playersListView = GetNode("MainGroup/pPlayers/playerListView") as HBoxContainer;
        plvElementPrefab = GD.Load<PackedScene>("res://Scenes/MainGui/PlayerListViewElement/PlayerListViewElement.tscn");

        //panel units control
        pUnitsControl = GetNode("MainGroup/pUnitsControl") as Panel;

        //START GAME
        pEnterGame = GetNode("MainGroup/pEnterGame") as Panel; 
        lblEnterGameMap = GetNode("MainGroup/pEnterGame/lblCreate") as Label; 
        leIP = GetNode("MainGroup/pEnterGame/leIP") as LineEdit; 

        //LOBBY
        pLobby = GetNode("MainGroup/pLobby") as Panel; 
        lblTerrainDataSize = GetNode("MainGroup/pLobby/crT/lblSize") as Label; 

        vbcPlayerUnitsCounts = GetNode("MainGroup/pLobby/crP/vbc") as VBoxContainer;
        vbcPlayerTypeChange = GetNode("MainGroup/pLobby/crP/vbc2") as VBoxContainer;
        vbcTerrainDatas = GetNode("MainGroup/pLobby/crT/vbc") as VBoxContainer;

        //INIT
        pUnitInfo.SetVisible(false);

        //OK
        GD.Print("GUI ready");
    }

    public override void _Process(float delta) {

        if (Input.IsActionPressed("ui_cancel")){
            onClickExit();
        }
        
    }

    //CALL BACK SIGNALS
    public void updateviewTimes(float gameTime, float turnTime){
        //invoked by Game
        lblTime.SetText( String.Format("TIME: {0}", Mathf.Floor(gameTime).ToString()));
        pbTurnTime.Value = turnTime;

    }

        //new game
    public void createPlayerListDatas(string[] datas){
        
        foreach(string dataPlayer in datas){
            PlayerListViewElement plve = plvElementPrefab.Instance() as PlayerListViewElement;
            playersListView.AddChild(plve);
            plve.setData(dataPlayer);
        }
        
    }
        //game.playersdata callback
    public void updatePlayerData(int idPlayer,string data){
        
        PlayerListViewElement plve = playersListView.GetChild(idPlayer) as PlayerListViewElement;
        plve.setData(data);
    }

    public void updatePositionView(Vector2 pos, int[] data){
        lblTerrain.SetText(String.Format("{0},{1}",pos.x,pos.y));
        lblTerrainDetail.SetText(String.Format("Terrain {0}\nDetail {1} \nH:{2}, Ro {3},Ri {4} ",
            data[0], data[1],  data[2], data[3], data[4]
            ));
        

        for (int i = 0; i<imagesTerrain.Length;i++){
            imagesTerrain[i].SetVisible(data[0] == i);
        }
    }

    public void updateSelectedOnMap(Vector2 pos, string[] datas){
        if (datas == null){
            pUnitInfo.SetVisible(false);
            return;
        }
        pUnitInfo.SetVisible (datas[0] != "None"); 

        lblUnit.SetText("ID: " + datas[0]);  //nombre,position,player,movimientos
        string detailText = String.Format(
            "Pos: {0},{1} \nPlayer: {2} \nMovePoints: {3}",
            pos.x,pos.y,datas[1],datas[2]);

        lblUnitDetail.SetText(detailText);

        //unit image
        foreach (Node2D n in unitsViews) n.SetVisible(false);
        switch(datas[0]){
            case "0": unitsViews[0].SetVisible(true); break;
            case "1": unitsViews[1].SetVisible(true); break;
            case "2": unitsViews[2].SetVisible(true); break;
        }

    }

    public void updateActualTurn(int idPlayerActual, bool isLocal, int turnNum){
        this.actualIdPlayer = idPlayerActual;
        string strTurnNum = "TURN: " + turnNum;
        string strPlayerTurn = "Actual Player: " + idPlayerActual;

        lblTurnNum.SetText(strTurnNum);
        lblActualPlayer.SetText(strPlayerTurn);

        if (isLocal){
            isLocalPlayerNow = true;
            //active panelUnitsControl
            //anim string: you turn!
            
        }else{
            isLocalPlayerNow = false;
        }

        //update mark on player listview
        for (int i = 0; i<playersListView.GetChildCount();i++){
            PlayerListViewElement plve = playersListView.GetChild(i) as PlayerListViewElement;
            plve.isActual = (idPlayerActual == i);
        }
       
        //animation 
        lblAnimTurnNum.SetText(strTurnNum); 
        lblAnimPlayerName.SetText(strPlayerTurn); 
        animTurn.Play("show"); 

    }

    public void updateMapData(int sizeMap, int[] terrain, int[] playerUnits ){
        GD.Print("GUI: updata map data");
        
        maxPlayerOnMap = 0;
        players = new Player[8];

        //panel LOBBY
        for(int i = 0; i < 8 ; i++ ){
            Label lblChild = vbcPlayerUnitsCounts.GetChild(i) as Label;
            Button buP = vbcPlayerTypeChange.GetChild(i) as Button;
            
            if (playerUnits[i]>0){
                //data
                players[i] = new Player(i);
                players[i].playerType = (i==0)? Player.TYPEPLAYER.LOCAL : Player.TYPEPLAYER.IA;//primero player local el resto IA
                players[i].playerteam = i;//free for all
                players[i].unitsCount =  playerUnits[i];

                //view
                lblChild.SetText(String.Format("P{0}.{1} Units:{2}", i, players[i].playerType, players[i].unitsCount));
                lblChild.SetVisible(true);
                buP.SetVisible(true);

                //connect onclick
                if(!buP.IsConnected("pressed",this,"onClickChangeTypePlayer")){
                    Godot.Collections.Array gdArray = new Godot.Collections.Array(); gdArray.Add(i);
                    buP.Connect("pressed",this,"onClickChangeTypePlayer",gdArray);
                }
                
                //multi
                maxPlayerOnMap++;

            }else{
                lblChild.SetVisible(false);
                buP.SetVisible(false);
            }
        }

        //datas terrain show view
        lblTerrainDataSize.SetText("Map: "+sizeMap+" x "+sizeMap); 

        vbcTerrainDatas.GetChild<Label>(0).SetText("Deep Water: " + terrain[0]);
        vbcTerrainDatas.GetChild<Label>(1).SetText("Water: " + terrain[1]);
        vbcTerrainDatas.GetChild<Label>(2).SetText("Ground: " + terrain[2]);
        vbcTerrainDatas.GetChild<Label>(3).SetText("Glass: " + terrain[3]);
        vbcTerrainDatas.GetChild<Label>(4).SetText("Forest: " + terrain[4]);
        vbcTerrainDatas.GetChild<Label>(5).SetText("Hill: " + terrain[5]);
        vbcTerrainDatas.GetChild<Label>(6).SetText("Mountain: " + terrain[6]);
        vbcTerrainDatas.GetChild<Label>(7).SetText("Top: " + terrain[7]);

        vbcTerrainDatas.GetChild<Label>(8).SetText("River: " + terrain[8]);
        vbcTerrainDatas.GetChild<Label>(9).SetText("Road: " + terrain[9]);
        vbcTerrainDatas.GetChild<Label>(10).SetText("Building: " + terrain[10]);


    }

    //clicks buttons edition
    public void onClickTab(int index){
       GetTree().SetInputAsHandled();//no repite el click

        switch(index){
            case 0: //SUBPANEL TERRAIN EDITION
                pIndiEdit.SetVisible(!pIndiEdit.IsVisible());
                pGenEdit.SetVisible(false);
                pUnitEdit.SetVisible(false);
                pSaveData.SetVisible(false);
                plLoadData.SetVisible(false);
                break;
            case 1: //SUBPANEL GENERACION
                pIndiEdit.SetVisible(false);
                pGenEdit.SetVisible(!pGenEdit.IsVisible());
                pUnitEdit.SetVisible(false);
                pSaveData.SetVisible(false);
                plLoadData.SetVisible(false);
                break;
            case 2: //SUBPANEL UNIDADES
                pIndiEdit.SetVisible(false);
                pGenEdit.SetVisible(false);
                pUnitEdit.SetVisible(!pUnitEdit.IsVisible());
                pSaveData.SetVisible(false);
                plLoadData.SetVisible(false);
                break;
            case 3: //SAVE
                pIndiEdit.SetVisible(false);
                pGenEdit.SetVisible(false);
                pUnitEdit.SetVisible(false);
                pSaveData.SetVisible(!pSaveData.IsVisible());
                plLoadData.SetVisible(false);
                EmitSignal("guiFocus",true); //avisa que la gui tiene el foco del input
                break;
            case 4: //LOAD
                pIndiEdit.SetVisible(false);
                pGenEdit.SetVisible(false);
                pUnitEdit.SetVisible(false);
                pSaveData.SetVisible(false);
                plLoadData.SetVisible(!plLoadData.IsVisible());
                plLoadData.poblateList();
                EmitSignal("guiFocus",true); //avisa que la gui tiene el foco del input
                break;
        }

        EmitSignal("editTerrainData",-1); //no edicion en editor
    }

    public void onClickTerrainEdition(int index){
        GetTree().SetInputAsHandled();//no repite el click en otros inputs

        EmitSignal("editTerrainData",index);
        lblTerrainEditorSelected.SetText(String.Format("Actual selection: {0}",index));
    }

    public void onClickGenerateTerrain(){
        GetTree().SetInputAsHandled();//no repite el click en otros inputs

        bool isOk = true;
        float[] data = new float[14];
        
        //main
        isOk &= float.TryParse(txtSeed.GetText(),out data[0]); 
        isOk &= float.TryParse(txtSizeMap.GetText(),out data[1]); 
        
        //noise values
        isOk &= float.TryParse(txtOctaves.GetText(),out data[2]); 
        isOk &= float.TryParse(txtPeriod.GetText(),out data[3]); 
        isOk &= float.TryParse(txtLacunarity.GetText(),out data[4]); 
        isOk &= float.TryParse(txtPersistence.GetText(),out data[5]); 

        //minHeights
        data[6] = slA1.GetValue();
        data[7] = slA2.GetValue();
        data[8] = slA3.GetValue();
        data[9] = slA4.GetValue();
        data[10]= slA5.GetValue();
        data[11]= slA6.GetValue();
        data[12]= slA7.GetValue();

        //rivers
        data[13]= slR.GetValue();

        if (isOk){
            EmitSignal("generateTerrain",data);
        }else{
            GD.Print("GENERAR PARSE ERROR");
        }

    }

    public void onClickAddUnit(int index){
        GetTree().SetInputAsHandled();//no repite el click en otros inputs
        EmitSignal("editTerrainData",index);
        lblUnitEditorSelected.SetText(String.Format("Actual selection: {0}",index));
    }
    
     //units player owner
    public void onEditIdPlayer(float value){
        int id = (int) value;
        lblPlayerId.SetText("Player id: " + id);
        EmitSignal("playerIdEditonChange",id);
    }

    //load save
    public void onClickSaveButton(bool save){
        if (save){

            if (txtNameData.Text.Length>0){
                EmitSignal("saveTerrain",txtNameData.GetText()); 

            }else{
                 GD.Print("NO VALID NAME");
                 return;//nothing
            }
        }

        pSaveData.Visible = false;
        EmitSignal("guiFocus",false); //sin foco
    }

    public void onClickLoadButton(string nameFile){
        if (nameFile != ""){
            actualLoadFile = nameFile;
            lblEnterGameMap.SetText("Create game, map: " + actualLoadFile);

        }else{
            GD.Print("NO LOAD");
            return;
        }


        //world map recupera foco?
        if(!pEnterGame.IsVisible() && !pLobby.IsVisible()){

            if(nameFile!=""){
                EmitSignal("loadTerrain",nameFile);
            }

            EmitSignal("guiFocus",false); 
        }
        
    }

    //start game and Lobby
    public void onClickCreateGame(){

        if (!actualLoadFile.Empty()){
            pEnterGame.SetVisible(false);
            pLobby.SetVisible(true);
            //create worldmap and wait for call back for collect data.
            EmitSignal("loadTerrain",actualLoadFile);
        }
    }

    public void onClickCreateGameSelectMap(){
        plLoadData.SetVisible(!plLoadData.IsVisible());
    }

    public void onClickJoin(){
        //not yet
        GD.Print("Not yet");
    }

    public void onClickChangeTypePlayer(int idPlayer){
        if (players == null) return;
        GD.Print("Onclick Player " + idPlayer);

        //change data
        players[idPlayer].playerType++;
        if((int)players[idPlayer].playerType > 2) players[idPlayer].playerType = 0;

        //view update
        vbcPlayerUnitsCounts.GetChild<Label>(idPlayer)
        .SetText(String.Format("P{0}.{1} Units:{2}", idPlayer, players[idPlayer].playerType, players[idPlayer].unitsCount));

    }

    public void onClickReady(){
        bool configOk = false;

        //1 player local:
        foreach(Player p in players){
            if(p == null)continue;
            if (p.playerType == Player.TYPEPLAYER.LOCAL) configOk = true;
        }

        if (!configOk) return;

        //other validations online here

        //emit data to GAME
        int activesPlayer = 0;
        int [,] playersDatas = new int[8,5];

        for(int i = 0; i<8;i++){
            if(players[i]== null)continue;
            playersDatas[i,0] = (int)players[i].playerType;
            playersDatas[i,1] = players[i].playerteam;
            playersDatas[i,2] = players[i].unitsCount;
            playersDatas[i,3] = players[i].resources;
            playersDatas[i,4] = players[i].incoming;
            
            if (players[i].unitsCount>0)activesPlayer++;
        }

        //change to gui GAME
        pLobby.SetVisible(false);
        setGameStyle(1);

        //send player actives to gui player barr
        string[]datasPlayers = new string[activesPlayer];
        int count = 0;

        for(int i = 0; i< 8;i++) {
            
            if(players[i]== null)continue;

            Player p = players[i];
            if(p.unitsCount<1) continue;
            
            datasPlayers[count] = String.Format("{0};{1};{2};{3};{4}",
                p.id, (int)p.playerType, p.resources, p.incoming ,p.unitsCount);//id, type, resource, incoming, numUnits

            count++;
        }

        createPlayerListDatas(datasPlayers);
        //avisa a game
        EmitSignal("startGame",actualLoadFile,playersDatas);
        //suelta foco
        EmitSignal("guiFocus",false);

    }

    //GAME End turn
    public void onClickNextTurn(){
        if(!isLocalPlayerNow)return;
        EmitSignal("endTurn",actualIdPlayer); //avisa que el player local finalizo el turno a game
    }

    //EXIT
    public void onClickExit(){
        if (exiting) return;
        exiting = true;
        ScenesLoader scenesLoader = GD.Load<PackedScene>("res://Scenes/SceneLoader/ScenesLoader.tscn").Instance() as ScenesLoader;
        AddChild(scenesLoader);
        scenesLoader.loadScene("res://Scenes/MenuMain/MainMenu.tscn");//exit a main menu
    }

   

    //GAME STYLE
    public void setGameStyle(int style){
        //editor, game or lobby?
        switch(style){
            case 0://editor
                pTaps.SetVisible(true);
                pTurn.SetVisible(false);
                pPlayers.SetVisible(false);
                pUnitsControl.SetVisible(false);
                pEnterGame.SetVisible(false);
            break;

            case 1://game
                pTaps.SetVisible(false);
                pTurn.SetVisible(true);
                pPlayers.SetVisible(true);
                pUnitsControl.SetVisible(true);
                pEnterGame.SetVisible(false);
            break;

            case 2: //enterGame
                pTaps.SetVisible(false);
                pTurn.SetVisible(false);
                pPlayers.SetVisible(false);
                pUnitsControl.SetVisible(false);
                pEnterGame.SetVisible(true);
                EmitSignal("guiFocus",true); //avisa que la gui tiene el foco del input
            break;

        }

    }

}
