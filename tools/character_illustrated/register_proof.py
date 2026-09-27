"""Export reviewed pixel landmarks to the Unity proof contract; never edits pixels.

Coordinates below are authored from the saved r1 atlases. Re-running is deterministic.
Screen-left/right bone suffixes are deliberate; they are not anatomical side labels.
"""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'app/Assets/SoloGym/Resources/AvatarIllustrated/Registration.json'
NAMES = ['pelvis','chest','neck','head','shoulder_l','elbow_l','hand_l','shoulder_r','elbow_r','hand_r','hip_l','knee_l','foot_l','hip_r','knee_r','foot_r']
PARENTS = ['', 'pelvis','chest','neck','chest','shoulder_l','elbow_l','chest','shoulder_r','elbow_r','pelvis','hip_l','knee_l','pelvis','hip_r','knee_r']
RADII = [.12,.115,.045,.072,.058,.050,.043,.058,.05,.043,.075,.057,.055,.075,.057,.055]

# Each cell has an independently measured neutral registration; no limb rescaling.
DATA = [
 ('male','studio','MaleStudio',[
  [(460,455),(458,270),(475,177),(480,110),(349,224),(313,351),(313,515),(552,225),(588,355),(610,520),(405,459),(363,657),(328,836),(518,459),(547,661),(563,837)],
  [(1060,455),(1060,270),(1067,177),(1071,110),(946,222),(908,352),(910,515),(1160,225),(1200,356),(1220,522),(1000,465),(965,662),(930,836),(1120,465),(1166,663),(1173,837)]
 ],[970,970]),
 ('female','studio','FemaleStudio',[
  [(538,451),(530,268),(527,190),(528,123),(427,234),(390,349),(370,526),(601,243),(632,357),(672,526),(475,460),(447,655),(390,837),(590,460),(599,659),(625,837)],
  [(1004,451),(1001,270),(1003,189),(1006,123),(901,235),(868,352),(858,525),(1083,243),(1116,357),(1158,523),(945,464),(907,658),(866,837),(1058,464),(1110,658),(1124,837)]
 ],[990,990]),
 ('male','gameplay','MaleGameplay',[
  [(491,477),(488,285),(488,196),(487,128),(377,258),(337,395),(348,564),(572,235),(610,343),(648,488),(443,485),(399,665),(364,823),(549,475),(574,645),(594,781)],
  [(1062,478),(1060,290),(1060,196),(1068,128),(947,260),(907,397),(927,565),(1156,239),(1189,344),(1229,489),(1015,488),(973,666),(948,823),(1122,477),(1140,645),(1180,781)]
 ],[985,985]),
 ('female','gameplay','FemaleGameplay',[
  [(540,463),(533,293),(527,216),(526,142),(428,274),(387,389),(370,555),(591,236),(632,360),(679,508),(478,477),(456,655),(391,823),(590,471),(604,650),(625,801)],
  [(1080,463),(1078,293),(1066,216),(1066,142),(966,274),(925,392),(915,560),(1132,239),(1174,360),(1217,508),(1014,480),(983,657),(939,823),(1129,471),(1144,648),(1177,801)]
 ],[998,998]),
]

def polygon(region, points, origin):
    return {'region':region,'points':[round((v-origin if i%2==0 else v)/(768 if i%2==0 else 1024),7) for i,v in enumerate(points)]}

def rect(region,x0,y0,x1,y1,origin):
    return polygon(region,[x0,y0,x1,y0,x1,y1,x0,y1],origin)

def masks(points, gender, view, equipped, origin):
    p=dict(zip(NAMES,points)); result=[]
    # Authored regions narrow the semantic mask; hue/chroma gating protects ink and cloth.
    hx,hy=p['head']; nx,ny=p['neck']; cx,cy=p['chest']; px,py=p['pelvis']
    result.append(rect('skin',hx-67,hy-70,hx+64,ny+57,origin))
    if not equipped:
        result.append(polygon('skin',[cx-110,ny-18,cx+108,ny-8,px+98,py-46,px-100,py-46],origin))
    for side in ['l','r']:
        sx,sy=p['shoulder_'+side]; ex,ey=p['elbow_'+side]; wx,wy=p['hand_'+side]
        # The elevated female near shoulder extends above its joint landmark.
        # Cover its complete painted cap so deep tones have no peach top edge.
        cap_height=60 if gender=='female' and view=='gameplay' and side=='l' else 39
        result.append(polygon('skin',[sx-49,sy-cap_height,sx+47,sy-cap_height+1,ex+43,ey+51,ex-43,ey+51],origin))
        result.append(rect('skin',wx-45,wy-48,wx+44,wy+38,origin))
        if not equipped:
            tx,ty=p['hip_'+side]; kx,ky=p['knee_'+side]; fx,fy=p['foot_'+side]
            result.append(polygon('skin',[tx-75,ty+100,tx+73,ty+100,kx+62,ky+41,fx+39,fy-28,fx-42,fy-28,kx-55,ky+36],origin))
    if gender=='male':
        result.append(polygon('hair',[hx-79,hy-38,hx-58,hy-85,hx+9,hy-107,hx+65,hy-74,hx+72,hy-34,hx+20,hy-42,hx-25,hy-26,hx-37,hy+18,hx-59,hy+9],origin))
    else:
        result.append(polygon('hair',[hx-115,hy-39,hx-100,hy-90,hx-46,hy-118,hx+14,hy-119,hx+54,hy-77,hx+64,hy-19,hx+54,hy+45,hx+33,hy+31,hx+34,hy-33,hx+8,hy-34,hx-35,hy-13,hx-56,hy+34,hx-77,hy+76,hx-106,hy+80,hx-116,hy+48],origin))
    return result

def main():
    underlaps=json.loads(Path(__file__).with_name('cloth_underlaps.json').read_text())
    region_data={}
    for gender in ['male','female']:
        path=Path(__file__).with_name(gender+'_regions.json')
        if path.exists(): region_data.update(json.loads(path.read_text()))
    entries=[]
    for gender,view,resource,cells,ground in DATA:
        path=OUT.parent/(resource+'.png')
        entry={'id':f'{gender}_normal_{view}','presentation':gender,'view':view,'resource':'AvatarIllustrated/'+resource,'source_sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'cells':[]}
        for i,points in enumerate(cells):
            origin=768*i; gx=points[0][0]
            entry['cells'].append({'id':'equipped' if i else 'base','rect':[origin,0,768,1024],
              'pivot':[(gx-origin)/768,ground[i]/1024],'ground':[(gx-origin)/768,ground[i]/1024],
              'skinSource':[.78,.51,.35],'hairSource':[.045,.041,.05],
              'joints':[{'id':n,'parent':parent,'point':[(xy[0]-origin)/768,xy[1]/1024],'radius':r} for n,parent,xy,r in zip(NAMES,PARENTS,points,RADII)],
              'maskPolygons':masks(points,gender,view,bool(i),origin)})
        for cell in entry['cells']:
            regions=region_data.get(entry['id'],{}).get(cell['id'],[])
            cell['deformRegions']=[{'id':r['id'],'points':polygon('skin',r['points'],cell['rect'][0])['points']} for r in regions]
            cell['underlaps']=[{'id':r['id'],'points':polygon('skin',r['points'],cell['rect'][0])['points'],
                               'uvOffset':[r['offsetPixels'][0]/768,r['offsetPixels'][1]/1024]}
                              for r in underlaps.get(entry['id'],{}).get(cell['id'],[])]
        entries.append(entry)
    result={'schema':'sologym.illustrated-rig.v1','sourceRevision':'playable-proof-r1','rigId':'illustrated_normal_v1',
      'approval':'derived proof pending user review; approved appearance masters remain immutable',
      'coordinateSystem':'top-left image pixels; joint suffix is screen side; foot joints are ankles',
      'entries':entries}
    OUT.parent.mkdir(parents=True,exist_ok=True)
    OUT.write_text(json.dumps(result,indent=2)+'\n')
    print(f'Exported {len(entries)} views / 8 registered surfaces to {OUT}')

if __name__=='__main__':
    main()
