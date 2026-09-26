using UnityEngine;
using UnityEngine.UI;

namespace SoloGym
{
    /// <summary>Small reusable vector emblems; all paths are in a 100-unit design space.</summary>
    public sealed class SystemIcon : MaskableGraphic
    {
        public string kind="sigil";
        VertexHelper mesh;
        Vector2 P(float x,float y){var r=rectTransform.rect;return new Vector2(r.xMin+x*r.width/100,r.yMax-y*r.height/100);}
        void Line(float x,float y,float a,float b,float w=2){SystemPanel.Line(mesh,P(x,y),P(a,b),color,Mathf.Max(1.2f,w*rectTransform.rect.width/100));}
        void Path(params float[] p){for(int i=0;i<p.Length-2;i+=2)Line(p[i],p[i+1],p[i+2],p[i+3]);}
        void Circle(float cx,float cy,float rx,float ry){for(int i=0;i<48;i++){float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;Line(cx+Mathf.Cos(a)*rx,cy+Mathf.Sin(a)*ry,cx+Mathf.Cos(b)*rx,cy+Mathf.Sin(b)*ry);}}
        protected override void OnPopulateMesh(VertexHelper v)
        {
            v.Clear();mesh=v;
            switch(kind)
            {
                case "back":Path(68,5,22,50,68,95);break;
                case "down":Path(15,35,50,70,85,35);break;
                case "globe":Circle(50,50,46,46);Circle(50,50,23,46);Line(4,50,96,50);Line(13,25,87,25);Line(13,75,87,75);break;
                case "lock":Path(23,45,23,94,77,94,77,45,23,45);Path(32,45,32,26,39,15,61,15,68,26,68,45);Circle(50,65,4,4);Line(50,67,50,80,4);break;
                case "shield":case "guard":Path(50,5,88,18,83,59,70,80,50,96,30,80,17,59,12,18,50,5);Path(50,16,77,26,71,58,50,82,29,58,23,26,50,16);break;
                case "train":case "dumbbell":Line(7,25,7,75,7);Line(18,13,18,87,9);Line(33,30,33,70,7);Line(33,50,67,50,8);Line(67,30,67,70,7);Line(82,13,82,87,9);Line(93,25,93,75,7);break;
                case "tower":Path(48,3,43,19,31,25,39,29,35,67,21,84,5,96,50,86,95,96,79,84,65,67,61,29,69,25,57,19,52,3);Path(50,15,50,76,18,92);Path(50,76,82,92);break;
                case "gym":Path(7,20,24,25,76,25,93,20);Path(7,29,24,33,76,33,93,29);Line(15,45,85,45,6);Line(29,33,19,91,7);Line(71,33,81,91,7);break;
                case "head":Path(50,7,29,18,10,51,19,70,50,91,81,70,90,51,71,18,50,7);Path(19,70,35,45,50,33,65,45,81,70);Line(50,7,50,33);break;
                case "torso":Path(30,10,16,20,5,43,21,50,26,32,24,90,76,90,74,32,79,50,95,43,84,20,70,10,61,24,39,24,30,10);Path(26,45,45,50,50,72,55,50,74,45);break;
                case "legs":Path(28,9,72,9,83,92,59,92,50,45,41,92,17,92,28,9);Line(27,20,73,20);Line(50,20,50,42);break;
                case "feet":Path(24,8,47,8,44,63,59,76,59,91,7,91,7,78,21,65,24,8);Path(66,8,88,8,85,62,98,77,98,91,64,91,64,76,62,65,66,8);for(int i=0;i<4;i++){Line(27,27+i*8,42,29+i*8);Line(69,27+i*8,83,29+i*8);}break;
                case "backpack":case "backgear":Path(22,27,35,18,65,18,78,27,87,90,13,90,22,27);Path(38,18,38,8,62,8,62,18);Path(25,48,75,48,77,82,23,82,25,48);Line(25,59,75,59);break;
                case "hands":case "power":Path(22,92,22,65,10,48,10,35,21,30,31,47,29,14,39,10,46,39,48,8,59,8,62,40,66,14,77,18,75,49,83,30,94,35,86,70,72,92,22,92);Line(26,78,76,78,3);break;
                case "focus":Circle(50,48,31,37);Path(50,9,50,93);Path(42,24,29,37,43,47,31,58,42,72);Path(58,24,71,37,57,47,69,58,58,72);break;
                case "flame":Path(48,3,53,28,73,45,78,66,65,86,50,98,30,84,21,64,26,47,35,64,41,46,38,26,48,3);Path(50,96,42,74,49,57,58,78,50,96);break;
                case "coin":Circle(50,50,44,44);Circle(50,50,37,37);Path(58,20,39,30,39,46,60,54,60,70,39,81);Line(49,13,49,88);break;
                case "clock":Circle(50,50,44,44);Path(50,18,50,52,72,52);break;
                case "warning":Circle(50,50,44,44);Line(50,20,50,60,4);Circle(50,77,2,2);break;
                case "gear":for(int i=0;i<12;i++){float a=i*Mathf.PI/6,b=a+.25f;Line(50+Mathf.Cos(a)*38,50+Mathf.Sin(a)*38,50+Mathf.Cos(a)*48,50+Mathf.Sin(a)*48,8);Line(50+Mathf.Cos(a)*38,50+Mathf.Sin(a)*38,50+Mathf.Cos(b)*38,50+Mathf.Sin(b)*38,4);}Circle(50,50,22,22);break;
                default:Path(50,2,61,35,91,50,61,65,50,98,39,65,9,50,39,35,50,2);Path(50,20,56,42,72,50,56,58,50,80,44,58,28,50,44,42,50,20);break;
            }
        }
    }
}
