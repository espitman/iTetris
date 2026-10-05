using System;
using System.Collections.Generic;
namespace iTetris.Core
{
    public sealed class CrystalBreakerRules
    {
        public sealed class Ball { public float X,Y,VX,VY; }
        public sealed class Brick { public float X,Y; public int HP,Color; }
        public sealed class Gift { public float X,Y; public bool Wide; }
        public readonly List<Ball> Balls=new List<Ball>();
        public readonly List<Brick> Bricks=new List<Brick>();
        public readonly List<Gift> Gifts=new List<Gift>();
        public int Score,Stage=1,Lives=3; public bool Waiting=true,Over,Won;
        public float Paddle,WideTime; public float HalfWidth=>WideTime>0?2.3f:1.5f;
        public event Action<float,float,int> Broken;
        int destroyed;
        public CrystalBreakerRules(){LoadStage();}
        public void SetPaddle(float x){Paddle=Math.Clamp(x,-12+HalfWidth,12-HalfWidth);if(Waiting&&Balls.Count>0){Balls[0].X=Paddle;Balls[0].Y=-7.2f;}}
        public void Launch(){if(!Waiting||Over)return;Waiting=false;Balls[0].VX=3;Balls[0].VY=9+Stage;}
        void LoadStage(){Bricks.Clear();Gifts.Clear();WideTime=0;for(int row=0;row<4+Stage;row++)for(int col=0;col<10;col++){if(Stage>1&&(row+col+Stage)%7==0)continue;Bricks.Add(new Brick{X=(col-4.5f)*2.2f,Y=6.6f-row*.95f,HP=Stage>2&&row<2?2:1,Color=(row+Stage-1)%7});}Serve();}
        void Serve(){Balls.Clear();Balls.Add(new Ball{X=Paddle,Y=-7.2f});Waiting=true;}
        public void Power(bool wide){if(wide){WideTime=15;SetPaddle(Paddle);}else{if(Waiting)Launch();int count=Math.Min(3,Balls.Count);for(int i=0;i<count&&Balls.Count<6;i++){var b=Balls[i];Balls.Add(new Ball{X=b.X,Y=b.Y,VX=-b.VY*.6f,VY=Math.Abs(b.VY)*.8f});}}}
        public void Tick(float dt){if(Over||Waiting)return;dt=Math.Clamp(dt,0,.1f);int steps=(int)Math.Ceiling(dt/.003f);float s=dt/Math.Max(1,steps);for(int n=0;n<steps;n++)Step(s);}
        void Step(float dt){if(Over||Waiting)return;WideTime=Math.Max(0,WideTime-dt);SetPaddle(Paddle);
            for(int i=Gifts.Count-1;i>=0;i--){var g=Gifts[i];g.Y-=3*dt;if(g.Y<=-7.7f&&g.Y>=-8.3f&&Math.Abs(g.X-Paddle)<HalfWidth+.3f){Power(g.Wide);Gifts.RemoveAt(i);}else if(g.Y<-10)Gifts.RemoveAt(i);}
            for(int k=Balls.Count-1;k>=0;k--){var b=Balls[k];float oldX=b.X,oldY=b.Y;b.X+=b.VX*dt;b.Y+=b.VY*dt;
                if(Math.Abs(b.X)>11.8f){b.X=Math.Clamp(b.X,-11.8f,11.8f);b.VX=-b.VX;}if(b.Y>8.8f){b.Y=8.8f;b.VY=-Math.Abs(b.VY);}
                if(b.VY<0&&oldY>=-7.55f&&b.Y<=-7.55f&&Math.Abs(b.X-Paddle)<=HalfWidth+.2f){float t=Math.Clamp((b.X-Paddle)/HalfWidth,-1,1),speed=10+Stage;b.Y=-7.55f;b.VX=t*speed*.82f;b.VY=(float)Math.Sqrt(speed*speed-b.VX*b.VX);}
                for(int j=Bricks.Count-1;j>=0;j--){var brick=Bricks[j];if(Math.Abs(b.X-brick.X)>1.2f||Math.Abs(b.Y-brick.Y)>.6f)continue;
                    if(Math.Abs(oldX-brick.X)>=1.2f){b.X=oldX;b.VX=-b.VX;}else{b.Y=oldY;b.VY=-b.VY;}
                    if(--brick.HP==0){Bricks.RemoveAt(j);Score+=100*Stage;destroyed++;Broken?.Invoke(brick.X,brick.Y,brick.Color);if(destroyed%9==0)Gifts.Add(new Gift{X=brick.X,Y=brick.Y,Wide=(destroyed/9)%2==1});}break;}
                if(b.Y<-10)Balls.RemoveAt(k);
            }
            if(Bricks.Count==0){if(Stage==5){Over=true;Won=true;}else{Stage++;LoadStage();}return;}
            if(Balls.Count==0){Lives--;Gifts.Clear();if(Lives==0)Over=true;else Serve();}
        }
    }
}
