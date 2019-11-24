using Godot;
using System;

public class MainGame : Node2D{
    
    public float time;

    ///////////////////////////////////////  METODOS
    public override void _Ready() {

        GD.Print("MAIN GAME INIT");
        
        //mouse confined?
        //Input.SetMouseMode(Input.MouseMode.Confined);

        //maxi?
        //OS.SetWindowFullscreen(true);
        OS.SetWindowMaximized(true);

        //load first scene directly
        PackedScene packedScene = (PackedScene) GD.Load("res://Scenes/MenuMain/MainMenu.tscn");//main menu
        //PackedScene packedScene = (PackedScene) GD.Load("res://Scenes/Game.tscn");//game
        AddChild(packedScene.Instance());
    }
    

}
