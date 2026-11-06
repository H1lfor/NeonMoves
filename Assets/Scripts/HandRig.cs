using UnityEngine;

namespace Neonmoves
{
    // Thumb, index, middle, ring, little finger; every finger has three bending joints.
    public sealed class HandRig
    {
        readonly Transform root;
        readonly Transform[,] joints=new Transform[5,3];
        readonly float side;
        public HandRig(Transform forearm,float side,Material glove,Material accent)
        {
            this.side=side;
            root=new GameObject("Articulated hand").transform;root.SetParent(forearm,false);
            root.localPosition=new Vector3(0,-.44f,-.015f);
            MeshArt.Shape("Palm",root,new Vector3(0,-.065f,0),new Vector3(.20f,.15f,.095f),glove,8);
            for(int finger=0;finger<5;finger++) {
                float length=finger==0?.045f:finger==2?.057f:finger==4?.043f:.051f;
                Transform parent=root;
                for(int joint=0;joint<3;joint++) {
                    var pivot=new GameObject("Finger "+finger+" joint "+joint).transform;
                    pivot.SetParent(parent,false);
                    pivot.localPosition=joint>0?new Vector3(0,-length,0):finger==0?new Vector3(-side*.091f,-.055f,0):new Vector3(side*(-.065f+(finger-1)*.043f),-.135f,0);
                    MeshArt.Shape("Finger segment",pivot,new Vector3(0,-length*.5f,0),new Vector3(.034f,length,.039f),joint==2?accent:glove,6);
                    joints[finger,joint]=pivot;parent=pivot;
                }
            }
        }
        public void Animate(string gesture,float blend)
        {
            var definition=GameRules.Gesture(gesture);
            root.localRotation=Quaternion.Slerp(root.localRotation,Quaternion.Euler(0,0,gesture=="Thumb_Up"?side*65:0),blend);
            for(int finger=0;finger<5;finger++)for(int joint=0;joint<3;joint++) {
                bool open=definition.fingers[finger]==1;
                float spread=joint!=0?0:finger==0?-side*65:gesture=="Victory"?(finger==1?-side*14:finger==2?side*12:0):side*(finger-2.5f)*5;
                float bend=open?0:joint==0?70:joint==1?95:65;
                joints[finger,joint].localRotation=Quaternion.Slerp(joints[finger,joint].localRotation,Quaternion.Euler(bend,0,spread),blend);
            }
        }
    }
}
