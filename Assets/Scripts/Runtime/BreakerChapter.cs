using UnityEngine;
using iTetris.Core;
namespace iTetris
{
    public sealed partial class GameController
    {
        readonly LineRenderer[] breakerChapterWaves=new LineRenderer[2];
        ParticleSystem BuildChapterParticles(string name,Sprite sprite)
        {
            var obj=new GameObject(name);obj.transform.SetParent(breakerWorld.transform,false);
            var system=obj.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;main.maxParticles=1000;main.playOnAwake=false;main.simulationSpace=ParticleSystemSimulationSpace.Local;
            var emission=system.emission;emission.enabled=false;
            var sheet=system.textureSheetAnimation;sheet.enabled=true;sheet.mode=ParticleSystemAnimationMode.Sprites;sheet.AddSprite(sprite);
            var renderer=system.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=new Material(Resources.Load<Material>("Materials/BreakerShard"));renderer.sharedMaterial.mainTexture=sprite.texture;renderer.sortingOrder=17;
            if(name=="Light gathering")for(int i=0;i<2;i++)
            {
                var wave=new GameObject("Aurora wave");wave.transform.SetParent(breakerWorld.transform,false);
                var line=wave.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=48;
                line.sharedMaterial=Resources.Load<Material>("Materials/BreakerTrail");line.startWidth=.35f;line.endWidth=.22f;line.sortingOrder=16;
                breakerChapterWaves[i]=line;
            }
            return system;
        }
        void DrawChapterAssembly(bool show,float t,float alpha)
        {
            breakerChapterDust.gameObject.SetActive(show);breakerChapterShards.gameObject.SetActive(show);
            for(int wave=0;wave<2;wave++)
            {
                var line=breakerChapterWaves[wave];line.gameObject.SetActive(show);
                line.startColor=new Color(.4f,.95f,1,alpha*.8f);line.endColor=new Color(.4f,.95f,1,alpha*.15f);
                for(int point=0;point<48;point++)
                {
                    float travel=point/47f*Mathf.Clamp01(t*1.8f);
                    line.SetPosition(point,new Vector3((wave==0?-1:1)*Mathf.Sin(travel*Mathf.PI*.8f)*15,-9.4f+travel*18,-.55f));
                }
            }
            if(!show)
            {
                foreach(var obj in breakerFormingBricks)obj.SetActive(false);
                breakerPreviewStage=-1;return;
            }
            if(breakerPreviewStage!=breaker.Stage+1)
            {
                foreach(var obj in breakerFormingBricks)Destroy(obj);breakerFormingBricks.Clear();
                var next=new CrystalBreakerRules(breaker.Stage+1);
                foreach(var brick in next.Bricks)
                {
                    var obj=BreakerSprite("Materializing next stage",brick.Color,BreakerPosition(brick.X,brick.Y),1.49f,5);
                    if(brick.MaxHP>1){obj.GetComponent<SpriteRenderer>().sprite=breakerDurabilitySprites[brick.Color];obj.transform.localScale=new Vector3(1.3f,1.03f,1);}
                    breakerFormingBricks.Add(obj);
                }
                breakerPreviewStage=breaker.Stage+1;
            }
            for(int i=0;i<breakerFormingBricks.Count;i++)
            {
                var obj=breakerFormingBricks[i];obj.SetActive(true);
                float begin=.22f+(i*7%27)/27f*.35f;
                float formed=Mathf.SmoothStep(0,1,Mathf.InverseLerp(begin,begin+.3f,t));
                obj.GetComponent<SpriteRenderer>().color=new Color(1,1,1,formed);
            }
            FillChapterParticles(breakerChapterDust,breakerDustParticles,t,alpha,false);
            FillChapterParticles(breakerChapterShards,breakerShardParticles,t,alpha,true);
        }
        void FillChapterParticles(ParticleSystem system,ParticleSystem.Particle[] particles,float t,float alpha,bool shards)
        {
            for(int i=0;i<particles.Length;i++)
            {
                float seed=Mathf.Repeat(i*.6180339f,1),travel=Mathf.Repeat(i*.037f+t*.42f,1);
                int sign=i%2==0?1:-1;
                Vector3 pos;
                if(i%3==0)
                {
                    var brick=breakerFormingBricks[i%breakerFormingBricks.Count];
                    pos=brick.transform.localPosition+new Vector3((seed-.5f)*3.3f,(Mathf.Repeat(i*.413f,1)-.5f)*1.4f,-.1f);
                }
                else
                {
                    pos=new Vector3(sign*Mathf.Sin(travel*Mathf.PI*.8f)*15,-9.4f+travel*18,-.5f);
                    float spread=shards?2f:.7f;
                    pos+=new Vector3(Mathf.Sin(i*12.3f+t*3)*spread,Mathf.Cos(i*7.1f+t*2)*spread,0);
                }
                particles[i].position=pos;
                particles[i].startSize=shards?Mathf.Lerp(.65f,1.8f,seed):Mathf.Lerp(.04f,.18f,seed);
                particles[i].startColor=new Color(.65f,1,1,alpha*(shards?1f:.9f));
                particles[i].rotation=shards?i*73+t*120:0;
                particles[i].startLifetime=10;particles[i].remainingLifetime=10;
            }
            system.SetParticles(particles,particles.Length);
        }
    }
}
