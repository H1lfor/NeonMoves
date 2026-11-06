using System;
using UnityEngine;

namespace Neonmoves
{
    public class SynthMusic : MonoBehaviour
    {
        AudioSource music,fx;
        AudioClip hit,miss;
        public void Create(float volume)
        {
            music=gameObject.AddComponent<AudioSource>(); fx=gameObject.AddComponent<AudioSource>(); music.loop=true; music.volume=volume*.5f; fx.volume=volume*.6f;
            music.clip=Compose(); hit=Tone(880,.12f); miss=Tone(110,.18f); music.Play();
        }
        static AudioClip Tone(float hz,float length)
        {
            int rate=24000; float[] data=new float[(int)(rate*length)];
            for(int i=0;i<data.Length;i++){float t=i/(float)rate;data[i]=Mathf.Sin(t*hz*2*Mathf.PI)*Mathf.Exp(-t*25)*.3f;}
            var clip=AudioClip.Create("Signal",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        static AudioClip Compose()
        {
            const int rate=24000; const float beat=60f/112; int count=(int)(beat*64*rate);var data=new float[count];
            int[] bass={40,40,43,43,36,36,38,38}; int[] arp={0,7,12,7,3,10,15,10};var random=new System.Random(42);
            for(int i=0;i<count;i++) {
                float t=i/(float)rate,b=t/beat; int beatIndex=(int)b; float q=(b-beatIndex)*beat;
                float kick=Mathf.Sin(2*Mathf.PI*(48*q+7*(1-Mathf.Exp(-q*25))))*Mathf.Exp(-q*13)*.40f;
                float snare=beatIndex%2==1?(float)(random.NextDouble()*2-1)*Mathf.Exp(-q*30)*.15f:0;
                float eighth=(b*2-Mathf.Floor(b*2))*beat*.5f;
                float hat=(float)(random.NextDouble()*2-1)*Mathf.Exp(-eighth*100)*.055f;
                float freq=440*Mathf.Pow(2,(bass[(beatIndex/8)%8]-69)/12f);
                float bassTone=(Mathf.Sin(t*freq*2*Mathf.PI)+Mathf.Sin(t*freq*4*Mathf.PI)*.25f)*.15f*Mathf.Min(q*30,1);
                float note=440*Mathf.Pow(2,(bass[(beatIndex/8)%8]+24+arp[(int)(b*2)%8]-69)/12f);
                float lead=(Mathf.Sin(t*note*2*Mathf.PI)+Mathf.Sin(t*note*4*Mathf.PI)*.2f)*Mathf.Exp(-eighth*10)*.065f;
                float fade=Mathf.Min(1,t*8)*Mathf.Min(1,(count-i)/(float)rate*8);
                data[i]=(kick+snare+hat+bassTone+lead)*fade;
            }
            var clip=AudioClip.Create("Neonmoves — Signal Circuit / original procedural soundtrack",count,1,rate,false);clip.SetData(data,0);return clip;
        }
        public void SetVolume(float value){if(music)music.volume=value*.5f;if(fx)fx.volume=value*.6f;}
        public void SetPlaying(bool playing,int stage,float difficulty=1){if(music)music.pitch=playing?(1+stage*.06f)*difficulty:1;}
        public void RestartBeat(){if(music){music.time=0;music.Play();}}
        public void Hit(bool success){if(fx)fx.PlayOneShot(success?hit:miss);}
        public void Pause(bool pause){if(music){if(pause)music.Pause();else music.UnPause();}}
    }
}
