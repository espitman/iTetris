using UnityEngine;
using UnityEngine.UI;
using iTetris.Core;

namespace iTetris
{
    public sealed partial class GameController
    {
        readonly Image[] mobilePreviewCells=new Image[16];
        void BuildPortraitInterface(Transform root)
        {
            // Clear the whole display even though the board camera occupies only its central region.
            var background=new GameObject("Mobile background camera").AddComponent<Camera>();background.depth=-10;
            background.cullingMask=0;background.clearFlags=CameraClearFlags.SolidColor;background.backgroundColor=new Color(.015f,.035f,.055f);
            Label(root,"i T e t r i s",new Vector2(0,907),new Vector2(450,65),42,Pale);
            ButtonAt(root,"II",new Vector2(370,907),new Vector2(125,78),()=>TogglePause());
            var sound=ButtonAt(root,"",new Vector2(-350,907),new Vector2(160,78),()=>{audioWorld.Toggle();UpdateSound();});soundText=sound.GetComponentInChildren<Text>();
            scoreText=Label(root,"000000",new Vector2(-245,828),new Vector2(350,60),42,Pale);
            levelText=Label(root,"01",new Vector2(175,828),new Vector2(130,60),38,Pale);
            Label(root,"SCORE",new Vector2(-245,870),new Vector2(300,32),23,Muted);
            Label(root,"LEVEL",new Vector2(175,870),new Vector2(150,32),23,Muted);
            linesText=Label(root,"LINES 000",new Vector2(335,828),new Vector2(200,45),23,Muted);
            bestText=Label(root,"",new Vector2(-250,778),new Vector2(350,35),22,Muted);
            modeText=Label(root,"MARATHON",new Vector2(250,778),new Vector2(300,35),22,Teal);
            holdText=Label(root,"HOLD",new Vector2(-310,740),new Vector2(160,32),22,Pale);
            Label(root,"NEXT",new Vector2(100,740),new Vector2(440,32),22,Pale);
            for(int group=0;group<4;group++)
            {
                var preview=Panel(root,new Vector2(group==3?-310:-90+group*195,684),new Vector2(170,75),new Color(.035f,.08f,.11f,.8f));
                for(int cell=0;cell<4;cell++)
                {
                    var tile=UIObject("Preview crystal",preview,Vector2.zero,Vector2.one*24);
                    var im=tile.gameObject.AddComponent<Image>();im.sprite=RoundedSprite();im.raycastTarget=false;
                    var rim=tile.gameObject.AddComponent<Outline>();rim.effectDistance=Vector2.one*2;rim.effectColor=Pale;
                    mobilePreviewCells[group*4+cell]=im;
                }
            }
            toastText=Label(root,"",new Vector2(0,-592),new Vector2(800,48),24,Teal);
            ButtonAt(root,"HELP",new Vector2(380,-600),new Vector2(120,55),()=>ToggleHelp());
            overlay=UIObject("Portrait menu",root,Vector2.zero,new Vector2(770,780));
            var imenu=overlay.gameObject.AddComponent<Image>();imenu.sprite=RoundedSprite();imenu.type=Image.Type.Sliced;imenu.color=new Color(.018f,.04f,.07f,.98f);
            overlayTitle=Label(overlay,"",new Vector2(0,260),new Vector2(700,100),56,Pale);
            overlaySubtitle=Label(overlay,"",new Vector2(0,155),new Vector2(700,100),28,Muted);
            primaryButton=ButtonAt(overlay,"PLAY",new Vector2(0,30),new Vector2(550,100),()=>PrimaryAction(),true);
            secondaryButton=ButtonAt(overlay,"NEW GAME",new Vector2(0,-100),new Vector2(550,100),()=>StartGame());
            ButtonAt(overlay,"MARATHON / ZEN",new Vector2(0,-225),new Vector2(550,90),()=>{zen=!zen;modeText.text=zen?"ZEN":"MARATHON";RefreshMenuMode();});
            ButtonAt(overlay,"GAME HUB",new Vector2(0,-335),new Vector2(550,80),()=>OpenHub());
            helpPanel=Panel(root,Vector2.zero,new Vector2(800,1100),new Color(.015f,.035f,.06f,.99f)).gameObject;
            Label(helpPanel.transform,"HOW TO PLAY",new Vector2(0,440),new Vector2(740,90),46,Pale);
            Label(helpPanel.transform,"Fill rows to clear them. Keep the stack low.\n\n← / →  Hold to move\n↶ / ↷  Rotate\n↓  Hold for soft drop\nDROP  Instantly drop the piece\nHOLD  Save or swap the current piece\n\nMarathon gets faster every 10 lines.\nZen lets you place pieces at your own pace.\n\nTap II or Android Back to pause.\nYour best score saves on this device.",new Vector2(0,0),new Vector2(740,750),30,Pale);
            ButtonAt(helpPanel.transform,"GOT IT",new Vector2(0,-440),new Vector2(550,100),()=>ToggleHelp(),true);helpPanel.SetActive(false);
        }
        void DrawMobilePreviews(PieceKind[] next)
        {
            for(int i=0;i<3;i++)DrawMobilePreview(next[i],i);
            for(int i=12;i<16;i++)mobilePreviewCells[i].gameObject.SetActive(game.Held.HasValue);
            if(game.Held.HasValue)DrawMobilePreview(game.Held.Value,3);
        }
        void DrawMobilePreview(PieceKind kind,int group)
        {
            var cells=TetrisGame.Shape(kind);float mx=0,my=0;foreach(var c in cells){mx+=c.X/4f;my+=c.Y/4f;}
            for(int i=0;i<4;i++)
            {
                var im=mobilePreviewCells[group*4+i];im.gameObject.SetActive(true);im.color=CrystalView.Palette[(int)kind];
                im.rectTransform.anchoredPosition=new Vector2((cells[i].X-mx)*27,(cells[i].Y-my)*27);
            }
        }
    }
}
