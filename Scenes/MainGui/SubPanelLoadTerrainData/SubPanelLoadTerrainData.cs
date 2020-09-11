using Godot;
using System;
using System.Collections.Generic;

public class SubPanelLoadTerrainData : Panel{

    //view refs
    VBoxContainer list;
    Label lblSelectedPath;

    private const string basePath ="user://terrains";

    //signals
    [Signal] public delegate void loadTerrain(string fileName);

    public override void _Ready() {
        list = GetNode("panel/vbc/vbc/Panel/ScrollContainer/list") as VBoxContainer;
        lblSelectedPath = GetNode("panel/vbc/vbc/lblSelectedPath") as Label;
        lblSelectedPath.Text= "";

        poblateList();
    }

    public void poblateList(){
        //borra
        foreach(Node obj in list.GetChildren()){
            obj.QueueFree();
        }

        //read directory terrain
        Directory dir = new Directory();
        Error err = dir.Open(basePath);
        if (err!= Error.Ok) return;

        dir.ListDirBegin();
        string fileName;
        List<string> fileNames = new List<string>();

        while(true){
            fileName = dir.GetNext();
            if (fileName == "") break;//fin
            if (fileName.BeginsWith("."))continue;//subdirectorios no
            fileNames.Add(fileName);
        }

        //poblate list with buttons
        foreach(string fn in fileNames){
            Button buFile = new Button();
            buFile.Text=(fn);
            buFile.RectMinSize = new Vector2(0,30f);
            Godot.Collections.Array data = new Godot.Collections.Array();
            data.Add(fn);
            buFile.Connect("pressed",this,"onCLickFileButton",data);
            list.AddChild(buFile);
        }

    }
    
    //onCLICK
    public void onCLickFileButton(string name){
        lblSelectedPath.Text=(name);
    }

    public void onClickDelete(){
        string fileName = lblSelectedPath.Text;
        lblSelectedPath.Text=("");
        if (fileName == "")return;

        Directory dir = new Directory();
        Error err = dir.Remove(basePath+"//"+fileName);
        if (err == Error.Ok) poblateList();
    }

    public void onClickCancel(){
        Visible = (false);
        EmitSignal("loadTerrain",""); //nada
    }

    public void onClickOk(){
        string fileName = lblSelectedPath.Text;
        if (fileName == "")return;
        Visible = (false);
        EmitSignal("loadTerrain",fileName); //coge los datos
    }

}
