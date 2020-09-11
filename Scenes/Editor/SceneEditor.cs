using Godot;
using System;

public class SceneEditor : Node2D{

    private MainGui gui;
    
    public override void _Ready() {
        gui = GetNode("MainGui") as MainGui;
        gui.setGameStyle(0);
    }


}
