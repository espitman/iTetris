using System;
using System.Text;
namespace iTetris.Core
{
    public static class BrickGardenVerification
    {
        public static string Run()
        {
            var report=new StringBuilder();int passed=0;
            Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("Garden failed: "+name);report.AppendLine("PASS: "+name);passed++;};
            var g=new BrickGardenRules(42);check(g.HasMoves&&g.CanMerge(0,1),"new garden has a legal merge");
            var board=new int[16];board[0]=board[1]=1;g.Restore(board,0,0,2);
            check(g.Merge(0,1)&&g.Board[0]==0&&g.Board[1]==2&&g.Score==40&&g.Moves==1,"merge grows destination and frees source");
            check(g.Next==2,"merge preserves planting preview");
            check(g.Place(0)&&g.Board[0]==2&&g.Moves==2,"empty tile receives preview rank");
            int score=g.Score,moves=g.Moves;check(!g.Place(0)&&!g.Merge(0,15)&&g.Score==score&&g.Moves==moves,"invalid moves leave state unchanged");
            Array.Clear(board,0,board.Length);board[3]=board[4]=1;g.Restore(board,0,0,1);check(!g.CanMerge(3,4),"row edges do not wrap");
            board[0]=board[5]=1;g.Restore(board,0,0,1);check(!g.CanMerge(0,5),"diagonals cannot merge");
            for(int i=0;i<16;i++)board[i]=(i%4+i/4)%2+1;g.Restore(board,0,0,1);check(!g.HasMoves,"full checkerboard ends the game");
            board[1]=board[0];g.Restore(board,0,0,1);check(g.HasMoves,"full board with a pair remains playable");
            board[0]=board[1]=7;g.Restore(board,0,0,1);check(g.Merge(0,1)&&g.Bloomed&&g.Score==2560,"rank eight is the bloom goal");
            for(int i=0;i<16;i++)board[i]=8;g.Restore(board,0,0,1);check(!g.HasMoves&&!g.Merge(0,1),"maximum rank does not overflow");
            check(!g.Restore(new int[3],0,0,1)&&!g.Restore(board,-1,0,1),"invalid saves rejected");
            board[0]=9;check(!g.Restore(board,0,0,1)&&g.Board[0]==8,"bad ranks reject atomically");
            g.Restart();check(g.Score==0&&g.Moves==0&&g.HasMoves&&!g.Bloomed,"restart resets progress");
            report.AppendLine("All "+passed+" garden rule checks passed.");return report.ToString();
        }
    }
}
