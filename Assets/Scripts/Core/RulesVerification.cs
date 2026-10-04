using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
namespace iTetris.Core
{
    public static class RulesVerification
    {
        static void Require(bool condition,string message){if(!condition)throw new Exception("FAILED: "+message);}
        static void Set(TetrisGame g,string name,object value)
        {typeof(TetrisGame).GetField("<"+name+">k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,value);}
        static void Place(TetrisGame g,PieceKind kind,int x,int y,int r=0)
        {Set(g,"Active",kind);Set(g,"X",x);Set(g,"Y",y);Set(g,"Rotation",r);}
        public static string Run()
        {
            var report=new StringBuilder();int count=0;
            Action<string,Action> test=(name,run)=>{run();count++;report.AppendLine("PASS "+name);};
            test("Every shape has four unique cells across all rotations",()=>{
                foreach(PieceKind p in Enum.GetValues(typeof(PieceKind)))for(int r=0;r<4;r++)
                {var seen=new HashSet<string>();foreach(var c in TetrisGame.Shape(p,r))seen.Add(c.X+","+c.Y);Require(seen.Count==4,p+" unique shape");}
            });
            test("Seven bag contains every piece once",()=>{
                for(int seed=0;seed<50;seed++){var g=new TetrisGame(seed);var seen=new HashSet<PieceKind>{g.Active};foreach(var p in g.Next(6))seen.Add(p);Require(seen.Count==7,"seven bag seed "+seed);}
            });
            test("Hold is limited to once per piece and resets after lock",()=>{
                var g=new TetrisGame(1);var first=g.Active;Require(g.Hold(),"first hold");Require(g.Held==first,"saved piece");Require(!g.Hold(),"duplicate hold blocked");g.HardDrop();Require(g.Hold(),"hold after lock");Require(g.Active==first,"swap retrieves original");
            });
            test("Wall collision and four rotations preserve cells",()=>{
                var g=new TetrisGame(4);Place(g,PieceKind.T,3,10);var before=g.ActiveCells();for(int i=0;i<4;i++)Require(g.Rotate(1),"rotate");var after=g.ActiveCells();for(int i=0;i<4;i++)Require(before[i].X==after[i].X&&before[i].Y==after[i].Y,"rotation cycle");while(g.Move(-1)){}Require(!g.Move(-1),"left boundary");
            });
            test("I piece floor kick succeeds using SRS offsets",()=>{
                var g=new TetrisGame(1);Place(g,PieceKind.I,3,-2);Require(g.Fits(g.Active,g.X,g.Y,0),"horizontal at floor");Require(g.Rotate(1),"floor rotation");Require(g.Y==0&&g.X==4,"I floor offset");
            });
            test("Ghost landing equals hard drop and awards distance score",()=>{
                var g=new TetrisGame(4);int distance=g.Y-g.GhostY();var cells=g.ActiveCells(g.GhostY());g.HardDrop();foreach(var c in cells)Require(g.Board[c.X,c.Y]!=0,"locked landing");Require(g.Score==distance*2,"drop points");
            });
            test("Lock delay waits half a second",()=>{
                var g=new TetrisGame(1);Place(g,PieceKind.O,3,-1);for(int i=0;i<4;i++)g.Tick(.1f);Require(g.Board[4,0]==0,"not locked early");g.Tick(.1f);Require(g.Board[4,0]!=0,"locks at delay");
            });
            test("Double clear compacts board and awards perfect clear",()=>{
                var g=new TetrisGame(2);Place(g,PieceKind.O,3,-1);for(int y=0;y<2;y++)for(int x=0;x<10;x++)if(x!=4&&x!=5)g.Board[x,y]=1;
                LockResult result=null;g.Locked+=r=>result=r;g.HardDrop();Require(g.Lines==2,"double");Require(result.PerfectClear,"perfect");Require(g.Score==2300,"perfect double points");for(int x=0;x<10;x++)Require(g.Board[x,0]==0,"empty bottom");
            });
            test("Four line clear, back to back and combo bonuses",()=>{
                var g=new TetrisGame(2);
                Action setup=()=>{Array.Clear(g.Board,0,g.Board.Length);Place(g,PieceKind.I,2,0,1);for(int y=0;y<4;y++)for(int x=0;x<10;x++)if(x!=4)g.Board[x,y]=2;g.Board[0,5]=3;};
                setup();g.HardDrop();Require(g.Lines==4&&g.Score==800,"first tetris");setup();g.HardDrop();Require(g.Lines==8&&g.Score==2050,"b2b and combo");Require(g.Board[0,1]==3,"row compaction");
            });
            test("Level increases at ten lines",()=>{var g=new TetrisGame(1);Set(g,"Lines",9);Place(g,PieceKind.I,3,-2);for(int x=0;x<10;x++)if(x<3||x>6)g.Board[x,0]=2;g.HardDrop();Require(g.Level==2&&g.Lines==10,"new level");Require(g.FallInterval<1,"faster gravity");});
            test("T spin requires rotation and three occupied corners",()=>{
                var g=new TetrisGame(1);Place(g,PieceKind.T,3,0);g.Board[3,0]=1;g.Board[5,0]=1;g.Board[3,2]=1;
                typeof(TetrisGame).GetField("lastRotate",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,true);
                LockResult result=null;g.Locked+=r=>result=r;g.HardDrop();Require(result.TSpin&&g.Score==400,"t spin bonus");
            });
            test("Spawn collision ends game and restart clears state",()=>{
                var g=new TetrisGame(1);for(int x=0;x<10;x++)for(int y=17;y<20;y++)g.Board[x,y]=1;g.Hold();Require(g.GameOver,"spawn collision");g.Restart();Require(!g.GameOver&&g.Score==0&&g.Held==null,"fresh game");for(int x=0;x<10;x++)Require(g.Board[x,19]==0,"cleared board");
            });
            test("Seeded stress play never produces out of bounds or partial pieces",()=>{
                for(int seed=0;seed<20;seed++){var g=new TetrisGame(seed);var rng=new Random(seed);for(int turn=0;turn<100&&!g.GameOver;turn++) {for(int r=0;r<rng.Next(4);r++)g.Rotate(1);int move=rng.Next(-6,7);for(int m=0;m<Math.Abs(move);m++)g.Move(Math.Sign(move));if(turn%5==0)g.Hold();foreach(var c in g.ActiveCells())Require(c.X>=0&&c.X<10&&c.Y>=0&&c.Y<22,"bounds");g.HardDrop();}}
            });
            report.AppendLine("ALL "+count+" RULES TESTS PASSED");return report.ToString();
        }
    }
}
