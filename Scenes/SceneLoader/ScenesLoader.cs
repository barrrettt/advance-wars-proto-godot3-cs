using Godot;
using System;

public class ScenesLoader : ColorRect{
    
    public float time;
    //LOADER
    ResourceInteractiveLoader riLoader;
    int waitFrames = 100;
    float timeMaxLoad = 100f;

    //REF VIEW
    private  AnimationPlayer anim;
    private ProgressBar pBarr;

    public override void _Ready() {
        anim = GetNode("animation") as AnimationPlayer;
        pBarr = GetNode("pbarr") as ProgressBar;
    }

       //LOAD SCENES
    public void loadScene(string path){
        if (riLoader != null) return;

        //cache?
        if (ResourceLoader.HasCached(path)){
                PackedScene packedScene = (PackedScene) ResourceLoader.Load(path,"",true);
                instanceScene(packedScene);
                return;
        }else{
            //no cache
            riLoader = ResourceLoader.LoadInteractive(path);
            if (riLoader == null) GD.Print("Error resourceloader");
            SetProcess(true);
        }
        

        //view
        this.Visible = true;
        this.pBarr.Visible = true;        
        anim.Play("fade");
        GD.Print("Loading " + path);
    }

    public override void _Process(float delta) {

        time += delta; //GAMETIME

        //LOADER
        if (riLoader == null){
            SetProcess(false);
            return;
        }

        if (waitFrames>0){
            waitFrames--;
            return;//espera antes de cargar
        }

        //tiempo máximo de bloqueo de hilo
        float initTime = OS.GetTicksMsec();
        while(OS.GetTicksMsec()< initTime+timeMaxLoad){

            Error err = riLoader.Poll();//progresando...

            if(err == Error.FileEof){
                //█OOOKKK
                PackedScene packedScene = (PackedScene)riLoader.GetResource();
                riLoader = null;
                instanceScene(packedScene);
                break;

            }else if(err == Error.Ok){
                updateLoadingProgress();//informa

            }else{ 
                GD.Print("ERROR");//ERROR
                riLoader = null;
                break;
            }
        }
    }

    private void updateLoadingProgress(){
        float progress = (float)(riLoader.GetStage()) / riLoader.GetStageCount();
        GD.Print("progress "+progress);
        pBarr.Value=(progress);
    }

    private void instanceScene(PackedScene packedScene){
        Node mainG = GetTree().Root.GetNode("MainGame");
        Node instance = packedScene.Instance();
        mainG.AddChild(instance);//cuelgo la instancia del nodo mainGame
        mainG.GetChild(0).QueueFree();//borro al hijo 1
        packedScene = null;
    }
    
}
