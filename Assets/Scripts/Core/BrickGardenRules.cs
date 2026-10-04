using System;
namespace iTetris.Core
{
    // A turn is either planting the next crystal or merging two equal neighbours.
    public sealed class BrickGardenRules
    {
        public const int Width=4,Count=16,MaxRank=8;
        public readonly int[] Board=new int[Count];
        readonly Random random;
        public int Score {get;private set;}
        public int Moves {get;private set;}
        public int Next {get;private set;}
        public int Highest {get {int n=0;foreach(int rank in Board)n=Math.Max(n,rank);return n;}}
        public bool Bloomed=>Highest==MaxRank;
        public bool HasMoves
        {
            get
            {
                for(int i=0;i<Count;i++)
                {
                    if(Board[i]==0)return true;
                    if(i%Width<Width-1&&CanMerge(i,i+1))return true;
                    if(i+Width<Count&&CanMerge(i,i+Width))return true;
                }
                return false;
            }
        }
        public BrickGardenRules(int seed){random=new Random(seed);Restart();}
        public void Restart()
        {
            Array.Clear(Board,0,Count);Score=Moves=0;
            int[] order=new int[Count];for(int i=0;i<Count;i++)order[i]=i;
            for(int i=Count-1;i>0;i--){int j=random.Next(i+1),v=order[i];order[i]=order[j];order[j]=v;}
            for(int i=0;i<7;i++)Board[order[i]]=random.NextDouble()<.8?1:2;
            Board[0]=Board[1]=1;Next=RollNext();
        }
        int RollNext()=>random.NextDouble()<.85?1:2;
        public bool Place(int index)
        {
            if(index<0||index>=Count||Board[index]!=0)return false;
            Board[index]=Next;Moves++;Next=RollNext();return true;
        }
        public bool CanMerge(int from,int to)
        {
            if(from<0||from>=Count||to<0||to>=Count)return false;
            return Board[from]>0&&Board[from]<MaxRank&&Board[from]==Board[to]
                &&Math.Abs(from%Width-to%Width)+Math.Abs(from/Width-to/Width)==1;
        }
        public bool Merge(int from,int to)
        {
            if(!CanMerge(from,to))return false;
            Board[to]++;Board[from]=0;Score+=(1<<Board[to])*10;Moves++;return true;
        }
        public bool Restore(int[] board,int score,int moves,int next)
        {
            if(board==null||board.Length!=Count||score<0||moves<0||next<1||next>2)return false;
            foreach(int rank in board)if(rank<0||rank>MaxRank)return false;
            Array.Copy(board,Board,Count);Score=score;Moves=moves;Next=next;return true;
        }
    }
}
