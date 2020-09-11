using Godot;
using System;

public class MainMenu : CanvasLayer{


    public override void _Ready() {

    }

    //ON CLICK
    public void onCLickBuNew(){
        //desde codigo para evitar ciclos
        ScenesLoader scenesLoader = GD.Load<PackedScene>("res://Scenes/SceneLoader/ScenesLoader.tscn").Instance() as ScenesLoader;
        AddChild(scenesLoader);
        scenesLoader.loadScene("res://Scenes/Game.tscn");//goto game
    }

    public void onCLickBuEditor(){
        ScenesLoader scenesLoader = GD.Load<PackedScene>("res://Scenes/SceneLoader/ScenesLoader.tscn").Instance() as ScenesLoader;
        AddChild(scenesLoader);
        scenesLoader.loadScene("res://Scenes/Editor/SceneEditor.tscn");//goto editor
    }

    public void onClickBuExit(){
        GetTree().Quit();//EXIT
    }

}
