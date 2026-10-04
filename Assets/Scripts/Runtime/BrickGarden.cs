using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using iTetris.Core;

namespace iTetris
{
    public sealed class GardenTile : MonoBehaviour {public int Index;}
    public sealed partial class GameController
    {
        BrickGardenRules garden;
        GameObject gardenWorld,gardenUi,gardenMenu,gardenHelp,gardenPreview;
        bool gardenActive,gardenPaused,gardenNoSave,gardenKeyboardFocus;
        int gardenSelected=-1,gardenCursor,gardenBest;
        readonly GameObject[] gardenPieces=new GameObject[16];
        readonly float[] gardenPulses=new float[16];
        Material gardenStone;
        Text gardenScore,gardenMoves,gardenBestText,gardenNext,gardenStatus,gardenMenuTitle,gardenMenuSubtitle;
        Button gardenResume;
        readonly Sprite[] gardenSprites=new Sprite[8];
        readonly GameObject[] gardenSelections=new GameObject[16];
        Transform gardenNextPanel;
        const string GardenSaveKey="BrickGarden.Save.v1";
        [Serializable] sealed class GardenSave {public int[] cells;public int score,moves,next;}
        // Pixel coordinates measured on GardenPlate (1586 × 992), in board index order.
        static readonly Vector2[] GardenCenters={
            new Vector2(791,701),new Vector2(916,621),new Vector2(1035,540),new Vector2(1140,456),
            new Vector2(666,621),new Vector2(791,540),new Vector2(912,457),new Vector2(1019,380),
            new Vector2(547,540),new Vector2(671,457),new Vector2(792,384),new Vector2(903,312),
            new Vector2(442,455),new Vector2(562,380),new Vector2(680,312),new Vector2(792,245)};
        // Top, right, bottom, left corners of each stone's upper face.
        static readonly Vector2[][] GardenFaces={
            new[]{new Vector2(791,649),new Vector2(868,700),new Vector2(791,750),new Vector2(713,700)},
            new[]{new Vector2(916,569),new Vector2(994,620),new Vector2(917,669),new Vector2(840,620)},
            new[]{new Vector2(1034,491),new Vector2(1109,539),new Vector2(1036,587),new Vector2(959,539)},
            new[]{new Vector2(1138,408),new Vector2(1213,455),new Vector2(1142,502),new Vector2(1066,455)},
            new[]{new Vector2(666,570),new Vector2(743,621),new Vector2(666,670),new Vector2(590,621)},
            new[]{new Vector2(791,491),new Vector2(866,539),new Vector2(792,587),new Vector2(717,539)},
            new[]{new Vector2(912,409),new Vector2(986,457),new Vector2(913,505),new Vector2(838,457)},
            new[]{new Vector2(1018,337),new Vector2(1092,380),new Vector2(1020,426),new Vector2(949,380)},
            new[]{new Vector2(548,491),new Vector2(623,539),new Vector2(548,588),new Vector2(473,539)},
            new[]{new Vector2(671,409),new Vector2(746,457),new Vector2(671,504),new Vector2(596,457)},
            new[]{new Vector2(792,341),new Vector2(866,383),new Vector2(792,430),new Vector2(718,383)},
            new[]{new Vector2(902,269),new Vector2(976,312),new Vector2(904,355),new Vector2(832,312)},
            new[]{new Vector2(443,408),new Vector2(517,455),new Vector2(439,502),new Vector2(369,455)},
            new[]{new Vector2(563,337),new Vector2(633,380),new Vector2(560,424),new Vector2(491,380)},
            new[]{new Vector2(680,272),new Vector2(752,312),new Vector2(680,354),new Vector2(609,312)},
            new[]{new Vector2(792,206),new Vector2(859,245),new Vector2(792,289),new Vector2(725,245)}};
        static Vector3 GardenPixel(Vector2 p)=>new Vector3((p.x-793)*40/1586f,(496-p.y)*25/992f,0);
        static Vector3 GardenPosition(int index)=>GardenPixel(GardenCenters[index])+Vector3.forward*.5f;
        static Mesh GardenFaceMesh(int index)
        {
            var vertices=new Vector3[4];
            for(int i=0;i<4;i++)vertices[i]=GardenPixel(GardenFaces[index][i])-GardenPixel(GardenCenters[index]);
            var mesh=new Mesh{name="Calibrated stone face "+index};mesh.vertices=vertices;mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateNormals();return mesh;
        }
        static readonly Color[] GardenColors={new Color(.13f,.83f,.98f),new Color(.58f,.35f,1),new Color(.23f,.47f,1),new Color(.15f,.95f,.68f),new Color(1,.75f,.26f),new Color(.98f,.35f,.62f),new Color(.69f,.83f,1),new Color(.96f,.91f,.66f)};

        void OpenGarden()
        {
            if(garden==null)BuildGarden();
            hubVisible=false;hubRoot.SetActive(false);tetrisWorld.SetActive(false);tetrisUiRoot.SetActive(false);
            gardenActive=true;gardenWorld.SetActive(true);gardenUi.SetActive(true);gardenPaused=false;gardenMenu.SetActive(false);gardenHelp.SetActive(false);shake=0;
            DrawGarden();
        }
        void BuildGarden()
        {
            garden=new BrickGardenRules(Environment.TickCount);gardenBest=PlayerPrefs.GetInt("BrickGarden.Best",0);
            if(!gardenNoSave&&PlayerPrefs.HasKey(GardenSaveKey))
            {
                try{var save=JsonUtility.FromJson<GardenSave>(PlayerPrefs.GetString(GardenSaveKey));if(save!=null)garden.Restore(save.cells,save.score,save.moves,save.next);}catch(ArgumentException){}
            }
            gardenWorld=new GameObject("Brick Garden world");gardenWorld.SetActive(false);
            var plate=Quad("Garden concept environment",Vector3.forward*6,new Vector2(40,25),Color.white,false);plate.transform.SetParent(gardenWorld.transform,true);
            var plateMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));plateMaterial.SetTexture("_BaseMap",Resources.Load<Texture2D>("Art/GardenPlate"));plate.GetComponent<Renderer>().sharedMaterial=plateMaterial;
            var atlas=Resources.Load<Texture2D>("Art/GardenCrystals");float cellWidth=atlas.width/4f,cellHeight=atlas.height/2f;
            var pivots=new[]{new Vector2(.4812f,.05859f),new Vector2(.43527f,.07031f),new Vector2(.45182f,.05469f),new Vector2(.48993f,.04297f),new Vector2(.50532f,.15625f),new Vector2(.49919f,.15234f),new Vector2(.48958f,.14648f),new Vector2(.48698f,.14844f)};
            for(int i=0;i<8;i++)gardenSprites[i]=Sprite.Create(atlas,new Rect((i%4)*cellWidth,(1-i/4)*cellHeight,cellWidth,cellHeight),pivots[i],100,0,SpriteMeshType.FullRect);
            gardenStone=CrystalView.Lit(new Color(.035f,.08f,.12f),.52f,.88f,new Color(.005f,.014f,.02f));
            for(int i=0;i<16;i++)
            {
                var tile=GardenMeshObject("Garden tile "+i,gardenWorld.transform,GardenFaceMesh(i),gardenStone);tile.transform.position=GardenPosition(i);
                tile.GetComponent<Renderer>().enabled=false;tile.layer=8;
                tile.AddComponent<MeshCollider>().sharedMesh=tile.GetComponent<MeshFilter>().sharedMesh;tile.AddComponent<GardenTile>().Index=i;
                var selection=GardenMeshObject("Selected stone tint",tile.transform,GardenFaceMesh(i),CrystalView.Unlit(new Color(.12f,.85f,.86f,.35f)));selection.transform.localPosition=Vector3.back*.025f;gardenSelections[i]=selection;selection.SetActive(false);
            }
            gardenUi=UIObject("Brick Garden interface",canvas.transform,Vector2.zero,new Vector2(1600,1000)).gameObject;
            var root=gardenUi.transform;
            Label(root,"B R I C K   G A R D E N",new Vector2(0,450),new Vector2(950,60),30,Pale);
            Label(root,"◇",new Vector2(0,408),new Vector2(100,26),18,Teal);
            var left=Panel(root,new Vector2(-654,418),new Vector2(230,108),new Color(.02f,.06f,.1f,.5f));
            Label(left,"S C O R E",new Vector2(0,30),new Vector2(210,22),12,Muted);
            gardenScore=Label(left,"0",new Vector2(0,-14),new Vector2(210,50),32,Pale);
            gardenBestText=Label(root,"",new Vector2(-627,-411),new Vector2(280,28),12,Muted);
            gardenMoves=Label(root,"",new Vector2(-627,-442),new Vector2(280,28),12,Muted);
            var right=Panel(root,new Vector2(674,386),new Vector2(140,185),new Color(.02f,.06f,.1f,.5f));gardenNextPanel=right;
            Label(right,"N E X T",new Vector2(0,67),new Vector2(130,24),12,Pale);
            gardenNext=Label(right,"",new Vector2(0,-74),new Vector2(130,24),10,Muted);
            ButtonAt(root,"GAME HUB",new Vector2(-676,-478),new Vector2(140,32),()=>OpenHub());
            ButtonAt(root,"PAUSE",new Vector2(691,-475),new Vector2(90,34),()=>PauseGarden());
            ButtonAt(root,"HELP",new Vector2(580,-475),new Vector2(90,34),()=>{gardenPaused=true;gardenHelp.SetActive(true);});
            ButtonAt(root,"NEW GARDEN",new Vector2(429,-475),new Vector2(160,34),()=>ShowGardenMenu("A FRESH START?","Your current garden will be replaced.",true));
            gardenStatus=Label(root,"Select two matching neighbours. Plant on empty tiles.",new Vector2(0,-416),new Vector2(780,36),14,Teal);
            Label(root,"GROW A LEVEL 8 CRYSTAL TO MAKE YOUR GARDEN BLOOM",new Vector2(0,-453),new Vector2(900,24),10,Muted);
            gardenMenu=Panel(root,Vector2.zero,new Vector2(600,470),new Color(.015f,.035f,.06f,.99f)).gameObject;
            gardenMenuTitle=Label(gardenMenu.transform,"",new Vector2(0,160),new Vector2(550,65),33,Pale);
            gardenMenuSubtitle=Label(gardenMenu.transform,"",new Vector2(0,88),new Vector2(550,70),16,Muted);
            gardenResume=ButtonAt(gardenMenu.transform,"RESUME",new Vector2(0,8),new Vector2(300,52),()=>ResumeGarden(),true);
            ButtonAt(gardenMenu.transform,"NEW GARDEN",new Vector2(0,-63),new Vector2(300,46),()=>RestartGarden());
            ButtonAt(gardenMenu.transform,"GAME HUB",new Vector2(0,-128),new Vector2(300,42),()=>OpenHub());
            gardenMenu.SetActive(false);
            gardenHelp=Panel(root,Vector2.zero,new Vector2(730,620),new Color(.015f,.035f,.06f,.99f)).gameObject;
            Label(gardenHelp.transform,"HOW TO GROW",new Vector2(0,245),new Vector2(650,60),32,Pale);
            Label(gardenHelp.transform,"Select a crystal, then click an equal-level neighbour.\nThe two crystals merge on the second tile.\nOnly side-by-side tiles can merge, not diagonals.\n\nClick an empty tile to plant the NEXT crystal.\nMerging frees space and earns points.\n\nGrow a level 8 crystal to make the garden bloom.\nKeep playing for a higher score.\nA full board with no matching neighbours ends the game.\n\nYour garden saves automatically on this device.",new Vector2(0,0),new Vector2(660,410),18,Pale);
            ButtonAt(gardenHelp.transform,"LET'S GROW",new Vector2(0,-244),new Vector2(280,52),()=>{gardenHelp.SetActive(false);gardenPaused=gardenMenu.activeSelf;},true);gardenHelp.SetActive(false);
        }
        static GameObject GardenMeshObject(string name,Transform parent,Mesh mesh,Material material)
        {
            var obj=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(parent,false);obj.GetComponent<MeshFilter>().sharedMesh=mesh;obj.GetComponent<MeshRenderer>().sharedMaterial=material;return obj;
        }
        static Mesh GardenDiamondMesh(float rx,float ry,float depth)
        {
            var p=new[]{new Vector3(0,ry,0),new Vector3(rx,0,0),new Vector3(0,-ry,0),new Vector3(-rx,0,0)};
            var verts=new List<Vector3>();var triangles=new List<int>();
            Action<Vector3,Vector3,Vector3> tri=(a,b,c)=>{int n=verts.Count;verts.Add(a);verts.Add(b);verts.Add(c);triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);};
            tri(p[0],p[1],p[2]);tri(p[0],p[2],p[3]);
            var d=new Vector3(0,-depth,depth);
            if(depth>0)for(int i=0;i<4;i++){int j=(i+1)%4;tri(p[i],p[i]+d,p[j]);tri(p[j],p[i]+d,p[j]+d);}
            var mesh=new Mesh{name="Garden diamond"};mesh.SetVertices(verts);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();return mesh;
        }
        GameObject GardenCluster(int rank,Vector3 position,Transform parent)
        {
            var obj=new GameObject("Level "+rank+" crystal",typeof(SpriteRenderer));obj.transform.SetParent(parent,false);obj.transform.localPosition=position;
            var renderer=obj.GetComponent<SpriteRenderer>();renderer.sprite=gardenSprites[rank-1];renderer.sortingOrder=100-Mathf.RoundToInt(position.y*4);
            float size=.44f+rank*.06f;obj.transform.localScale=Vector3.one*size;
            return obj;
        }
        void DrawGarden()
        {
            for(int i=0;i<16;i++)
            {
                if(gardenPieces[i]!=null)Destroy(gardenPieces[i]);gardenPieces[i]=null;
                if(garden.Board[i]>0)gardenPieces[i]=GardenCluster(garden.Board[i],GardenPosition(i)+new Vector3(0,0,-.7f),gardenWorld.transform);
            }
            if(gardenPreview!=null)Destroy(gardenPreview);
            gardenPreview=UIObject("Next crystal",gardenNextPanel,new Vector2(0,-5),new Vector2(104,116)).gameObject;
            var preview=gardenPreview.AddComponent<Image>();preview.sprite=gardenSprites[garden.Next-1];preview.preserveAspect=true;preview.raycastTarget=false;
            gardenNext.text="LEVEL "+garden.Next;gardenScore.text=garden.Score.ToString("N0");gardenMoves.text="MOVES  "+garden.Moves;
            gardenBest=Mathf.Max(gardenBest,garden.Score);gardenBestText.text="BEST  "+gardenBest.ToString("N0");
            UpdateGardenSelection();
            if(!garden.HasMoves)ShowGardenMenu("GARDEN COMPLETE","No space left and no matching neighbours.\nScore  "+garden.Score.ToString("N0"),false);
            else if(garden.Bloomed)gardenStatus.text="Your garden is in bloom. Keep growing for a higher score.";
        }
        void UpdateGardenSelection()
        {
            for(int i=0;i<16;i++)gardenSelections[i].SetActive(i==gardenSelected||(gardenKeyboardFocus&&gardenSelected<0&&i==gardenCursor));
        }
        void GardenClick(int index)
        {
            if(gardenPaused||index<0||index>=16)return;
            gardenCursor=index;
            bool changed=false;
            if(garden.Board[index]==0)
            {
                changed=garden.Place(index);gardenSelected=-1;gardenStatus.text="A new crystal takes root.";audioWorld.Play("hold");
            }
            else if(gardenSelected>=0&&garden.CanMerge(gardenSelected,index))
            {
                garden.Merge(gardenSelected,index);gardenSelected=-1;changed=true;gardenStatus.text="LEVEL "+garden.Board[index]+"  ·  +"+((1<<garden.Board[index])*10)+" POINTS";audioWorld.Play("clear");GardenBurst(index);
            }
            else
            {
                gardenSelected=gardenSelected==index?-1:index;
                gardenStatus.text=gardenSelected<0?"Click an empty tile to plant the next crystal.":"Choose a side-by-side crystal of the same level.";audioWorld.Play("move");
            }
            if(changed){gardenPulses[index]=.35f;DrawGarden();SaveGarden();}else UpdateGardenSelection();
        }
        void GardenBurst(int index)
        {
            var origin=GardenPosition(index)+new Vector3(0,.6f,-1.1f);Color color=GardenColors[garden.Board[index]-1];
            for(int i=0;i<18;i++)
            {
                var obj=Quad("Garden sparkle",origin,Vector2.one*.05f,color*2,false);obj.transform.SetParent(gardenWorld.transform,true);
                float life=UnityEngine.Random.Range(.35f,.8f);sparks.Add(new Spark{T=obj.transform,Velocity=new Vector3(UnityEngine.Random.Range(-2,2),UnityEngine.Random.Range(1,4),0),Life=life,Max=life});
            }
        }
        void UpdateGarden()
        {
            UpdateAtmosphere();UpdateSparks();
            for(int i=0;i<16;i++)if(gardenPulses[i]>0&&gardenPieces[i]!=null){gardenPulses[i]-=Time.deltaTime;gardenPieces[i].transform.localScale=Vector3.one*(.44f+garden.Board[i]*.06f)*(1+.16f*Mathf.Sin(Mathf.Max(0,gardenPulses[i])/.35f*Mathf.PI));}
            if(Input.GetKeyDown(KeyCode.Escape)){if(gardenHelp.activeSelf){gardenHelp.SetActive(false);gardenPaused=gardenMenu.activeSelf;}else if(gardenMenu.activeSelf)ResumeGarden();else PauseGarden();}
            if(gardenPaused)return;
            int row=gardenCursor/4,col=gardenCursor%4;
            if(Input.GetKeyDown(KeyCode.LeftArrow))col=Mathf.Max(0,col-1);if(Input.GetKeyDown(KeyCode.RightArrow))col=Mathf.Min(3,col+1);
            if(Input.GetKeyDown(KeyCode.UpArrow))row=Mathf.Max(0,row-1);if(Input.GetKeyDown(KeyCode.DownArrow))row=Mathf.Min(3,row+1);
            if(gardenCursor!=row*4+col){gardenKeyboardFocus=true;gardenCursor=row*4+col;UpdateGardenSelection();}
            if(Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.Space))GardenClick(gardenCursor);
            if(Input.GetMouseButtonDown(0)&&!EventSystem.current.IsPointerOverGameObject())
            {
                RaycastHit hit;if(Physics.Raycast(cameraMain.ScreenPointToRay(Input.mousePosition),out hit,100,1<<8)){gardenKeyboardFocus=false;GardenClick(hit.collider.GetComponent<GardenTile>().Index);}
            }
        }
        void ShowGardenMenu(string title,string subtitle,bool resume)
        {gardenPaused=true;gardenMenu.SetActive(true);gardenMenuTitle.text=title;gardenMenuSubtitle.text=subtitle;gardenResume.gameObject.SetActive(resume);}
        void PauseGarden(){if(!gardenMenu.activeSelf)ShowGardenMenu("PAUSED","Take a breath. Your garden can wait.",garden.HasMoves);}
        void ResumeGarden(){if(!garden.HasMoves)return;gardenMenu.SetActive(false);gardenPaused=false;}
        void RestartGarden()
        {garden.Restart();gardenSelected=-1;gardenCursor=0;gardenPaused=false;gardenMenu.SetActive(false);gardenHelp.SetActive(false);gardenStatus.text="A fresh garden. Start with a matching pair.";DrawGarden();SaveGarden();}
        void SaveGarden()
        {
            if(garden==null||gardenNoSave)return;
            PlayerPrefs.SetString(GardenSaveKey,JsonUtility.ToJson(new GardenSave{cells=garden.Board,score=garden.Score,moves=garden.Moves,next=garden.Next}));
            PlayerPrefs.SetInt("BrickGarden.Best",gardenBest);PlayerPrefs.Save();
        }
        #if !UNITY_WEBGL || UNITY_EDITOR
        IEnumerator GardenCapture()
        {
            yield return new WaitForSecondsRealtime(1);OpenGarden();
            var board=new[]{0,1,2,1,1,1,0,2,2,0,3,0,0,1,2,1};
            garden.Restore(board,1240,26,1);gardenSelected=1;gardenCursor=1;DrawGarden();
            yield return new WaitForSecondsRealtime(1);
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--capture-path");
            string path=index>=0&&index+1<args.Length?args[index+1]:System.IO.Path.Combine(Application.persistentDataPath,"BrickGarden.png");
            ScreenCapture.CaptureScreenshot(path);yield return new WaitForSecondsRealtime(2);
            if(Array.IndexOf(args,"--quit-after-capture")>=0)Application.Quit();
        }
        #endif
    }
}
