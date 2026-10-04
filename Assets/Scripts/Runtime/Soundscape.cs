using UnityEngine;
namespace iTetris
{
    public sealed class Soundscape : MonoBehaviour
    {
        AudioSource music, effects;
        AudioClip move, rotate, drop, clear, hold, end;
        public bool Muted { get; private set; }
        public void Initialize()
        {
            music=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=.25f;music.clip=Resources.Load<AudioClip>("Audio/Aurora");
            effects=gameObject.AddComponent<AudioSource>();effects.volume=.36f;
            move=Tone(330,.035f);rotate=Tone(550,.07f);drop=Tone(140,.16f);clear=Tone(880,.55f);hold=Tone(660,.14f);end=Tone(110,1f);
            Muted=PlayerPrefs.GetInt("Muted",0)==1;Apply();if(music.clip!=null)music.Play();
        }
        static AudioClip Tone(float frequency,float duration)
        {
            int n=(int)(44100*duration);var data=new float[n];
            for(int i=0;i<n;i++) {float t=i/44100f;float env=Mathf.Min(1,t/.006f)*Mathf.Exp(-t*5/duration);data[i]=(.6f*Mathf.Sin(2*Mathf.PI*frequency*t)+.15f*Mathf.Sin(2*Mathf.PI*frequency*2*t))*env;}
            var clip=AudioClip.Create("Crystal tone",n,1,44100,false);clip.SetData(data,0);return clip;
        }
        public void Play(string cue)
        {
            if(Muted)return;
            AudioClip clip=cue=="move"?move:cue=="rotate"?rotate:cue=="drop"?drop:cue=="clear"?clear:cue=="hold"?hold:end;
            effects.PlayOneShot(clip);
        }
        public void Toggle() {Muted=!Muted;PlayerPrefs.SetInt("Muted",Muted?1:0);Apply();}
        void Apply() {music.mute=effects.mute=Muted;}
    }
}
