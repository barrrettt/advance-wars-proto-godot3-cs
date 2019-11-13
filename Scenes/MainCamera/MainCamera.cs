using Godot;
using System;

public class MainCamera : Node2D{

    //ESTATE CONTROLER
    private enum TIPOCAMERA{NOCONTROL,CONTROLPC, JOYPAD}
    [Export] private TIPOCAMERA tipoCamera = TIPOCAMERA.CONTROLPC;
    private Vector2 posCameraFollow;//cinatics: other player movement or similar
    private Vector2 velocity; 
    [Export]public float Speed = 200f;
    [Export]public float maxSpeed = 600f;

    [Export]public float minZoom = 0.50f;
    [Export]public float maxZoom = 0.75f;
    float zoom;
    //referencias
    private Camera2D camera;

    //medidas control de posicion
    Vector2 screenSize;
    Vector2 mapLimitPoint = new Vector2(200,200); //de (0,0) a (100,100) por ejemplo
    
    int widthMod, heightMod;

    //NOTIFICAR CAMBIO DE POSICION DE CAMARA PARA OPTIMIZAR LA VISTA
    private Vector2 lastPositionCamera;
    [Signal] public delegate void cameraMoved(Vector2 cameraPosition);

    //██████████████████████████████████████
    public override void _Ready() {
        camera = GetNode<Camera2D>("Camera2D");
        camera.MakeCurrent();
        camera.SetZoom( Vector2.One * maxZoom);//max
        zoom =  maxZoom;

        //observacion para cambion de resolucion
        GetTree().Connect("screen_resized", this, "screenChange");
        screenChange();

        //limite camera default
        mapResised(100,null,null);

        //joy connections
         GD.Print("JOYs: ");
        foreach(var joy in Input.GetConnectedJoypads()){
            GD.Print("-"+joy.ToString());
        }
    }

    public void screenChange(){
        Position = camera.GetCameraScreenCenter();
        screenSize = GetViewportRect().Size;
        widthMod = (int)  (screenSize.x/2 * zoom);
        heightMod = (int) (screenSize.y/2 * zoom);
    }

    //callback on createMap for change camera limits
    public void mapResised(int size,int[] terrains, int[] playerUnits){
        mapLimitPoint = new Vector2(size * 32, size * 32);
        camera.LimitRight = size * 32;
        camera.LimitBottom = size * 32;
    }

    //DIBUJAR DEBUG
    public override void _Draw(){
        Vector2 pos = new Vector2();
        float pointSize = 2.0f;
        Color colorPoint = new Color(1.0f,0,0,1.0f);

        //camera center (0,0)
        DrawCircle (pos ,pointSize,colorPoint);

        //esquinas
        pos = new Vector2(-widthMod,-heightMod);
        DrawCircle (pos,pointSize,colorPoint);//UpL

         pos = new Vector2(widthMod,-heightMod);
        DrawCircle (pos,pointSize,colorPoint);//UpR

        pos = new Vector2(widthMod,heightMod);
        DrawCircle (pos,pointSize,colorPoint);//DR

        pos = new Vector2(-widthMod,heightMod);
        DrawCircle (pos,pointSize,colorPoint);//DL

        

    }

    //CONTROL CAMERA
    public override void _Process(float delta){
        //inputs
        velocity = new Vector2();
        Speed = 200;

        Vector2 mousePos = GetGlobalMousePosition();

        //mouse central click move
        if (Input.IsMouseButtonPressed(3)){
            velocity = (mousePos - Position);
            if (velocity.LengthSquared()<100f) {
                velocity = Vector2.Zero;//cerca no mueve
            }
            Speed = maxSpeed;
        }

        //joypad (usar un imput aparte para joypad)
        if (tipoCamera == TIPOCAMERA.JOYPAD){
            if (Input.GetConnectedJoypads().Count > 0){
                velocity.x = Input.GetJoyAxis(0,0);//x
                velocity.y = Input.GetJoyAxis(0,1);//y
                if (velocity.Length()< 0.2f) velocity = Vector2.Zero; //death zone
            }
        }

        //keyboard-movement
        if (Input.IsActionPressed("ui_right")) velocity.x += 1;
        if (Input.IsActionPressed("ui_left")) velocity.x -= 1;
        if (Input.IsActionPressed("ui_down")) velocity.y += 1;
        if (Input.IsActionPressed("ui_up")) velocity.y -= 1;
        if (Input.IsActionPressed("ui_alt")) Speed = maxSpeed;

        //velocidad final normalizada y delta
        velocity = velocity.Normalized() * delta * Speed;

        //Aplica movimiento
        Vector2 finalPos = Vector2.Zero;
        if (tipoCamera == TIPOCAMERA.NOCONTROL){
            finalPos = posCameraFollow;//instantaneo

        }else if (tipoCamera == TIPOCAMERA.CONTROLPC){
            finalPos = Position + velocity ;//progresivo
        }

        //controla limites con camera
        Vector2 camPos = camera.GetCameraScreenCenter();
        Vector2 dirCam = finalPos - camPos; 
        float margen = 100f;
        if (Mathf.Abs(dirCam.x) > margen) finalPos.x = Position.x;
        if (Mathf.Abs(dirCam.y) > margen) finalPos.y = Position.y;
        Position = finalPos;

        //señar de cambio de posicion
        if (Position != lastPositionCamera){
            lastPositionCamera = Position;
            EmitSignal("cameraMoved",Position);
        }
    }

    //zoom camera
    public override void _UnhandledInput(InputEvent @event){
        if (tipoCamera == TIPOCAMERA.NOCONTROL) return;

        const float velZ = 0.025f; 

        if (@event is InputEventMouseButton){
            InputEventMouseButton emb = (InputEventMouseButton)@event;
            if (emb.IsPressed()){
                if (emb.ButtonIndex == (int)ButtonList.WheelUp){
                    if (zoom < maxZoom){ zoom += velZ;}
                }
                if (emb.ButtonIndex == (int)ButtonList.WheelDown){
                    if (zoom > minZoom){ zoom -= velZ; }
                }
                camera.Zoom = Vector2.One * zoom;
                screenChange(); //control de limite de camara
            }
        }
    } 
    
    //CALL NO MOVE CAMERA
    public void disableInput(bool isDisable){
        
        if (isDisable){
            tipoCamera = TIPOCAMERA.NOCONTROL;
        }else{
            tipoCamera = TIPOCAMERA.CONTROLPC;
        }
    }

}
