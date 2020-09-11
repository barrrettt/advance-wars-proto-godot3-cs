using Godot;
using System;
using System.Collections.Generic;

public class Unit : Node2D{
    //A quien pertenece
    [Export]public string nameUnit;

    [Export] public int owerPlayer;

    //movimiento
    public int wpX;
    public int wpY;
    [Export] public int navAgentType;//soldado.. coche avion,...
    [Export]public int maxMovePoints;//max (20 es lo normal)
    public int movePoints;

    //vision
    [Export]public int visionMaxima = 15;
    //disparo
    [Export]public int maxAtackRange = 3;//sin pesos
    [Export]public int alcanzeMinimoDisparo = 0;
    [Export]public bool canRunAndFire = false;
    public bool hasAtack = true;//1 fire/turn
    public int ammo = 10;

    //defensa
    //..

    //vida
    [Export] public int hpMax = 100;
    public int hp;

    //municion, comida, combustible...

    //PROPIEDAD
    private Sprite spPropietarioIndicator;

    //TEXT HIT AND HPBARR
    private Label lblHitPoints;
    private Control hpBarr;
    private ColorRect crLevelHp;
    private float crMaxSize;

    //ANIMACION MOVIMIENTO
    private AnimationPlayer animation;

    private bool isMoving = false;
    private float animMoveSpeed = 200f;

    private Vector2[] wayPoints;
    private int actualMovePoint = 0;

    //ANIMACION SCALA Y FLIP
    private Node2D baseNode;
    private Vector2 initScale = Vector2.One;
    private bool flipH = false;

    //ZONAS Y PATH
    private TileMap tmZone;
    private bool paintMousePath = false;
    private Line2D line2D;
    private Vector2[] firezone = new Vector2[0];

    //MODELO DE NAVEGACION LOCAL
    private Navigation subNav;

    [Signal] public delegate void moveCell(); //movimientos ejecutados
    [Signal] public delegate void ordersDone(); //Orden completada ejecutados


    ////////////////////////////////////////////  METODOS
    public override void _Ready(){

        spPropietarioIndicator = GetNode("spColorPlayer") as Sprite;
        tmZone = GetNode("TmZone") as TileMap;
        line2D = GetNode("Line2D") as Line2D;
        baseNode = GetNode("base") as Node2D;
        animation = GetNode("base/AnimationPlayer") as AnimationPlayer;
        lblHitPoints = GetNode("lblHitPoints") as Label;
        hpBarr = GetNode("BarHp") as Control;
        crLevelHp = GetNode("BarHp/crLevel") as ColorRect;
        crMaxSize = crLevelHp.RectSize.x;

        //init idle
        animation.CurrentAnimation = "Idle";
        animation.PlaybackSpeed = (1.9f + (float)GD.RandRange(0.0f,0.2f));

        //anim scale
        initScale = baseNode.Scale;
        
        //line
        line2D.Visible = true;
        line2D.ClearPoints();

        //Color player actual

        //ready 
        movePoints = maxMovePoints;
        hp = hpMax;

    }

    public override void _Process(float delta){
        Vector2 mousePos = GetGlobalMousePosition();

        //efecto de aumento
        float dist2 = ( mousePos- Position).LengthSquared();
        if(dist2<300f){
            baseNode.Scale = (initScale * 1.2f);
        }else{
            baseNode.Scale = initScale;
        }

        //flipH
        if (flipH){
            Vector2 scale = baseNode.Scale;
            Vector2 vFlip = (new Vector2(scale.x *-1f ,scale.y));
            baseNode.Scale = vFlip;
        }

        //walk animation efect
        if (isMoving){
           movingEffect(delta);            
        }

        if (paintMousePath){
            paintLine();
        }
        
    }

    private void movingEffect(float delta){
        Vector2 dir =  wayPoints[actualMovePoint] - Position;
        Vector2 move =  dir.Normalized() * delta * animMoveSpeed; //normalize & delta
        Position += move;//move 

        flipH = (move.x < 0);

        if(dir.LengthSquared() < (tmZone.CellSize.x/2)){
            
            actualMovePoint++;
            EmitSignal("moveCell");//send move partial

            //finaly?
            if (actualMovePoint>=wayPoints.Length){
                Position = wayPoints[wayPoints.Length-1];
                isMoving = false;
                actualMovePoint = 0;
                animationFinish();
            }

        }
    }

    //para mostrar areas de fuego
    public override void _Draw(){
        Vector2 vNull =  new Vector2(-100,-100);
        Color color = new Color(1,0,0, 1.0f);
        float radio = 16f;
        //coords 
        if (subNav == null)return;
        
        //to center relative
        int center = subNav.cuadros.GetLength(0)/2;
        float cellHalf = tmZone.CellSize.x/2;
        Vector2 vCenter = new Vector2(center ,center);
        Vector2 cOffset =  (vCenter*-1) * tmZone.CellSize.x;

        foreach(Vector2 relatPoint in firezone){
            if (relatPoint == vNull) continue;//no null
            if (relatPoint == Vector2.Zero) continue;//no center //vCenter

            Vector2 fPoint = relatPoint * tmZone.CellSize; //cOffset +
            drawCross(fPoint,radio,color);
        }
    }

    private void drawCross(Vector2 center, float size, Color color){
        float mSize = (size/2); float oneSize = size/9;
        Vector2 pUL = new Vector2(center.x- mSize,center.y- mSize);
        Vector2 pUR = new Vector2(center.x+ mSize,center.y- mSize);
        Vector2 pDL = new Vector2(center.x- mSize,center.y+ mSize);
        Vector2 pDR = new Vector2(center.x+ mSize,center.y+ mSize);
        
        DrawRect(new Rect2(pUL,oneSize*3,oneSize),color);//ULx
        DrawRect(new Rect2(pUL,oneSize,oneSize*3),color);//ULy

        DrawRect(new Rect2(pUR,oneSize*-3,oneSize),color);//URx
        DrawRect(new Rect2(pUR,oneSize*-1,oneSize*3),color);//URy

        DrawRect(new Rect2(pDR,oneSize*-3,oneSize*-1),color);//DRx
        DrawRect(new Rect2(pDR,oneSize*-1,oneSize*-3),color);//DRy

        DrawRect(new Rect2(pDL,oneSize*3,oneSize*-1),color);//DRx
        DrawRect(new Rect2(pDL,oneSize*1,oneSize*-3),color);//DRy

        //Rect2 rect = new Rect2(center.x-(size/2),center.y-(size/2),size,size);
        //DrawRect(rect,color);
    }


    //UPDATE SUB NAVIGATION
    public void updateSubNav (Cuadro[,] subzone, Unit[,] subUnits){
        subNav = new Navigation();
        subNav.createNavigationSystem(subzone,subUnits);
    }
    
    //SELECCIONAR UNIDAD
    public void selectMe(int playerId, bool isLocal){

        int zoneType = 0;//only move points
        
        //soy del player actual o no?
        if (playerId == owerPlayer && isLocal){
            paintMousePath = true;//show pointer path
        }else{
            zoneType = 1;//max move
        }
        
        //paintDisableNavPoints();//debug
        
        //zona de vision:
        //Vector2[]relativePoints =  getZone(2);
        //paintZone(relativePoints,false);//verd

        //zona de tiro:
        if (hasAtack) paintFireZone(getZone(3));

        //zona de movimiento en verde
        Vector2[]relativePoints =  getZone(zoneType);
        paintZone(relativePoints,false);
    }

    public void unSelectMe(){
        paintMousePath = false;
        clearZones();
    }

    //VALIDATION ACTIONS
    public bool isValidMove(int destX, int destY){
        int relX = destX - wpX;
        int relY = destY - wpY;

        int center = subNav.cuadros.GetLength(0)/2;
        
        subNav.blockUnitsExcept(new Vector2(center,center));//bloquea otras unidades

        subNav.createPath(
            center, center,//s
            relX+center, relY + center, //f
            navAgentType);

        subNav.createActualPath();//exec

        if ( subNav.path.Length<2)return false;//no nulo
        if (subNav.totalPathWeight > movePoints) return false;//no tiene puntos de movimiento
        return true;
    }

    public bool isValidAttack(int initX, int initY, int destX, int destY){
        int relX = destX - initX;
        int relY = destY - initY;

        int center = subNav.cuadros.GetLength(0)/2;
        
        subNav.unlockUnits();//desbloquea todos

        subNav.createPath(
            center, center,//s
            relX+center, relY + center, //f
            6); //ataque aire: peso 1

        subNav.createActualPath();//exec

        if ( subNav.path.Length<2)return false;//no nulo
        if (subNav.totalPathWeight > maxAtackRange) return false;//no alcanza
        if (!hasAtack) return false;
        if (ammo<1)return false;
        return true;

    }

    //GET ZONES
    public Vector2[] getZone(int typeZone){
        Vector2 vNull = new Vector2(-100,-100);
        int center = subNav.cuadros.GetLength(0)/2;// cuanto mas pequeño -> mejor tiempo de búsqueda.
        int radio = center/2;
        
        //configure A*
        int aStarType = navAgentType; 
        int points = movePoints;
        bool isAstarActive = true;

        switch (typeZone){
            case 0: // movePoints
                subNav.blockUnitsExcept(new Vector2(center,center));//unidades bloquean excepto yo
                //subNavigation.blockUnits();//unidades bloquean navegacion
                //subNavigation.unlockUnits();//unidades no bloquean
            break;
            case 1: //zona de movimiento maximo
                points = maxMovePoints; 
                subNav.blockUnitsExcept(new Vector2(center,center));//unidades bloquean excepto yo
            break; 

            case 2: //zona de vision
                aStarType=7; 
                points = visionMaxima; 
                subNav.unlockUnits();
            break; 
            case 3: //zona de ataque directo
                aStarType=6; 
                points = maxAtackRange; 
                subNav.unlockUnits();//hacer un subconjunto de bloquear aliados y propios
            break; 
        }

        //crea zona con A*
        Vector2[] validPoints = subNav.getZone(
            center,center,//posicion
            radio,//radio
            isAstarActive,aStarType,points);//pesos A*

        //to relative
        Vector2[] relativePoints = new Vector2[validPoints.Length]; 

        for (int i = 0;i<validPoints.Length;i++){
            Vector2 point = validPoints[i];
            if (point == vNull)continue;
            relativePoints[i] = new Vector2(point.x - center, point.y - center);
        }
        
        return relativePoints;
    }

    //IA MOVES & ATACK (move to max range atack)
    public Vector2 atackMove(int targetX, int targetY){
        if (subNav.getAstarAgent(0) == null) GD.Print("ERROR NO SUBNAV CREATED"); 
        
        Vector2 vNull = new Vector2(-100,-100);//null;
        Vector2[] moveZone = getZone(0);
        List<Vector2> atackPoints = new List<Vector2>();

        //moves to worldCoords and get atackpoints
        for (int i = 0;i<moveZone.Length;i++){

            if (moveZone[i] == vNull){
                continue;
            }

            moveZone[i].x += wpX;
            moveZone[i].y += wpY;

            if(isValidAttack((int)moveZone[i].x,(int)moveZone[i].y,targetX,targetY)){
                atackPoints.Add(new Vector2((int)moveZone[i].x,(int)moveZone[i].y));
            }
        }
        
        Vector2[] potencialPoints;
        //hay puntos de ataque?
        if(atackPoints.Count>1){
            potencialPoints = atackPoints.ToArray();
        }else{
            potencialPoints = moveZone;
        }

        //el mas cercano de los puntos potenciales
        Vector2 closed = new Vector2(-100,-100);//null
        float minDist2 = float.MaxValue;

        foreach(Vector2 point in potencialPoints){
            if (point == vNull)continue;

            float dirX = targetX - point.x;
            float dirY = targetY - point.y;
            float dist2 = (dirX*dirX) + (dirY*dirY);
            
            if (dist2<minDist2){
                minDist2 = dist2;
                closed = point;
            }
        }
        
        return closed;
    }

    //PAINT ZONES
    private void paintDisableNavPoints(){
        int maxRadioZone = subNav.sizeMap/2;// 

        for (int i = 0; i<subNav.sizeMap; i++){
            for (int j = 0; j<subNav.sizeMap; j++){
               
                int cellIndex = -1;

                int id = subNav.getIndexNavPoint(j,i);
                AStar astar = subNav.getAstarAgent(navAgentType);
                if (astar.HasPoint(id)){
                     if (astar.IsPointDisabled(id)){
                        cellIndex = 48;//unidad con bloqueo
                    }else{
                        cellIndex = 49;//unidad navegable
                    }
                }
               
                int x = j-maxRadioZone;
                int y = i-maxRadioZone;
                tmZone.SetCell(x,y,cellIndex);//set tilemap
            }
        }

    }

    private void paintFireZone(Vector2[] relativePoints){
        firezone = relativePoints;
        Update();
    }

    private void paintZone(Vector2[] relativePoints,bool isRed){
        int maxRadioZone = subNav.sizeMap/2;// 
         //paint
        Vector2 vNull = new Vector2(-100,-100);

        foreach(Vector2 relPos in relativePoints){
            int cellIndex = -1;
            if (relPos != vNull){
                cellIndex = isRed?48:49;//rojo o verde
            } 
            int x = (int)relPos.x;//-maxRadioZone;
            int y = (int)relPos.y;//-maxRadioZone;
            tmZone.SetCell(x,y,cellIndex);//set tilemap
        }
    }
    
    public void clearZones(){
        if (line2D!=null)line2D.ClearPoints();
        if (tmZone!= null) tmZone.Clear();
        if (firezone != null){
            firezone = new Vector2[0];
            Update();
        }
    }

    //PAINT LINE
    public void paintLine(){
        //coords 
        int center = subNav.cuadros.GetLength(0)/2;
        float cellHalf = tmZone.CellSize.x/2;
        Vector2 centerOffSet =  new Vector2(-center ,-center) * tmZone.CellSize.x;

        Vector2 mworld = GetGlobalMousePosition()- Position + new Vector2(cellHalf,cellHalf);
        Vector2 mouseCellPos =  tmZone.WorldToMap(mworld);
        
        subNav.createPath(
            center, center,//s
            (int)mouseCellPos.x+center, (int)mouseCellPos.y+center, //f
            navAgentType);

        subNav.createActualPath();//exec

         //init paint
        Vector3[] path =  subNav.path;
        if ( path.Length<2)return;//no nulo
        if (subNav.totalPathWeight>movePoints) return;//no tiene puntos de movimiento
        
        line2D.ClearPoints();

        //next
        Vector2 lastPw = centerOffSet + (new Vector2(path[0].x,path[0].y) * tmZone.CellSize);

        foreach (Vector3 pw in path){
            Vector2 pw2 =  centerOffSet + (new Vector2(pw.x,pw.y) * tmZone.CellSize);
            line2D.AddPoint(lastPw);
            lastPw = pw2;
        }

        //finaly
        if( path.Length>0){
            Vector2 pwFinal = new Vector2(path[path.Length-1].x, path[ path.Length-1].y );
            lastPw =  centerOffSet + (new Vector2(pwFinal.x,pwFinal.y) * tmZone.CellSize);
            line2D.AddPoint(lastPw);
        }

    }

    //MOVE ANIMATIONS, DATA & VIEW
    public void moveOrder( int destX, int destY){
        int relX = destX - wpX;
        int relY = destY - wpY;

        int center = subNav.cuadros.GetLength(0)/2;

        subNav.createPath(
            center, center,//s
            relX + center, relY + center, //f
            navAgentType);

        subNav.createActualPath();//exec

        if (subNav.path.Length<2)return;//no nulo
        if (subNav.totalPathWeight>movePoints) return;//no tiene puntos de movimiento

        this.wayPoints = new Vector2[subNav.path.Length];

        for(int i=0;i<subNav.path.Length;i++){
            Vector2 v2 = new Vector2(subNav.path[i].x - center, subNav.path[i].y - center);
            wayPoints[i] =  Position + ( v2 * tmZone.CellSize);
        }

        //update final world position
        wpX = wpX+relX;
        wpY = wpY+relY;

         //move points update 
        movePoints -= (int)subNav.totalPathWeight;

        //init animation
        isMoving = true;
        animation.Play("Walk");
    }
   
    public void newTurn(int propietario){

        bool isMine = this.owerPlayer == propietario;
        spPropietarioIndicator.Visible=(isMine);//indicador de propiedad
        
        //restablece puntos de movimieto y fuego
        if (isMine){
            movePoints = maxMovePoints;
            hasAtack = true;
        }

    }

    //ATACK/HIT ANIMATIONS, DATA & VIEW
    public int execAttack(Vector2 dir){
        if(!hasAtack)return 0;

        //consume
        if (ammo>0){
            ammo--;
        }else{
            return 0;
        }

        flipH = (dir.x<0);

        animation.Play("Attack");
        
        //data
        hasAtack = false;
        if (!canRunAndFire) movePoints = 0;

        return 20;//atack value
    }
    public void execHit(Vector2 dir,int hitPoints, int defense){
        GD.Print("Unit. hp loss:"+hitPoints);
        hp -= hitPoints;

        flipH = (dir.x<0);

        animation.Play("Hit");
        
        //hit text
        lblHitPoints.Visible=(true);
        lblHitPoints.Text=("-"+hitPoints);

        //hp Barr
        float value = ((float)hp)/hpMax;
        value = Mathf.Clamp(value,0, 1f);
        hpBarr.Visible=(value < 1f);
        crLevelHp.SetSize(new Vector2(value*crMaxSize,crLevelHp.RectSize.y));
    }
    public void animationHitFinish(){
        lblHitPoints.Visible=(false);
        
        if (hp>0){
            animation.Play("Idle"); 

        }else{
            //die animation here
            QueueFree();//die
        }
        
    }

    //FINISH call back
    public void animationFinish() {
        animation.Play("Idle"); 
        EmitSignal("ordersDone");//send end actions 
    }

}
