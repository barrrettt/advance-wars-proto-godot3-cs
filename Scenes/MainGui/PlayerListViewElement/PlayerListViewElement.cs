using Godot;
using System;

public class PlayerListViewElement : ColorRect{

    private Label lblNamePlayer;
    private Label lblPlayerType;
    private Label lblDetailPlayer;
    private ColorRect crIsActual;

    public bool isActual = false;
    private Color defaultColor;
    private float time;

    public override void _Ready() {
        lblNamePlayer = GetNode("hbc/vbc/lblPlayerName") as Label;
        lblPlayerType = GetNode("hbc/vbc/lblPlayerType") as Label;
        lblDetailPlayer = GetNode("hbc/vbc/lblDetailPlayer") as Label;
        crIsActual = GetNode("crIsActual") as ColorRect;
        defaultColor = crIsActual.Color;
    }

    public override void _Process(float delta){
        time +=delta;
        
        crIsActual.Visible = isActual;
        if(isActual){
            float value = (Mathf.Cos(time*10)*0.2f)+0.5f;
            crIsActual.Color = new Color(value,defaultColor.g,defaultColor.b,defaultColor.a);
        }else{
            crIsActual.Color = defaultColor;
        }

        
    }

    public void setData(string dataPlayer){
        string[] data = dataPlayer.Split(";");
        lblNamePlayer.Text=("PLAYED ID: "+ data[0]);
        lblPlayerType.Text=("TYPE: " + (Player.TYPEPLAYER)int.Parse(data[1]) );
        lblDetailPlayer.Text=(String.Format("I:{0}(+{1}) U:{2}",data[2],data[3],data[4] ));
    }

}
