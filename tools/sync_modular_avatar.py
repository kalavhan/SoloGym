#!/usr/bin/env python3
"""Package the user-accepted MVP subset without modifying any source PNG."""
import argparse
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'assets/sprites/autosprite'
DEST = ROOT / 'app/Assets/SoloGym/Resources/AvatarAutoSprite/modular-front-v1'
PRESETS = {f'{gender}-{build}' for gender in ('female','male') for build in ('slim','medium','overweight','muscular')}

def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()

def triangulate(points):
    def cross(a,b,c): return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
    area=sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(points,points[1:]+points[:1]))
    sign=1 if area>0 else -1
    remaining=list(range(len(points))); triangles=[]
    while len(remaining)>3:
        for pos,b in enumerate(remaining):
            a,c=remaining[pos-1],remaining[(pos+1)%len(remaining)]
            if sign*cross(points[a],points[b],points[c])<=0: continue
            if any(all(sign*cross(points[u],points[v],points[k])>=0 for u,v in [(a,b),(b,c),(c,a)]) for k in remaining if k not in (a,b,c)): continue
            triangles.extend([a,b,c]);remaining.pop(pos);break
        else: raise ValueError('Invalid occlusion polygon')
    triangles.extend(remaining)
    assert abs(sum(abs(cross(points[a],points[b],points[c])) for a,b,c in zip(triangles[::3],triangles[1::3],triangles[2::3]))-abs(area))<.001
    return triangles

def build(sync=False):
    fitting = SOURCE / 'basic-clothing-fit-r3/fitting.json'
    data = json.loads(fitting.read_text())
    images = {}
    def image(record):
        path = SOURCE / record['path']
        assert digest(path) == record['sha256'], path
        resource = 'images/' + record['sha256']
        images[resource] = dict(source=record['path'],sha256=record['sha256'],resource=resource,width=record['width'],height=record['height'])
        return resource
    items = {i['id']:i for i in data['items']}
    hairs = {i['id']:i for i in data['hairstyles']}
    def layer(fit, asset):
        return dict(resource=image(asset['image']),sourceRect=fit['sourceRect'],destinationRect=fit['destinationRect'])
    def polygons(polys):
        return [dict(points=[dict(x=x,y=y) for x,y in p],triangles=triangulate(p)) for p in polys]
    bodies=[]
    for b in data['bodies']:
        if b['id'] not in PRESETS: continue
        f=b['fit']
        bodies.append(dict(id=b['id'],gender=b['gender'],bodyType=b['bodyType'],resource=image(b['image']),
            clothingFitSha256=b['clothingFitSha256'],hairFitSha256=b['hairFitSha256'],
            torso=layer(f['torso'],items[f['torso']['assetId']]),legs=layer(f['legs'],items[f['legs']['assetId']]),
            hair=[dict(id=id,**layer(fit,hairs[id])) for id,fit in b['hairFits'].items()],
            neck=polygons(f['neckOcclusion']),hands=polygons(f['handOcclusion'])))
    assert {b['id'] for b in bodies} == PRESETS and len(bodies)==8
    # One shared canvas/camera for every preset; no body-dependent enlargement.
    catalog=dict(schema='sologym.modular-avatar.v1',artStatus='accepted_for_mvp_with_known_issues',
        sourceFittingSha256=digest(fitting),canvasSize=1024,viewBounds=dict(x=270,y=0,width=480,height=1024),
        excludedBodyIds=['female-obese','male-obese'],tier='base',animated=False,bodies=bodies,images=list(images.values()))
    text=json.dumps(catalog,indent=2)+'\n'
    if sync:
        (DEST/'images').mkdir(parents=True,exist_ok=True)
        for row in images.values(): shutil.copyfile(SOURCE/row['source'],DEST/(row['resource']+'.png'))
        (DEST/'catalog.json').write_text(text)
    else:
        assert (DEST/'catalog.json').read_text()==text,'Run tools/sync_modular_avatar.py --sync'
    for row in images.values(): assert digest(DEST/(row['resource']+'.png')) == row['sha256']
    assert {p.stem for p in (DEST/'images').glob('*.png')} == {i['sha256'] for i in images.values()},'Unexpected packaged sprite'
    print(f'PASS: 8 MVP bodies, {len(images)} unchanged separate sprites, exact r3 fits, no obese presets or animation.')

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--sync',action='store_true');build(parser.parse_args().sync)
