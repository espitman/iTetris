using System;
using System.Text;
namespace iTetris.Core
{
 public static class CrystalBreakerVerification
 {
  public static string Run(){var output=new StringBuilder();void Check(bool ok,string name){if(!ok)throw new Exception(name);output.AppendLine("PASS: "+name);}var g=new CrystalBreakerRules();
   Check(g.Bricks.Count==27&&g.Lives==3&&g.Waiting,"breaker initial stage and serve");g.SetPaddle(100);Check(g.Paddle==CrystalBreakerRules.Wall-g.HalfWidth&&g.Balls[0].X==g.Paddle,"paddle clamps and carries served ball");g.Launch();Check(!g.Waiting&&g.Balls[0].VY>0,"launch starts ball");
   g.Balls[0].X=CrystalBreakerRules.Wall-CrystalBreakerRules.Radius-.01f;g.Balls[0].Y=0;g.Balls[0].VX=12;g.Balls[0].VY=0;g.Tick(.02f);Check(g.Balls[0].VX<0,"side wall reflects ball");
   g.SetPaddle(0);var b=g.Balls[0];b.X=1;b.Y=CrystalBreakerRules.PaddleY+.65f;b.VX=0;b.VY=-15;g.Tick(.02f);Check(b.VY>0&&b.VX>0,"paddle steers upward with minimum vertical speed");
   g.Power(true);Check(g.HalfWidth==4.3f,"wide power enlarges paddle");g.Power(false);Check(g.Balls.Count==2,"multiball creates extra ball");foreach(var ball in g.Balls){ball.Y=-11;}g.Tick(.01f);Check(g.Lives==2&&g.Waiting&&g.Balls.Count==1,"loss consumes one life and serves");
   g.Launch();g.Bricks.Clear();g.Bricks.Add(new CrystalBreakerRules.Brick{X=0,Y=0,HP=2});b=g.Balls[0];b.X=0;b.Y=-.7f;b.VX=0;b.VY=30;g.Tick(.01f);Check(g.Bricks[0].HP==1&&b.VY<0&&g.Score==0,"fast ball hits armored brick without tunneling");
   b.Y=-.7f;b.VY=30;g.Tick(.01f);Check(g.Stage==2&&g.Score==100&&g.Waiting,"last brick scores and advances stage");
   g.Stage=5;g.Bricks.Clear();g.Launch();g.Tick(.01f);Check(g.Over&&g.Won,"final stage victory");
   g=new CrystalBreakerRules();g.Launch();g.Gifts.Add(new CrystalBreakerRules.Gift{X=0,Y=CrystalBreakerRules.PaddleY+.36f,Wide=true});g.Tick(.02f);Check(g.Gifts.Count==0&&g.WideTime>14,"falling power is caught by paddle");g.WideTime=.001f;g.Tick(.01f);Check(g.HalfWidth==3.3f,"wide power expires");
   g=new CrystalBreakerRules();for(int i=0;i<3;i++){g.Launch();g.Balls[0].Y=-11;g.Tick(.01f);}Check(g.Over&&!g.Won&&g.Lives==0,"three losses end game");
   g=new CrystalBreakerRules();g.Launch();g.Bricks.Clear();
   for(int i=0;i<3;i++)g.Bricks.Add(new CrystalBreakerRules.Brick{X=i*4,Y=0,HP=1});
   b=g.Balls[0];for(int i=0;i<2;i++){b.X=i*4;b.Y=-.75f;b.VX=0;b.VY=15;g.Tick(.02f);}
   Check(g.Combo==2&&g.Score==300,"consecutive crystal breaks award combo score");
   b.X=0;b.Y=CrystalBreakerRules.PaddleY+.65f;b.VX=0;b.VY=-15;g.Tick(.02f);
   Check(g.Combo==0,"paddle contact resets combo");
   return output.ToString();
  }
 }
}
