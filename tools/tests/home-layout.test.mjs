import assert from 'node:assert/strict';
import {readFile, mkdir, writeFile} from 'node:fs/promises';
import {fileURLToPath} from 'node:url';
import {resolve} from 'node:path';
import {defaults, change, validate, clone, History, footprint} from '../review/home-layout.mjs';
const root=fileURLToPath(new URL('../../',import.meta.url)),res=resolve(root,'app/Assets/SoloGym/Resources');
const json=async p=>JSON.parse(await readFile(p,'utf8'));
const room=await json(resolve(res,'Rooms/RefugeR1/room.json')),list=await json(resolve(res,'Rooms/RefugeR1/objects.json'));
const defs=await Promise.all(list.objects.map(e=>json(resolve(res,e.resource+'.json'))));
const catalog=list.objects.map((e,i)=>({...e,id:defs[i].id})),base=defaults(room,defs);
let passed=0;
function check(name,fn){fn();passed++;console.log('PASS '+name);}
const find=(layout,id)=>layout.objects.find(e=>e.objectId===id);
check('default layout includes all unique objects and survives JSON',()=>assert.deepEqual(validate(JSON.parse(JSON.stringify(base)),room,defs),base));
const moved=change(base,'home.training-bench',{position:{x:501,y:795},scale:1.1},true,catalog,defs,room);
check('bench movement scales contents about its pivot',()=>{
  for(const id of ['home.training-bottle','home.training-towel']){
    const old=find(base,id),next=find(moved,id);
    assert.ok(Math.abs(next.position.x-(501+(old.position.x-451)*1.1))<1e-6);
    assert.ok(Math.abs(next.position.y-(795+(old.position.y-785)*1.1))<1e-6);
    assert.equal(next.scale,1.1);
  }
});
check('grouping does not move unrelated objects or mutate input',()=>{
  assert.deepEqual(find(base,'home.training-bench').position,{x:451,y:785});
  assert.deepEqual(find(moved,'home.writing-desk'),find(base,'home.writing-desk'));
});
check('individual adjustment leaves contents alone',()=>{
  const next=change(base,'home.training-bench',{position:{x:501,y:795}},false,catalog,defs,room);
  assert.deepEqual(find(next,'home.training-bottle'),find(base,'home.training-bottle'));
});
check('rejected grouped edit is atomic',()=>{
  const before=JSON.stringify(moved);
  assert.throws(()=>change(moved,'home.training-bench',{position:{x:-500,y:795}},true,catalog,defs,room));
  assert.equal(JSON.stringify(moved),before);
});
for(const [name,mutate] of [
  ['wrong room',d=>d.roomId='other'],['unsupported version',d=>d.version=99],
  ['missing object',d=>d.objects.pop()],['duplicate object',d=>d.objects[0]=d.objects[1]],
  ['unknown object',d=>d.objects[0].objectId='unknown'],['wrong slot',d=>d.objects[0].slotId='hero.feet'],
  ['unknown variant',d=>d.objects[0].variantId='winter'],['invalid layer',d=>d.objects[0].layer='wall'],
  ['outside room',d=>d.objects[0].position.x=-9999],['zero scale',d=>d.objects[0].scale=0],
  ['nonfinite position',d=>d.objects[0].position.x=NaN],['fractional order',d=>d.objects[0].drawOrder=.5],
  ['nonboolean visibility',d=>d.objects[0].visible='yes'],['oversized character',d=>d.character.scale=3],
]){
  check('reject '+name,()=>{const candidate=clone(base);mutate(candidate);assert.throws(()=>validate(candidate,room,defs));});
}
check('undo/redo restores whole grouped transaction',()=>{
  const h=new History(base);h.commit(moved);assert.deepEqual(h.undo(),base);assert.deepEqual(h.redo(),moved);
  h.undo();h.commit(change(base,'home.trophy',{visible:false},false,catalog,defs,room));assert.equal(h.forward.length,0);
});
check('state placement transforms the footprint, preserving proportions',()=>{
  const d=defs.find(d=>d.id==='home.training-bench'),a=footprint(find(base,d.id),d),b=footprint(find(moved,d.id),d);
  assert.ok(Math.abs((b[1].x-b[0].x)/(a[1].x-a[0].x)-1.1)<1e-6);
});
let fixture=change(moved,'home.training-ring',{position:{x:468,y:570},scale:.9},false,catalog,defs,room);
fixture=change(fixture,'character',{position:{x:818,y:716},scale:.9},false,catalog,defs,room);
fixture=change(fixture,'home.trophy',{visible:false},false,catalog,defs,room);
check('edited fixture survives export/import including hidden items',()=>assert.deepEqual(validate(JSON.parse(JSON.stringify(fixture)),room,defs),fixture));
if(process.argv.includes('--write-fixtures')){
  const out=resolve(root,'artifacts/visual/HomeAlignment');await mkdir(out,{recursive:true});
  await writeFile(resolve(out,'edited-layout.json'),JSON.stringify(fixture,null,2)+'\n');
  await writeFile(resolve(out,'default-layout.json'),JSON.stringify(base,null,2)+'\n');
}
console.log(passed+' checks passed.');
