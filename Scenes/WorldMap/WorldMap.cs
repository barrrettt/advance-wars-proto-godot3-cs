using Godot;
using System;
using System.Collections.Generic;

public class WorldMap : Node2D{

    //SIGNALS
    [Signal] public delegate void MapPoint(Vector2 point, int[] data); //gui mouse pos info terrain
    [Signal] public delegate void SelectedPoint(Vector2 point, string[] datas); //gui info selected
    [Signal] public delegate void mapGenerated(Vector2 sizeMap, int[] terrain, int[] playerUnits ); //mapa generado, avisa a camara. y a GUI
    [Signal] public delegate void order(int playerId, int unitX, int unitY, int destX, int destY, int orderType); //mapa generado, avisa a camara.
    [Signal] public delegate void orderFinish(int playerId); //order and animation is done. for IA and GUI.
    [Signal] public delegate void unitKill(int playerId); //unit kill
    
    [Signal] public delegate void sendOrder(PlayerOrder order);

    //referencias
    private TileMap tmTerrain;  private TileMap tmDetails; private TileMap tmBigDetails; private TileMap tmBuildings;
    private TileMap[] tileMaps; //array para pasar a los metodos mas comodo
    private Node2D nUnits; 

    //control de seleccion 
    public enum CONTROLTYPE{NONE,MOUSE,PAD}
    [Export] public CONTROLTYPE tipoControl = CONTROLTYPE.MOUSE;

    //selection
    private Vector2 actualPoint = Vector2.Zero;
    private Vector2 actualSelectionPoint;
    private Unit selectedUnit = null; 

    //selector
    private Node2D selectionViewer;

    //prefabs 
    private PackedScene[] prefabsUnits; 

    //EDITANDO 
    private bool isEdition = false; 
    private int editionType = -1;//tipo de pintura /unidad
    private int idPlayerPropietario = 0;//para unidades, propietario
    private Vector2 lastEditionPos;//evita repeticiones 

    //DATA Terrain and units 
    public DataTerrain dataTerrain; 

    //threats 
    System.Threading.Thread thrCellViewControler;//cells cercanas solo
    public bool path_Ok = true; 

    //view control
    private Vector2 cameraPosition = Vector2.Zero; 
    private int maxLengthTileMapView = 40; 
    private int minLengthUpdateView = 42; 
    //gameTime:
    public float time;

    //gameControls
    public bool isGame = false;
    public bool waitingForCreateOrder = false;
    public int playerActual = 0;
    public bool isLocalPlayer = true;
    public bool waitingUnitAction = false;

    //███████████████████████████████████████████████████████   FUNCTIONS

    public override void _Ready() {
        tmTerrain = GetNode("Terrain/TileMapTerrain") as TileMap;
        tmDetails = GetNode("Terrain/TileMapDetails") as TileMap;
        tmBuildings = GetNode("Terrain/TileMapBuildings") as TileMap;
        tmBigDetails = GetNode("Terrain/TileMapBigDetails") as TileMap;

        tileMaps = new TileMap[]{tmTerrain,tmDetails,tmBuildings,tmBigDetails};

        nUnits = GetNode("Terrain/Units") as Node2D;

        selectionViewer = GetNode("Terrain/Seleccion") as Node2D;

        //delete old units view
        for (int i = 0; i < nUnits.GetChildCount() ;i++){
            nUnits.GetChild<Node2D>(i).QueueFree();
        }

        //CREATE RANDOM INIT TERRAIN
        float[] initDataTerrain = new float[]{
            0f,100f,
            8f,6f,1.5f,0.02f,
            2,10,15,20,25,30,35,
            1};

        dataTerrain = new DataTerrain("INIT",initDataTerrain);    

        //get packetscenes with units
        prefabsUnits = new PackedScene[3];
        prefabsUnits[0] =  GD.Load<PackedScene>("res://Scenes//Units//Soldier//UnitSoldier.tscn");
        prefabsUnits[1] =  GD.Load<PackedScene>("res://Scenes//Units//SoldierB//UnitSoldierB.tscn");
        prefabsUnits[2] =  GD.Load<PackedScene>("res://Scenes//Units//AlienA//UnitAlien.tscn");

        //indicators init
        actualPoint = Vector2.One*-1;
        selectionViewer.Visible=(false);
        dataTerrain.navigation.createPath(0,0,0,0,0);//borra linea en rios

        //THREATS
        if (thrCellViewControler!=null &&thrCellViewControler.IsAlive)thrCellViewControler.Abort();
        thrCellViewControler = new System.Threading.Thread(() => this.taskAsincViewControl());  thrCellViewControler.Start();//view subzone
    }
    
    public override void _Process(float delta){
        time+= delta;
        /* 
        //paint paths
        if (cuadroSeleccion.IsVisible()){
            if (unidadSelecionada != null){

                Vector2 mworld = GetGlobalMousePosition();
                Vector2 mouseCellPos = tmTerrain.WorldToMap(mworld);

                //dataTerrain.navigation.blockUnitsExcept(punteroActual);
                createPath(
                    (int) punteroActual.x, (int) punteroActual.y,//s
                    (int)mouseCellPos.x, (int)mouseCellPos.y, //f
                     unidadSelecionada.navAgentType);
            }
        }
        */
    }
    
    //CONTROL
    public override void _UnhandledInput(InputEvent @event){
        if (tipoControl == CONTROLTYPE.NONE) return;

        Vector2 mworld = GetGlobalMousePosition();
        Vector2 mouseCellPos = tmTerrain.WorldToMap(mworld);
        
        if(!isGame){
            editionControls(@event,mouseCellPos);
        }else{
            gameControls(@event,mouseCellPos);
        }
        
        //mouse pos terrain data to gui:
        Cuadro cuadro = dataTerrain.getCuadro(mouseCellPos);
        if (cuadro != null){
            int[] data = new int[]{
                (int)cuadro.tipo,
                cuadro.cosmeticDetail,
                cuadro.height,
                cuadro.road?1:0,
                cuadro.river?1:0
                };

            EmitSignal("MapPoint",mouseCellPos,data);//send
        }        
    }
    
    private void editionControls(InputEvent @event,Vector2 mouseCellPos){
        
        //click event
        if (@event is InputEventMouseButton){
            if (Input.IsMouseButtonPressed(1)){
                if (isEdition){
                    if(lastEditionPos != mouseCellPos){
                        changeDataTerrain(mouseCellPos,editionType);
                    }
                    lastEditionPos = mouseCellPos;//evita repeciciones

                }else{
                    selectPosition(mouseCellPos);//coloca el puntero en el terreno
                }
            }
            //derecho click
            if (Input.IsMouseButtonPressed(2)){
                deseleccionar();
            }
        }

        //move and pressed when editing terrain
        if (@event is InputEventMouseMotion){
            if (Input.IsMouseButtonPressed(1)){
                if (isEdition){
                    if (lastEditionPos != mouseCellPos){
                        changeDataTerrain(mouseCellPos,editionType);
                    }
                    lastEditionPos = mouseCellPos;
                }
            }
        }
    }

    private void gameControls (InputEvent @event,Vector2 mouseCellPos){
        //click event
        if (@event is InputEventMouseButton){
            
            if (waitingUnitAction)return;

            if (Input.IsMouseButtonPressed(1)){
                
                if (!waitingForCreateOrder){
                    selectPosition(mouseCellPos);
                    if (selectedUnit != null){
                        if (selectedUnit.owerPlayer == playerActual && isLocalPlayer){
                            waitingForCreateOrder = true;
                        }else{
                            waitingForCreateOrder = false;
                        }
                    }

                }else{
                    waitingForCreateOrder = false;
                    deseleccionar();

                    //CREATE ORDER
                    Vector2 unitCellPos = tmTerrain.WorldToMap(selectedUnit.Position);
                    int unitX = (int)unitCellPos.x;  int unitY = (int)unitCellPos.y; 
                    int destX = (int)mouseCellPos.x; int destY = (int)mouseCellPos.y;

                    //move or atack??= enemy on destination position?
                    bool isMove = true;
                    if (dataTerrain.units[destX,destY] != null){
                        Unit u = dataTerrain.units[destX,destY];
                        if (u.owerPlayer != playerActual){
                            isMove = false;//ataca
                        }
                    }

                    //valide localy first
                    if (tryOrder(unitX, unitY, destX, destY, isMove?0:1)){
                        //newt emit order intent to game controler.
                        EmitSignal("order", playerActual, unitX, unitY, destX, destY, isMove?0:1);
                        EmitSignal("sendOrder", new PlayerOrder(playerActual, unitX, unitY, destX, destY, isMove?0:1));

                        //debug
                        string strOrder = string.Format("Order: player {0}. unitX {1}. unitY {2}. DestinoX {3}. DestinoX {4}. OrderType {5}.",
                        playerActual, unitX, unitY, destX, destY, isMove?0:1);
                        GD.Print(strOrder);

                        execOrder(unitX, unitY, destX, destY, isMove?0:1);//comment me. This function must be called from the game controler

                    }else{

                        GD.Print("ORDER LOCALY NOT VALID");
                        
                        selectPosition(mouseCellPos);
                        if (selectedUnit != null){
                            if (selectedUnit.owerPlayer == playerActual && isLocalPlayer){
                                waitingForCreateOrder = true;
                            }else{
                                waitingForCreateOrder = false;
                            }
                        }

                    }

                } 
            }

            //derecho click
            if (Input.IsMouseButtonPressed(2)){
                deseleccionar();
            }
        }
    }

    //ORDERS
    public bool tryOrder(int unitX, int unitY, int destX, int destY, int orderType){
        //get basic data:
        Unit unit = dataTerrain.units[unitX,unitY];
        Cuadro cuadroUnit = dataTerrain.cuadros[unitX,unitY];

        Unit unitDestino = dataTerrain.units[destX,destY];
        Cuadro cuadroDestino = dataTerrain.cuadros[destX,destY];

        switch(orderType){
            case 0: //move
                return (unit.isValidMove(destX, destY));
                
            case 1: //atack
                return (unit.isValidAttack(unitX,unitY,destX, destY));
        }

        return true;
    }

        //callback when Game validate order
    public void execOrder(int unitX, int unitY, int destX, int destY, int orderType){
        //get basic data:
        Unit unit = dataTerrain.units[unitX,unitY];
        selectedUnit = unit;//using selectedUnit in others metods
        Cuadro cuadroUnit = dataTerrain.cuadros[unitX,unitY];

        Unit unitDestino = dataTerrain.units[destX,destY];
        Cuadro cuadroDestino = dataTerrain.cuadros[destX,destY];

        switch(orderType){
            case 0: //move
                GD.Print("Executing MOVE!");
                unit.moveOrder(destX, destY);
                dataTerrain.units[unitX,unitY] = null;
                dataTerrain.units[destX,destY] = unit;
                break;

            case 1: //attack
                GD.Print("Executing ATACK!");
                Vector2 dir = new Vector2(destX-unitX, destY-unitY);
                int hitPoints = unit.execAttack(dir);
                unitDestino.execHit(dir*-1,hitPoints,0);//puntos del golpe y defensa del terreno

                //kill? -> delete and report
                if (unitDestino.hp<=0){
                    EmitSignal("unitKill",dataTerrain.units[destX,destY].owerPlayer);//GUI and Game victory conditions
                    dataTerrain.units[destX,destY] = null;
                }

            break;
        }

        //waiting for finish
        waitingUnitAction = true;
    }

        //callback unit when move
    public void unitMoved(){
        GD.Print("worldMap.unitMoved(). Use this for reactions or fogOfWar-RevealMap");

    }

        //callback unit when animations finished
    public void ordersFinish(){
        //update navigations last units
        if (selectedUnit!=null){
            int x = (int)actualPoint.x;
            int y = (int)actualPoint.y;
            
            selectedUnit.updateSubNav(
                    dataTerrain.navigation.getSubZone(x,y ,20),
                    dataTerrain.navigation.getSubUnits(x,y,20));//update last selected unit or target

        }
        
        waitingUnitAction = false;
        EmitSignal("orderFinish",playerActual);//for IA and GUI
    }
        
        //callbak from game, when turn is over.
    public void nextTurn(int idPlayerNow, bool isLocal, int turn){
        //set control
        this.playerActual = idPlayerNow;
        this.isLocalPlayer = isLocal;
        //turn nothing is for gui.

        //quita seleccion
        deseleccionar();
        
        //update units new turn:
        for (int i = 0; i<dataTerrain.sizeMap; i++){
            for (int j = 0; j<dataTerrain.sizeMap; j++){
                Unit u = dataTerrain.units[j,i];
                if (u!=null){
                    u.newTurn(playerActual);//indicador de propiedad
                } 
            }
        }

    }

        //IA update unit subnav data
    public void updateUnit(int x, int y){
        Unit unit = dataTerrain.units[x,y];
        Cuadro[,] subCuadros = dataTerrain.navigation.getSubZone(x,y,20);//subconjunto terreno para busqueda optimizada
        Unit[,] subUnits = dataTerrain.navigation.getSubUnits(x,y,20);//subconjunto terreno para busqueda optimizada
        unit.updateSubNav(subCuadros,subUnits);//update
    }

    //SELECIONAR UNA CASILLA
    public void selectPosition(Vector2 tilePos){
        
        string idUnit = "None";//oculta panel por defecto
        string pp = "-1"; string puntosM = "0";

        selectionViewer.Position = tmTerrain.MapToWorld(tilePos)  + tmTerrain.CellSize/2f;

        //SELECCION
        if (tilePos != actualPoint){
            actualPoint = tilePos;
            selectionViewer.Visible=(true);
            
            if (selectedUnit!= null){
                selectedUnit.unSelectMe();
            }

            int x = (int)tilePos.x; 
            int y = (int)tilePos.y;
            selectedUnit = dataTerrain.units[x,y];

            if (selectedUnit != null){
                //GET AND PAINT UNIT SELECTED WALK AREA, ONLY IN LOCAL PLAYER
                if (isLocalPlayer) selectUnit(x,y);
                
                //muestra el panel de seleccion con info de unit
                idUnit = selectedUnit.nameUnit;
                pp = selectedUnit.owerPlayer.ToString();
                puntosM = selectedUnit.movePoints + "/" + selectedUnit.maxMovePoints; 
            }            

        }else{
            deseleccionar();
        }

        
        //show/hide GUI panel info unit selected
        string[] datas = new string[]{
            idUnit, pp, puntosM
        };
        EmitSignal("SelectedPoint",tilePos,datas);
    }

        //select and update subNav 
    public void selectUnit(int x, int y){
        Cuadro[,] subCuadros = dataTerrain.navigation.getSubZone(x,y,20);//subconjunto terreno para busqueda optimizada
        Unit[,] subUnits = dataTerrain.navigation.getSubUnits(x,y,20);//subconjunto terreno para busqueda optimizada
        selectedUnit.updateSubNav(subCuadros,subUnits);//update
        
        selectedUnit.selectMe(playerActual,isLocalPlayer);//show zones: move and atack
        
    }

    public void deseleccionar(){
        actualPoint = Vector2.One*-1;
        selectionViewer.Visible=(false);

        if (selectedUnit!=null){
            selectedUnit.unSelectMe();
        }
        if (isGame) waitingForCreateOrder = false;

        EmitSignal("SelectedPoint",actualPoint,new string[]{"None", "-1", "0"} );
    }
    
    ////EDICION INDIVIDUAL: lo llama una señal de la gui
    public void onClickEditButton(int index){
        
        if (index == -1){
            editionType = -1;
            isEdition = false;
        } else{
            //activa edicion
            editionType = index;
            isEdition = true;
        }
        deseleccionar();//borra las selecciones
    }

    public void changeDataTerrain(Vector2 position, int type){
        
        switch(type){
            case 0: 
                dataTerrain.addTerrain(position,Cuadro.TIPO.DEEPWATER,tileMaps);
                break;
                
            case 1: 
                dataTerrain.addTerrain(position,Cuadro.TIPO.WATER,tileMaps);
                break; 

            case 2: 
                dataTerrain.addTerrain(position,Cuadro.TIPO.GROUND,tileMaps);
                break; 

            case 3: 
                dataTerrain.addTerrain(position,Cuadro.TIPO.GRASS,tileMaps);
                break;; 

            case 4: 
                dataTerrain.addTerrain(position,Cuadro.TIPO.FOREST,tileMaps);
                break; 

            case 5: 
                dataTerrain.addTerrain(position,Cuadro.TIPO.HILL,tileMaps);
                break; 

            case 6: 
                dataTerrain.addTerrain(position,Cuadro.TIPO.MOUNTAIN,tileMaps);
                break;

            case 7: 
                dataTerrain.addTerrain(position,Cuadro.TIPO.TOP,tileMaps);
                break; 

            case 8: //borrar rios y carreteras
                dataTerrain.removeDetail(position,tileMaps);
                break; 
            
            case 9: //roads
                dataTerrain.addRoad(position,tileMaps);
                break; 

            case 10://river
                dataTerrain.addRiver(position,tileMaps);
                break;

            case 11://buildings
                dataTerrain.addBuilding(position,tileMaps);
                break;

                //delete UNIT
                case 100:
                    if (dataTerrain.deleteUnit(position))  GD.Print("Delete unit " + type);
                    break;
        }

        //UNIDATES ADD:
        if(type>100){
            type -= 101;
            Unit unit = dataTerrain.addUnit( prefabsUnits[type], position, this.idPlayerPropietario ,tmTerrain,  nUnits); 
            if (unit!= null){
                //GD.Print(String.Format("add unit {0}", type));
            }
        } 

        //oculta puntero y rutas
        selectPosition(actualPoint);
    }

    public void changeIdPlayerEdition(int idPlayer){
        this.idPlayerPropietario = idPlayer;
    }

    //NEW MAP: create natural worlds & signal
    public void generateTerrain(float[] data){
        //init
        selectionViewer.Visible=(false);
        actualSelectionPoint = Vector2.Zero;
        
        //delete old units view
        for (int i = 0; i < nUnits.GetChildCount() ;i++){
            nUnits.GetChild<Node2D>(i).QueueFree();
        }
        
        //create data
        dataTerrain = new DataTerrain("GENERATED TERRAIN",data);
        
        deferredCallViewUpdate();//only close terrain view

        //views/camera map info
        emmitMapData();
        
    }
    
    public void saveData(string name){

        GD.Print("Saving "+name);
        File file = new File();
        Directory dir = new Directory();
        string path = "user://terrains";

        if (dir.Open(path) != Error.Ok){
            dir.MakeDir(path);
            GD.Print("Dir created");
        }

        //open
        Error error = file.Open(string.Format("{0}//{1}.dat",path,name), File.ModeFlags.Write);
        if (error!= Error.Ok) return;

        //save  
        file.StoreLine("<data>");
             file.StoreLine("<name>"+dataTerrain.name+"</name>");
             file.StoreLine("<seed>"+dataTerrain.seed+"</seed>");
             file.StoreLine("<sizeMap>"+dataTerrain.sizeMap+"</sizeMap>");

            file.StoreLine("<cells>");
            for (int i = 0; i<dataTerrain.sizeMap; i++){
                for (int j = 0; j<dataTerrain.sizeMap; j++){
                    Cuadro c = dataTerrain.cuadros[j,i];
                    Unit u = dataTerrain.units[j,i];
                    file.StoreLine("<c>");
                        //casilla de terreno
                        file.StoreLine("<tipo>"+(int)c.tipo+"</tipo>");
                        file.StoreLine("<height>"+c.height+"</height>");
                        file.StoreLine("<cosmeticDetail>"+c.cosmeticDetail+"</cosmeticDetail>");
                        file.StoreLine("<buildingDetail>"+c.buildingDetail+"</buildingDetail>");
                        file.StoreLine("<river>"+c.river+"</river>");
                        file.StoreLine("<road>"+c.road+"</road>");
                        file.StoreLine("<building>"+c.building+"</building>");

                        //unit
                        string nameUnit = "-1"; int propietario = -1;
                        if (u!=null){
                            nameUnit = u.nameUnit;
                            propietario = u.owerPlayer;
                        }
                        file.StoreLine("<nameUnit>"+nameUnit+"</nameUnit>");
                        file.StoreLine("<propietario>"+propietario+"</propietario>");
                        
                    file.StoreLine("</c>");
                }
            }
            file.StoreLine("</cells>");
        file.StoreLine("</data>");
        //close
        file.Close();
    }

    public void loadData(string name){
        GD.Print("Loading " + name);
        File file = new File();
        string path = "user://terrains//" +name;

        if (!file.FileExists(path)){
        GD.Print("File no exist: " +path);
        return;
        }

        //init
        selectionViewer.Visible=(false);
        actualSelectionPoint = Vector2.Zero;
        
        //delete old units view
        for (int i = 0; i < nUnits.GetChildCount() ;i++){
            nUnits.GetChild<Node2D>(i).QueueFree();
        }

        //open file     
        XMLParser xmlP = new XMLParser();
        Error err = xmlP.Open(path);
        if (err != Error.Ok) return;

        //read data         
        dataTerrain = new DataTerrain("");
        xmlP.Read();//data
            xmlP.Read(); xmlP.Read(); dataTerrain.name = xmlP.GetNodeData();  xmlP.Read(); //name
            xmlP.Read(); xmlP.Read(); dataTerrain.seed = int.Parse(xmlP.GetNodeData());  xmlP.Read(); //seed
            xmlP.Read(); xmlP.Read(); dataTerrain.sizeMap = int.Parse(xmlP.GetNodeData());  xmlP.Read(); //size
            
            Cuadro[,] cuadros = new Cuadro[dataTerrain.sizeMap,dataTerrain.sizeMap];//los coloca despues
            Unit[,] units = new Unit[dataTerrain.sizeMap,dataTerrain.sizeMap];//los coloca despues

            xmlP.Read(); //cells
            for (int i = 0; i<dataTerrain.sizeMap; i++){
                for (int j = 0; j<dataTerrain.sizeMap; j++){
                    Cuadro c = new Cuadro();
                    xmlP.Read(); //cell
                        //CUADRO
                        xmlP.Read(); xmlP.Read(); c.tipo = (Cuadro.TIPO) int.Parse(xmlP.GetNodeData()); xmlP.Read(); //type
                        xmlP.Read(); xmlP.Read(); c.height = int.Parse(xmlP.GetNodeData()); xmlP.Read(); //heigth
                        xmlP.Read(); xmlP.Read(); c.cosmeticDetail = int.Parse(xmlP.GetNodeData()); xmlP.Read(); //cosmeticdetail
                        xmlP.Read(); xmlP.Read(); c.buildingDetail = int.Parse(xmlP.GetNodeData()); xmlP.Read(); //buldDetail
                        xmlP.Read(); xmlP.Read(); c.river = bool.Parse(xmlP.GetNodeData()); xmlP.Read(); //ri
                        xmlP.Read(); xmlP.Read(); c.road = bool.Parse(xmlP.GetNodeData()); xmlP.Read(); //ro
                        xmlP.Read(); xmlP.Read(); c.building = bool.Parse(xmlP.GetNodeData()); xmlP.Read(); //bu
                        cuadros[j,i] = c;
                        
                        //UNIT
                        xmlP.Read(); xmlP.Read();string nameUnit = xmlP.GetNodeData(); xmlP.Read(); //unit
                        xmlP.Read(); xmlP.Read();int propietario = int.Parse(xmlP.GetNodeData()); xmlP.Read(); //propietario

                        if(nameUnit!="-1"){
                            Unit unit = new Unit();
                            unit.nameUnit = nameUnit;
                            unit.owerPlayer = propietario;
                            units[j,i] = unit;
                        }

                    xmlP.Read(); //cell end
                }
            }

            xmlP.Read(); //cells end
        xmlP.Read(); //data end

        GD.Print("DATA LOADED."); 

        this.dataTerrain.cuadros = cuadros;
        this.dataTerrain.units = new Unit[dataTerrain.sizeMap,dataTerrain.sizeMap];

        // Uptate Navigation system with terrain
        this.dataTerrain.navigation.createNavigationSystem(this.dataTerrain.cuadros,this.dataTerrain.units);
        GD.Print("NAVIGATION CREATED."); 

        //add units
        for (int i = 0; i<dataTerrain.sizeMap; i++){
            for (int j = 0; j<dataTerrain.sizeMap; j++){
                if(units[j,i] != null){
                    int id =int.Parse(units[j,i].nameUnit);
                    dataTerrain.addUnit(prefabsUnits[id], new Vector2(j,i),units[j,i].owerPlayer,tmTerrain, nUnits); 
                }
            }
        }
        GD.Print("UNITS ADD."); 

        //THREAT RESTART
        if (thrCellViewControler!=null &&thrCellViewControler.IsAlive)thrCellViewControler.Abort();
        thrCellViewControler = new System.Threading.Thread(() => this.taskAsincViewControl());  thrCellViewControler.Start();

        //new view
        deferredCallViewUpdate();
        emmitMapData();
    }
    
    public void emmitMapData(){
        //terrains & details 11
        int[] terrains = new int[]{
            0,0,0,0,0,0,0,0,//8 terrain
            0,0,0 //3 detail
        };

        //players and units
        int[] playerUnits = new int[]{
            0,0,0,0,0,0,0,0 //8 player max
        };

        for (int i = 0; i<dataTerrain.sizeMap; i++){
            for (int j = 0; j<dataTerrain.sizeMap; j++){

                Cuadro cuadro = dataTerrain.cuadros[j,i];
                if (cuadro.tipo == Cuadro.TIPO.DEEPWATER) terrains[0]++;
                if (cuadro.tipo == Cuadro.TIPO.WATER) terrains[1]++;
                if (cuadro.tipo == Cuadro.TIPO.GROUND) terrains[2]++;
                if (cuadro.tipo == Cuadro.TIPO.GRASS) terrains[3]++;
                if (cuadro.tipo == Cuadro.TIPO.FOREST) terrains[4]++;
                if (cuadro.tipo == Cuadro.TIPO.HILL) terrains[5]++;
                if (cuadro.tipo == Cuadro.TIPO.MOUNTAIN) terrains[6]++;
                if (cuadro.tipo == Cuadro.TIPO.TOP) terrains[7]++;
                if (cuadro.river) terrains[8]++;
                if (cuadro.road) terrains[9]++;
                if (cuadro.building) terrains[10]++;

                Unit unit = dataTerrain.units[j,i];
                if (unit!=null){
                    playerUnits[unit.owerPlayer]++;
                }
            }
        }
        EmitSignal("mapGenerated",dataTerrain.sizeMap,terrains,playerUnits); //camera and gui
    }

    //RENDER CONTROL: only change scene at close camera position
    public void cameraMove(Vector2 position){
        cameraPosition = position;
    }

    private void taskAsincViewControl(){
        Vector2 lastPosition = Vector2.Zero;

        while(this!= null){
            if((lastPosition - cameraPosition).LengthSquared()< 500) continue;//optimized
            lastPosition = cameraPosition;//optimizar
            CallDeferred("deferredCallViewUpdate");//secure call update View
        }
        GD.Print("End thr");
    }

    private void deferredCallViewUpdate(){
        Vector2 mapPos = tmTerrain.WorldToMap(cameraPosition);
        int x = (int) mapPos.x; int y = (int) mapPos.y;

        //Control views
        dataTerrain.GetViewPos(x, y, maxLengthTileMapView, minLengthUpdateView , tileMaps);
    }

}

// ███████████████████████████████████ TERRAIN

public class DataTerrain{
    public string name;
    public int seed;
    public int sizeMap;
    public Cuadro[,] cuadros;//data terrain
    public Unit[,] units; //unidades

    //GENERATION
    OpenSimplexNoise noise = new OpenSimplexNoise();
    Random random = new Random();

    //GENERAL NAVIGATION SYSTEM 
    public Navigation navigation;

    ////////////////////////////////////////////////////////NATURAL GENERATION
    public DataTerrain(string name, float[] data){
        
        this.name = name;
        this.navigation = new Navigation();

        this.seed = (int)data[0];//get Seed 
        this.sizeMap = (int) data[1];//sizeMap

        //INIT DATA
        this.cuadros = new Cuadro[sizeMap,sizeMap]; 
        this.units = new Unit[sizeMap,sizeMap]; 

        for (int i = 0; i<sizeMap; i++){
            for (int j = 0; j<sizeMap; j++){
                cuadros[j,i] = new Cuadro();//nuevos
                units[j,i] = null;
            }
        }

        //NOISE AND RANDOM
        if ( this.seed == 0){
            this.seed = random.Next();
        } 
        random = new Random(this.seed);
        noise = new OpenSimplexNoise();
        noise.Seed = this.seed;
        noise.Octaves = (int) data[2];
        float period = data[3]; 
        noise.Period = period;
        noise.Lacunarity = data[4];
        noise.Persistence = data[5];
        
        //DATAS MIN HEIGHTS (6 datas)
        int[] hDatas = new int[]{
            (int)data[6],(int)data[7],(int)data[8],(int)data[9],(int)data[10],(int)data[11],(int)data[12]
        };

        int riversCount = (int)data[13];//

        //generate Heights
        int maxPassses = 10;
        float heightStep = 10f;
        int nOffSetX = random.Next(-sizeMap,sizeMap);
        int nOffSetY = random.Next(-sizeMap,sizeMap);
        
        for(int p = 0; p < maxPassses; p++){
            noise.Period = p * period;

            for (int i = 0; i<sizeMap; i++){
                for (int j = 0; j<sizeMap; j++){
                    cuadros[j,i].height += (int) Mathf.Abs(noise.GetNoise2d(j+nOffSetX,i+nOffSetY) * heightStep);
                }
            }
        }
        
        //tops for rivers generation:
        List<Vector2> tops = new List<Vector2>();
        List<Vector2> waters = new List<Vector2>();

        //HEIGHT TO DATA
        for (int i = 0; i<sizeMap; i++){
            for (int j = 0; j<sizeMap; j++){

                Cuadro cuadro = cuadros[j,i];
                Vector2 pos = new Vector2(j,i);
                int height = cuadro.height;

                //BASIC TYPE
                cuadro.tipo = Cuadro.TIPO.DEEPWATER;//DeepW default
                if (cuadro.height>=hDatas[0]){ cuadro.tipo = Cuadro.TIPO.WATER; }
                if (cuadro.height>=hDatas[1]){ cuadro.tipo = Cuadro.TIPO.GROUND; }
                if (cuadro.height>=hDatas[2]){ cuadro.tipo = Cuadro.TIPO.GRASS;  }
                if (cuadro.height>=hDatas[3]){ cuadro.tipo = Cuadro.TIPO.FOREST; }
                if (cuadro.height>=hDatas[4]){ cuadro.tipo = Cuadro.TIPO.HILL;  }
                if (cuadro.height>=hDatas[5]){ cuadro.tipo = Cuadro.TIPO.MOUNTAIN; }
                if (cuadro.height>=hDatas[6]){ cuadro.tipo = Cuadro.TIPO.TOP;}

                //bosque 5% en altura de ground 2
                if (hDatas[2] <= height && height < hDatas[3] && random.Next(0,100) < 4){
                    cuadro.setRandomTree(random);
                }
                //bosque 90% en altura de bosque
                if (hDatas[3] <= height && height < hDatas[4] && random.Next(0,100) < 91){
                    cuadro.setRandomTree(random);
                }
                //bosque 25% en rocas
                if (hDatas[4] <= height && height < hDatas[5] && random.Next(0,100) < 26){
                    cuadro.setRandomTree(random);
                }
                //montaña 90% apartir de altura de montaña             
                if (height >= hDatas[5] && random.Next(0,100) < 91){
                    cuadro.setRandomMontain(random);
                }

                //add tops and water for river algoritm
                if (cuadro.tipo == Cuadro.TIPO.TOP) tops.Add(pos); 
                if (cuadro.tipo == Cuadro.TIPO.WATER) waters.Add(pos); 

            }
        }
        
        //nav
        navigation.createNavigationSystem(cuadros,units);

        //RIVERS
        if (riversCount>0){
            for (int count = 0; count < riversCount; count++){
                if(tops.Count>0){
                    //init point
                    int index = random.Next(0,tops.Count);
                    Vector2 posA = tops[index];
                    tops.RemoveAt(index);

                    //final point
                    if(waters.Count>0){
                        index = random.Next(0,waters.Count);
                        Vector2 posB = waters[index];
                        waters.RemoveAt(index);

                        //length control (50x50) para controlar el tiempo de A*
                        if ((posB-posA).LengthSquared()<5000){
                            //calculate
                            navigation.createPath((int)posA.x, (int)posA.y,(int)posB.x, (int)posB.y,0);//por coste por altura
                            createRiverPath(navigation.path);//paint rivers
                        }                   
                    }
                }
            }
            
            //no rivers on mountains or waters
            for (int i = 0; i<sizeMap; i++){
                for (int j = 0; j<sizeMap; j++){

                    Cuadro cuadro = cuadros[j,i];
                    if(cuadro.river){
                        if(cuadro.tipo == Cuadro.TIPO.DEEPWATER||
                            cuadro.tipo == Cuadro.TIPO.WATER||
                            cuadro.tipo == Cuadro.TIPO.MOUNTAIN||
                            cuadro.tipo == Cuadro.TIPO.TOP){
                            
                            cuadro.river = false;
                            cuadro.cosmeticDetail = -1;
                            updateLineView(j,i);
                        }
                        if (cuadro.tipo == Cuadro.TIPO.MOUNTAIN||
                            cuadro.tipo == Cuadro.TIPO.TOP){
                                cuadro.setRandomMontain(random);//detail mnt
                        }
                    }
                }
            }
        }
        
        //CIUDADES

        //FINALY UPDATE A*
        navigation.createNavigationSystem(cuadros,units);
        //fin generation
    }

    public DataTerrain(string name){
        this.name = name;
        this.noise = new OpenSimplexNoise();
        this.random = new Random();
        this.navigation = new Navigation();
        cuadros = new Cuadro[0,0];
        units = new Unit[0,0];
    }

    public void createRiverPath(Vector3[] path){
        
        Vector2 lastPw = new Vector2(path[0].x, path[0].y);
        foreach (Vector3 pw in path){

            //CHANGE
            int x =(int)lastPw.x; 
            int y = (int)lastPw.y;
            Cuadro cuadro = cuadros[x,y];

            //if (cuadro.river) return;//colision con otro rio -> sale

            //update views (me,up,rigth,down,left) with limits control
            cuadro.river = true;
            updateLineView(x,y);

            //neighbors positions
            int upX =  x,  upY = y-1;
            int rightX = x+1, rightY = y;
            int downX = x, downY = y+1;
            int leftX = x-1, leftY = y;

            //UP
            if (upY >= 0) updateLineView(upX,upY);
            //R
            if (rightX<sizeMap) updateLineView(rightX,rightY);
            //D
            if (downY<sizeMap)updateLineView(downX,downY);
            //L
            if (leftX >= 0) updateLineView(leftX,leftY);
            
            //NEXT
            lastPw = new Vector2(pw.x,pw.y);//next;
        }
    }

    //GET DATA and VIEWS
    public Cuadro getCuadro (Vector2 position){
        int[] pos = new int[]{(int)position.x,(int)position.y};
        if (!limitsControl(pos[0],pos[1])) return null;
        return cuadros[pos[0],pos[1]];
    }

    public void GetAllView(TileMap[] tilemaps){
        tilemaps[0].Clear();
        tilemaps[1].Clear();
        tilemaps[2].Clear();

        for (int i = 0; i<sizeMap; i++){
            for (int j = 0; j<sizeMap; j++){
                Cuadro cuadro = cuadros[j,i];
                tilemaps[0].SetCell(j,i,cuadro.getViewTerrain());
                tilemaps[1].SetCell(j,i,cuadro.cosmeticDetail);
                tilemaps[2].SetCell(j,i,cuadro.buildingDetail);
            }
        }

    }
    
    public void GetViewPos(int centerX, int centerY, int lenght, int limit, TileMap[] tilemaps){
        //center rect
        int initX = centerX - lenght;  int initY = centerY - lenght;
        int finX = centerX + lenght; int finY = centerY + lenght;

        //limit rect
        int jMin = centerX - limit; int jMax = centerX + limit;
        int iMin = centerY - limit; int iMax = centerY + limit;

         for (int i = iMin; i< iMax; i++){
            for (int j = jMin; j < jMax; j++){

                if ((j<initX || j>finX) || (i<initY || i > finY)){
                    //oculta
                    tilemaps[0].SetCell(j,i,-1);
                    tilemaps[1].SetCell(j,i,-1);
                    tilemaps[2].SetCell(j,i,-1);
                }else{
                    //muestra
                    if (limitsControl(j,i)){
                        Cuadro cuadro = cuadros[j,i];
                        tilemaps[0].SetCell(j,i,cuadro.getViewTerrain());
                        tilemaps[1].SetCell(j,i,cuadro.cosmeticDetail);
                        tilemaps[2].SetCell(j,i,cuadro.buildingDetail);
                    }
                }
            }
        }
    }

    private bool limitsControl(int x, int y){
        bool isOk = true;
        
        isOk &= (x>=0 && x<sizeMap);
        isOk &= (y>=0 && y<sizeMap);
        
        return isOk;
    }

    //INDIVIDUAL TERRAIN EDITION
    public void addTerrain(Vector2 position, Cuadro.TIPO type, TileMap[] tilemaps ){

        int x =(int)position.x; 
        int y = (int)position.y;

        if (!limitsControl(x,y)) return;

        Cuadro cuadro = cuadros[x,y];

        //clear
        cuadro.cosmeticDetail = -1;
        cuadro.buildingDetail = -1;
        cuadro.road = false;
        cuadro.river = false;
        cuadro.building = false;

        //add 
        switch(type){
            case Cuadro.TIPO.DEEPWATER:
                cuadro.tipo = Cuadro.TIPO.DEEPWATER;
                cuadro.cosmeticDetail = -1;
            break;

            case Cuadro.TIPO.WATER:
                cuadro.tipo = Cuadro.TIPO.WATER;
                cuadro.cosmeticDetail = -1;
            break;

            case Cuadro.TIPO.GROUND:
                cuadro.tipo = Cuadro.TIPO.GROUND;
                cuadro.cosmeticDetail = -1;
            break;

            case Cuadro.TIPO.GRASS:
                cuadro.tipo = Cuadro.TIPO.GRASS;
                cuadro.cosmeticDetail = -1;
                if (random.Next(0,100) < 4) cuadro.setRandomTree(random);
            break;

            case Cuadro.TIPO.FOREST:
                cuadro.tipo = Cuadro.TIPO.FOREST;
                cuadro.cosmeticDetail = -1;
                if (random.Next(0,100) < 91) cuadro.setRandomTree(random);
            break;

            case Cuadro.TIPO.HILL:
                cuadro.tipo = Cuadro.TIPO.HILL;
                cuadro.cosmeticDetail = -1;
                if (random.Next(0,100) < 26) cuadro.setRandomTree(random);
            break;
            
            case Cuadro.TIPO.MOUNTAIN:
                cuadro.tipo = Cuadro.TIPO.MOUNTAIN;
                
                if (random.Next(0,100) < 85) cuadro.setRandomMontain(random);
            break;

            case Cuadro.TIPO.TOP:
                cuadro.tipo = Cuadro.TIPO.TOP;
                cuadro.cosmeticDetail = -1;
                if (random.Next(0,100) < 91) cuadro.setRandomMontain(random);
            break;
        }

        //to tilemaps view
        tilemaps[0].SetCell(x,y, cuadro.getViewTerrain());
        tilemaps[1].SetCell(x,y, cuadro.cosmeticDetail);
        tilemaps[2].SetCell(x,y, cuadro.buildingDetail);

        //navigation cost update
        navigation.updateWeightsAstars(x,y);      
    }

    public void addRoad(Vector2 position, TileMap[] tilemaps){
         addLine(position,0, tilemaps);
     }
    
    public void addRiver(Vector2 position, TileMap[] tilemaps){
        int x =(int)position.x; int y = (int)position.y;
        if (!limitsControl(x,y)) return;

        Cuadro cuadro = cuadros[x,y];
        if (cuadro.tipo == Cuadro.TIPO.DEEPWATER || cuadro.tipo == Cuadro.TIPO.WATER ) return;

        addLine(position,1, tilemaps);//ok
     }

    public void addBuilding(Vector2 position, TileMap[] tilemaps){
        int x =(int)position.x; 
        int y = (int)position.y;
        if (!limitsControl(x,y)) return;

        Cuadro cuadro = cuadros[x,y];
        
        if (cuadro.tipo == Cuadro.TIPO.DEEPWATER || cuadro.tipo == Cuadro.TIPO.WATER ) return;

        //clear others
        cuadro.road = false;
        cuadro.river = false;
        cuadro.cosmeticDetail = -1;

        //add random building detail
        cuadro.setRandomBuilding(random);
        cuadro.building = true;

        //to tilemaps view
        tilemaps[0].SetCell(x,y, cuadro.getViewTerrain());
        tilemaps[1].SetCell(x,y, cuadro.cosmeticDetail);
        tilemaps[2].SetCell(x,y, cuadro.buildingDetail);

        //navigation cost update
        navigation.updateWeightsAstars(x,y);   
    }

    public void removeDetail(Vector2 position, TileMap[] tilemaps){
         addLine(position,-1, tilemaps);
     }
    
    //RIVERS AND ROADS
    private void addLine(Vector2 position, int detailType ,TileMap[] tilemaps){
        
        int x =(int)position.x; 
        int y = (int)position.y;
        
        if (!limitsControl(x,y)) return;
        Cuadro cuadro = cuadros[x,y];

        //add data 
        if (detailType ==-1){
            cuadro.road = false;
            cuadro.river = false;
        }

        //puede haber rio y road
        if (detailType == 0){
            cuadro.road = true;
        } 

        if (detailType == 1){
            cuadro.river = true;
        }

        //quita el building
        cuadro.building = false;
        cuadro.buildingDetail = -1;
        tilemaps[2].SetCell(x,y,  cuadro.buildingDetail);

        //neighbors positions
        int upX =  x,  upY = y-1;
        int rightX = x+1, rightY = y;
        int downX = x, downY = y+1;
        int leftX = x-1, leftY = y;

        //update views (me,up,rigth,down,left) with limits control
        if (detailType !=-1) {
            updateLineView(x,y);
        }else{
            cuadro.cosmeticDetail = -1;
        }
        tilemaps[0].SetCell(x,y, cuadro.getViewTerrain());
        tilemaps[1].SetCell(x,y, cuadro.cosmeticDetail);
        
        //UP
        if (upY >= 0){
            updateLineView(upX,upY);
            tilemaps[0].SetCell(upX,upY,cuadros[upX,upY].getViewTerrain());
            tilemaps[1].SetCell(upX,upY,cuadros[upX,upY].cosmeticDetail);
        }
        //R
        if (rightX<sizeMap){
            updateLineView(rightX,rightY);
            tilemaps[0].SetCell(rightX,rightY,cuadros[rightX,rightY].getViewTerrain());
            tilemaps[1].SetCell(rightX,rightY,cuadros[rightX,rightY].cosmeticDetail);
        }
        //D
        if (downY<sizeMap){
            updateLineView(downX,downY);
            tilemaps[0].SetCell(downX,downY,cuadros[downX,downY].getViewTerrain());
            tilemaps[1].SetCell(downX,downY,cuadros[downX,downY].cosmeticDetail);
        }
        //L
        if (leftX >= 0){
            updateLineView(leftX,leftY);
            tilemaps[0].SetCell(leftX,leftY,cuadros[leftX,leftY].getViewTerrain());
            tilemaps[1].SetCell(leftX,leftY,cuadros[leftX,leftY].cosmeticDetail);
        }
        
        //navigation cost update
        navigation.updateWeightsAstars(x,y);    

        //END
    }

    private void updateLineView(int x, int y){
        Cuadro cuadro = cuadros[x,y];
        if (!limitsControl(x,y)) return;

        int upX =  x,  upY = y-1;
        int rightX = x+1, rightY = y;
        int downX = x, downY = y+1;
        int leftX = x-1, leftY = y;

        //Find tiles
        bool upA = false; bool upB = false;
        if (limitsControl(upX,upY)){
            upA = (cuadros[upX,upY].road);
            upB = (cuadros[upX,upY].river);
        }

        bool rightA = false; bool rightB = false;
        if (limitsControl(rightX,rightY)){
            rightA = (cuadros[rightX,rightY].road);
            rightB = (cuadros[rightX,rightY].river);
        }

        bool downA = false; bool downB = false;
        if (limitsControl(downX,downY)){
            downA = (cuadros[downX,downY].road);
            downB = (cuadros[downX,downY].river);
        }

        bool leftA = false; bool leftB = false;
        if (limitsControl(leftX,leftY)){
            leftA = (cuadros[leftX,leftY].road);
            leftB = (cuadros[leftX,leftY].river);
        }

        //get my river detail style
        if (cuadro.river)  cuadro.cosmeticDetail = cuadro.getRiverStyle(upB,rightB,downB,leftB);
        //get my road detail style
        if (cuadro.road)  cuadro.cosmeticDetail = cuadro.getRoadStyle(upA,rightA,downA,leftA);
    }

    ////////////////////////////////////////////////////// UNITS
    public Unit addUnit(PackedScene prefab, Vector2 cellCoords, int idPlayer,TileMap tmTilemap, Node2D parent){

        int x = (int)cellCoords.x;
        int y = (int) cellCoords.y;

        //ocupado?
        Unit actualUnit = units[x,y];
        if (actualUnit != null){
            return null; //sitio ocupado
        }

        //limites?
        if (!limitsControl(x,y)) return null;

        //instancia + parent + worldPos
        Unit unit = prefab.Instance() as Unit;
        parent.AddChild(unit); 
        unit.Position = tmTilemap.MapToWorld(cellCoords) + new Vector2(tmTilemap.CellSize.x/2 ,tmTilemap.CellSize.y/2);

        //el tipo de terreno permite al tipo de agente?
        int idPoint = navigation.getIndexNavPoint(x,y);
        if (! navigation.getAstarAgent(unit.navAgentType).HasPoint(idPoint)){
            unit.QueueFree();
            return null;//terreno no valido para el tipo de agente!
        } 

        /////////////////////////////////////////////////////////////////////////    OK
        if (actualUnit != null) actualUnit.QueueFree();//el viejo se borra

        unit.owerPlayer = idPlayer;
        units[x,y] = unit;//to terrain data and update subNav
        unit.updateSubNav(
                    navigation.getSubZone(x,y,20),
                    navigation.getSubUnits(x,y,20));

        //world position
        unit.wpX = x;
        unit.wpY = y;

        //conect signals move and final actions whit worldMapcontroler
        unit.Connect("moveCell",parent.GetParent().GetParent(),"unitMoved");
        unit.Connect("ordersDone",parent.GetParent().GetParent(),"ordersFinish");
        //GD.Print("Unit add ok");

        return unit;
    }

    public bool deleteUnit( Vector2 cellCoords){
        int x = (int)cellCoords.x;
        int y = (int) cellCoords.y;

        //ocupado?
        Unit actualUnit = units[x,y];
        if (actualUnit != null){
            actualUnit.QueueFree();//borra
            units[x,y] = null;//vacio
            return true;
        }

        return false;
    }

    public void updateAllNavigationsUnits(){
        //update navigations all units
        for (int i = 0; i<sizeMap; i++){
            for (int j = 0; j<sizeMap; j++){
                Unit u = units[j,i];
                if (u == null)continue;
                u.updateSubNav(
                    navigation.getSubZone(j,i,20),
                    navigation.getSubUnits(j,i,20));//update
            }
        }
    }

}

// ███████████████████████████████████
public class Cuadro{
    public enum TIPO{DEEPWATER,WATER,GROUND,GRASS,FOREST,HILL,MOUNTAIN,TOP}

    public TIPO tipo = TIPO.DEEPWATER;

    public int cosmeticDetail = -1; public int buildingDetail = -1;

    public int height = 0;

    public bool road = false, river = false, building = false;

    //MAPEO: DATO -> TILESET CELL INDEX
    public int getViewTerrain(){
        int cellValue = 0;
        //maping data terrain to index tileset.
        switch(tipo){
            case TIPO.DEEPWATER: cellValue = 0; break;//water deep
            case TIPO.WATER: cellValue = 1; break;//water 
            case TIPO.GROUND: cellValue = 2; break;//beach
            case TIPO.GRASS: cellValue = 3; break;//glass
            case TIPO.FOREST: cellValue = 4; break;//glass deep
            case TIPO.HILL: cellValue = 5; break;//rocks
            case TIPO.MOUNTAIN: cellValue = 6; break;//mountain
            case TIPO.TOP: cellValue = 7; break;//top
        }
        return cellValue;
    }

    public void setRandomTree(Random random){
        int cellValue = -1;
        //trees
        switch(random.Next(0,8)){
            case 0: cellValue = 8; break;//tree1
            case 1: cellValue = 9; break;//tree2
            case 2: cellValue = 10; break;//tree3
            case 3: cellValue = 11; break;//tree4
            case 4: cellValue = 12; break;//tree5
            case 5: cellValue = 13; break;//tree6
            case 6: cellValue = 14; break;//tree7
            case 7: cellValue = 15; break;//tree8
        }
        cosmeticDetail= cellValue;
    }

    public void setRandomMontain(Random random){
        int cellValue = -1;
        //trees
        switch(random.Next(0,4)){
            case 0: cellValue = 16; break;//mont1
            case 1: cellValue = 17; break;//mont2
            case 2: cellValue = 18; break;//mont3
            case 3: cellValue = 19; break;//mont4
        }
        cosmeticDetail= cellValue;
    }

    public void setRandomBuilding(Random random){
        int cellValue = -1;
        switch(random.Next(0,6)){
            case 0: cellValue = 42; break;//b1
            case 1: cellValue = 43; break;//b2
            case 2: cellValue = 44; break;//b3
            case 3: cellValue = 45; break;//b4
            case 4: cellValue = 46; break;//b5
            case 5: cellValue = 47; break;//b6
        }
        buildingDetail = cellValue;
    }

    //lines styles
    public int getRoadStyle(bool up, bool right, bool down, bool left){
        int cv = 20;
        
        //2
        if (up || down) cv = 21;//ok
        if (right || left) cv = 20;//ok
        
        //3
        if (up && right) cv = 23;//ok
        if (up && left) cv = 24;//ok

        if (down && right) cv = 25;//ok
        if (down && left) cv = 26;//ok

        //4
        if (!up && right && down && left) cv = 27;
        if (up && !right && down && left) cv = 28;
        if (up && right && !down && left) cv = 29;
        if (up && right && down && !left) cv = 30;

        //5
        if (up && right && down && left) cv = 22;

        return cv;
    }

    public int getRiverStyle(bool up, bool right, bool down, bool left){
        int cv = 31;
        
        //2
        if (up || down) cv = 32;//ok
        if (right || left) cv = 31;//ok
        
        //3
        if (up && right) cv = 34;//ok
        if (up && left) cv = 35;//ok

        if (down && right) cv = 36;//ok
        if (down && left) cv = 37;//ok

        //4
        if (!up && right && down && left) cv = 38;
        if (up && !right && down && left) cv = 39;
        if (up && right && !down && left) cv = 40;
        if (up && right && down && !left) cv = 41;

        //5
        if (up && right && down && left) cv = 33;

        return cv;
    }

}

public class Navigation{
    //NAVIGATION SYSTEM
    private  AStar aStar = new AStar();
    private  AStar aStarShip = new AStar();
    private  AStar aStarHuman = new AStar();
    private  AStar aStarCar = new AStar();
    private  AStar aStarHeavy = new AStar();
    private  AStar aStarAirLow= new AStar();
    private  AStar aStarAir = new AStar();
    private  AStar aStarVision = new AStar();
    private AStar [] aStarAll;//all A*

    //data terrain
    public int sizeMap;
    public Cuadro[,] cuadros;
    public Unit[,] units;
    public int starPoint, finalPoint;
    public Vector3[] path = new Vector3[]{Vector3.Zero};
    public float totalPathWeight;
    public int typeAgent;

    //METODOS

    ////////////////////////////////////////////////////// NAVIGATION  A*
    public void createNavigationSystem(Cuadro[,] cuadros, Unit[,] units){
        this.sizeMap = cuadros.GetLength(0);
        this.cuadros = cuadros;
        this.units = units;

        createAstar(aStar,0);
        createAstar(aStarShip,1);
        createAstar(aStarHuman,2);
        createAstar(aStarCar,3);
        createAstar(aStarHeavy,4);
        createAstar(aStarAirLow,5);
        createAstar(aStarAir,6);
        createAstar(aStarVision,7);

        aStarAll = new AStar[]{
            aStar,aStarShip,aStarHuman,aStarCar,aStarHeavy,aStarAirLow,aStarAir,aStarVision
        };
    }
    
    private void createAstar(AStar aStar, int agentType){
        //create points
        for (int i = 0; i<sizeMap; i++){
            for (int j = 0; j<sizeMap; j++){

                Vector3 point = new Vector3 (j,i,0f);
                int indexP = getIndexNavPoint(j,i);
                float asWeight = getNavWeight(j,i,agentType);
                if(asWeight==int.MaxValue)continue;//no navegables se marcan con maxvalue
                aStar.AddPoint(indexP,point,asWeight);//cuadros[j,i].height
            }
        }

        //conect neigbours points
        for (int i = 0; i<sizeMap; i++){
            for (int j = 0; j<sizeMap; j++){

                Vector3 point = new Vector3 (j,i,0f);
                int indexP = getIndexNavPoint(j,i);

                if (!aStar.HasPoint(indexP))continue;
                
                int upX =  j,  upY = i-1;
                int rightX = j+1, rightY =i;
                int downX = j, downY = i+1;
                int leftX = j-1, leftY = i;

                if (limitsControl(upX,upY)){
                    int index2 = getIndexNavPoint(upX,upY);
                    if(aStar.HasPoint(index2)){
                        aStar.ConnectPoints(indexP, index2, false);
                    }
                }

               if (limitsControl(rightX,rightY)){
                    int index2 = getIndexNavPoint(rightX,rightY);
                    if(aStar.HasPoint(index2)){
                        aStar.ConnectPoints(indexP, index2, false);
                    }
                }

                if (limitsControl(downX,downY)){
                    int index2 = getIndexNavPoint(downX,downY);
                    if(aStar.HasPoint(index2)){
                        aStar.ConnectPoints(indexP, index2, false);
                    }
                }
                
                if (limitsControl(leftX,leftY)){
                    int index2 = getIndexNavPoint(leftX,leftY);
                    if(aStar.HasPoint(index2)){
                        aStar.ConnectPoints(indexP, index2, false);
                    }
                }            
            }
        }
        //end astar
    }
    //IDs
    public int getIndexNavPoint(int x, int y){
        return x + sizeMap * y; //0,0 -> 0 | 1,0 -> 1 | 2,3 -> 302 ...
    }
    
    //RUTAS
    public void createPath(int aX, int aY, int bX, int bY, int typeAgent){
        if (!limitsControl(aX,aY) && limitsControl(bX,bY)){
            GD.Print("LIMITES!");
            return;
        }

        starPoint = getIndexNavPoint(aX, aY);
        finalPoint = getIndexNavPoint(bX, bY);
        this.typeAgent = typeAgent;

        createActualPath();
    }

    public void createActualPath(){
        //calcular ruta con el aStar correspondiente
        AStar myAstar = getAstarAgent(typeAgent);
        
        totalPathWeight = 0; //no se puede llegar
        path = new Vector3[0];

        //calcule patch
        if (myAstar.HasPoint(finalPoint) && myAstar.HasPoint(starPoint)){
            
            //make path
            path = myAstar.GetPointPath(starPoint,finalPoint); 
            
            //total Weight (no first point)
            for (int i = 1; i<path.Length;i++){
                int id = getIndexNavPoint((int)path[i].x, (int)path[i].y);
                totalPathWeight += myAstar.GetPointWeightScale(id);//total weight
            }

            if (path.Length>50){
                //GD.Print("A* Long path!" + path[0] + " -> " + path[path.Length-1] + " LENGHT: " + path.Length + " W: " + totalPathWeight);
            }
            
        }else{
            //GD.Print("POINT BLOCK");
        }

    }

    //ZONAS
    public  Vector2[] getZone(int x, int y, int length, bool isAstar, int aStarType, float maxWeight){

        //null vector
        Vector2 vNull = new Vector2(-100,-100);

        //size: n= (2n+1)^2
        int size = (2*length +1) * (2*length +1);
        Vector2 []cells = new Vector2[size];

        //limits rect
        int jMin = x - length; int jMax = x + length+1;
        int iMin = y - length; int iMax = y + length+1;

        //GD.Print(String.Format("LIMITES ZONA DE X: {0} a {1} DE Y: {2} a {3}",jMin,jMax,iMin,iMax));

        int count = 0;
        for (int i = iMin; i< iMax; i++){
            for (int j = jMin; j < jMax; j++){

                Vector2 vDestino = new Vector2(j,i);
                Vector2 dir = vDestino - new Vector2(x,y);//dir

                cells[count] = vNull;//nulo por defecto

                //distancia manhattan dentro de radio?
                float dirL = Mathf.Abs(dir.x) + Mathf.Abs(dir.y);
                if (dirL <= length){
                    cells[count] = new Vector2(j,i); //ok  
                } 

                //limites de mapa?
                if (!limitsControl(j,i)){
                    cells[count] = vNull;//nulo
                }

                //finalmente
                count++;
            }
        }
        
        //A* filter
        
        count = 0;
        if (isAstar){
            for (int i=0;i<cells.Length;i++){
                
                //salta no validos
                if (cells[i] == vNull){
                    continue;
                }

                //crear ruta y analizar valided de casilla
                createPath(x,y, (int)cells[i].x, (int)cells[i].y, aStarType);
                float pathWeight = totalPathWeight;

                //sin longitud no
                int lenght = path.Length;
                if (lenght<1){
                    cells[i] = vNull;
                    continue;
                } 

                //control de peso
                if (totalPathWeight>maxWeight){
                    cells[i] = vNull;
                    continue;
                }
                count++;
            }
        }
        //GD.Print("zone valid cells: " +count);
        return cells;
    }

    //subzonas
    public Cuadro[,] getSubZone(int x, int y, int radio){

        //limits rect
        int jMin = x - radio; int jMax = x + radio+1;
        int iMin = y - radio; int iMax = y + radio+1;
        
        int size = (2*radio +1);
        Cuadro[,] subCuadros = new Cuadro[size,size]; 
        
        //get
        int sI = 0;
        for (int i = iMin; i< iMax; i++){
            int sJ = 0;

            for (int j = jMin; j < jMax; j++){
                
                //limites de mapa?
                if (limitsControl(j,i)){
                    subCuadros[sJ,sI] = cuadros[j,i];//add
                }else{
                    subCuadros[sJ,sI] = null;
                }
                sJ++;
            }
            sI++;
        }
        return subCuadros;
    }

    public Unit[,] getSubUnits(int x, int y, int radio){
          //limits rect
        int jMin = x - radio; int jMax = x + radio+1;
        int iMin = y - radio; int iMax = y + radio+1;
        
        int size = (2*radio +1);
        Unit[,] subUnits = new Unit[size,size]; 
        
        //get
        int sI = 0;
        for (int i = iMin; i< iMax; i++){
            int sJ = 0;

            for (int j = jMin; j < jMax; j++){
                
                //limites de mapa?
                if (limitsControl(j,i)){
                    subUnits[sJ,sI] = units[j,i];//add
                }else{
                    subUnits[sJ,sI] = null;
                }
                sJ++;
            }
            sI++;
        }
        return subUnits;
     }

    //AGENTS
    public AStar getAstarAgent(int typeAgent){
        AStar myAstar = aStar;
        switch(typeAgent){
            case 0:  myAstar = aStar; break;//heights
            case 1:  myAstar = aStarShip; break;//ships
            case 2:  myAstar = aStarHuman; break;//humans
            case 3:  myAstar = aStarCar; break;//cars
            case 4:  myAstar = aStarHeavy; break;//heavy vehicles
            case 5:  myAstar = aStarAirLow; break;//fly low
            case 6:  myAstar = aStarAir; break;//fly normal
            case 7:  myAstar = aStarVision; break;//vision
        }
        return myAstar;
    }

    //CREATING DIFERENTS NAVIGATION AGENTS
    public void updateWeightsAstars(int x, int y){
        int indexPoint = getIndexNavPoint(x,y);

        if (aStar.HasPoint(indexPoint)) aStar.SetPointWeightScale(indexPoint,getNavWeight(x,y,0));//natural
        if (aStarShip.HasPoint(indexPoint)) aStarShip.SetPointWeightScale(indexPoint,getNavWeight(x,y,1));//ship
        if (aStarHuman.HasPoint(indexPoint)) aStarHuman.SetPointWeightScale(indexPoint,getNavWeight(x,y,2));//human

    }

    public float getNavWeight(int x, int y, int agentType){
        Cuadro cuadro = this.cuadros[x,y];
        float weight = 1;

        if(cuadro == null){
             return int.MaxValue;
        }
        

        //TIPOS
        switch(agentType){
            case 0: //GRAVEDAD(bajar es fácil subir difícil) sobre 100
                if(cuadro.tipo == Cuadro.TIPO.DEEPWATER || cuadro.tipo ==Cuadro.TIPO.WATER){
                    weight = 1;
                }
                if (cuadro.river) weight = 1;
                 switch(cuadro.tipo){
                    case Cuadro.TIPO.GROUND:    weight=2;   break;
                    case Cuadro.TIPO.GRASS:     weight=4;   break;
                    case Cuadro.TIPO.FOREST:    weight=8;   break;
                    case Cuadro.TIPO.HILL:      weight=10;  break;
                    case Cuadro.TIPO.MOUNTAIN:  weight=20;  break;
                    case Cuadro.TIPO.TOP:       weight=40;  break;
                }
             break;
            
            case 1:  //BARCOS//sobre 20
                if(cuadro.tipo != Cuadro.TIPO.DEEPWATER && cuadro.tipo !=Cuadro.TIPO.WATER){
                    weight = int.MaxValue;//solo en agua
                }
                if (cuadro.tipo == Cuadro.TIPO.DEEPWATER)  weight = 5;//4
                if (cuadro.tipo == Cuadro.TIPO.WATER)  weight = 4; //5
                break;

            case 2:  //HUMAN (sobre 20 de distancia)
                if(cuadro.tipo == Cuadro.TIPO.DEEPWATER || cuadro.tipo ==Cuadro.TIPO.WATER){
                    weight = int.MaxValue;//solo tierra
                }
                switch(cuadro.tipo){
                    case Cuadro.TIPO.GROUND:    weight=4;  break;//5
                    case Cuadro.TIPO.GRASS:     weight=5;  break;//4
                    case Cuadro.TIPO.FOREST:    weight=6;  break;//3
                    case Cuadro.TIPO.HILL:      weight=6;  break;//3
                    case Cuadro.TIPO.MOUNTAIN:  weight=10;  break;//2
                    case Cuadro.TIPO.TOP:       weight=10;  break;//2
                }
                if (cuadro.river)weight = 10;//2
                if (cuadro.road) weight = 4;//5
                break;

            case 3:  //CAR sobre 20
                if(cuadro.tipo == Cuadro.TIPO.DEEPWATER 
                || cuadro.tipo ==Cuadro.TIPO.WATER 
                || cuadro.tipo ==Cuadro.TIPO.MOUNTAIN
                || cuadro.tipo ==Cuadro.TIPO.TOP){
                    weight = int.MaxValue;//solo tierra y sin montañas o tops
                }
                switch(cuadro.tipo){
                    case Cuadro.TIPO.GROUND:    weight=3;  break;//6
                    case Cuadro.TIPO.GRASS:     weight=5;  break;//4
                    case Cuadro.TIPO.FOREST:    weight=10;  break;//2
                    case Cuadro.TIPO.HILL:      weight=10;  break;//2
                }
                if (cuadro.river)weight = 10;//2
                if (cuadro.road) weight = 2;//10
                break;

            case 4: //HEAVY
                if(cuadro.tipo == Cuadro.TIPO.DEEPWATER 
                || cuadro.tipo ==Cuadro.TIPO.WATER 
                || cuadro.tipo ==Cuadro.TIPO.MOUNTAIN
                || cuadro.tipo ==Cuadro.TIPO.TOP){
                    weight = int.MaxValue;//solo tierra y sin montañas o tops
                }
                switch(cuadro.tipo){
                    //sobre 20
                    case Cuadro.TIPO.GROUND:    weight=5;  break;//4
                    case Cuadro.TIPO.GRASS:     weight=5;  break;//4
                    case Cuadro.TIPO.FOREST:    weight=10;  break;//2
                    case Cuadro.TIPO.HILL:      weight=10;  break;//2
                }
                if (cuadro.river)weight = weight = int.MaxValue;;//no puede
                if (cuadro.road) weight = 4;//5
                break;

            case 5: //LOW AIR (sin montañas o tops)
                weight = 1;//20
                if (cuadro.tipo == Cuadro.TIPO.MOUNTAIN || 
                    cuadro.tipo == Cuadro.TIPO.TOP) 
                    weight = int.MaxValue;;//no puede
            break;

            case 6: //AIR/INDIRECT MOVEMENT sobre 20
                weight = 1;//20
                break;

            case 7: //Vista: base sobre 20 pero depende del tipo de unidad y el terreno sobre el que está: hill+2 mont:+2 top:+2
                weight = 2;//10
               switch(cuadro.tipo){
                    case Cuadro.TIPO.FOREST:    weight=10;  break;//2
                    case Cuadro.TIPO.MOUNTAIN:  weight= 5;  break;//4
                    case Cuadro.TIPO.TOP:       weight= 5;  break;//4
                }
                break;
        }
        return weight;
    }

    private bool limitsControl(int x, int y){
        bool isOk = true;
        
        isOk &= (x>=0 && x<sizeMap);
        isOk &= (y>=0 && y<sizeMap);
        
        return isOk;
    }

    //UNITS (para mover poner false, true, para crear poner true, true y las misma posicion. para destruir lo mismo pero falses) 
    public void blockUnits(){
        //int count = 0;
        for (int i = 0; i<sizeMap; i++){
            for (int j = 0; j<sizeMap; j++){
                
                Unit unit = units[j,i];
                int id = getIndexNavPoint(j,i);
                bool noUnit = (unit==null);                
                
                if (!noUnit){
                    //GD.Print("Unit at " + j +" " + i );
                    //count++;
                }

                foreach (AStar astar in aStarAll){
                    if (astar ==this.aStar)continue;//este se usa para calculos generales, sin unidades
                    
                    if (astar.HasPoint(id)){
                        astar.SetPointDisabled(id, !noUnit); // si hay unidad -> boquea el punto
                    }
                }
            }
        }
        //GD.Print("Units: " +count);
    }

    public void blockUnitsExcept(Vector2 except){
        blockUnits();//bloquea todo y luego desbloquea la posicion excepcion

        int id = getIndexNavPoint((int)except.x,(int)except.y);
        
        foreach (AStar astar in aStarAll){
            if (astar.HasPoint(id)){
                astar.SetPointDisabled(id, false);//activa solo al individuo
            }
        }
    }

    public void unlockUnits(){

         for (int i = 0; i<sizeMap; i++){
            for (int j = 0; j<sizeMap; j++){
                int id = getIndexNavPoint(j,i);             
                foreach (AStar astar in aStarAll){                    
                    if (astar.HasPoint(id)){
                        astar.SetPointDisabled(id, false);//desbloquea todos
                    }
                }
            }
        }
    }


}

//SIGNAL SEND DATAS
public class PlayerOrder: Node{
    public int playerId, unitX, unitY, destX, destY, orderType;

    public PlayerOrder(int playerId, int unitX, int unitY, int destX, int destY, int orderType){
        this.playerId = playerId;
        this.unitX = unitX;
        this.unitY = unitY;
        this.destX = destX;
        this.destY = destY;
        this.orderType = orderType;
    }

    

}