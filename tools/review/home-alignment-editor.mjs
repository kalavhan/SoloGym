import {clone, defaults, validate, change, History} from './home-layout.mjs';
const $ = id => document.getElementById(id), canvas = $('room'), ctx = canvas.getContext('2d');
const resources = '/app/Assets/SoloGym/Resources/', storageKey = 'sologym-home-layout-r1';
let room, definitions, catalog, characters, history, base, selected = 'home.training-ring', preview, drag;
const art = new Map();
const status = (message, error = false) => { $('status').textContent = message; $('status').className = error ? 'error' : ''; };
async function json(path) { const r = await fetch(path); if (!r.ok) throw new Error('No se pudo cargar ' + path); return r.json(); }
async function picture(path, crop) {
  const image = new Image(); image.src = path; await image.decode();
  const rect = crop ? {x:crop.x, y:image.height-crop.y-crop.height, width:crop.width, height:crop.height}
    : {x:0,y:0,width:image.width,height:image.height};
  const sample = document.createElement('canvas'); sample.width = rect.width; sample.height = rect.height;
  const c = sample.getContext('2d', {willReadFrequently:true}); c.drawImage(image,rect.x,rect.y,rect.width,rect.height,0,0,rect.width,rect.height);
  const pixels = c.getImageData(0,0,rect.width,rect.height).data;
  let left=rect.width, top=rect.height, right=0, bottom=0;
  for(let y=0;y<rect.height;y++) for(let x=0;x<rect.width;x++) if(pixels[(y*rect.width+x)*4+3]>10){
    left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x+1);bottom=Math.max(bottom,y+1);
  }
  return {image,rect,pixels,bounds:{left,top,right,bottom}};
}
function entries() {
  const data = preview || history.current;
  const items = data.objects.map(state => {
    const d=definitions.find(d=>d.id===state.objectId), k=d.sourceScale*state.scale;
    return {id:state.objectId,state,art:art.get(d.variants.find(v=>v.id===state.variantId).resource),
      left:state.position.x-d.pivot.x*d.sourceSize.x*k,top:state.position.y-(1-d.pivot.y)*d.sourceSize.y*k,
      k,z:state.layer==='foreground'?2:0,order:state.drawOrder};
  });
  const c=characters.find(c=>c.id===$('character').value), s=data.character;
  const half=Math.max(...characters.flatMap(c=>[c.feet.x,c.width-c.feet.x]));
  const height=Math.max(...characters.map(c=>c.height-c.feet.y)), k=Math.min(300/(2*half),500/height)*s.scale;
  items.push({id:'character',state:s,art:art.get(c.resource),left:s.position.x-c.feet.x*k,
    top:s.position.y-(c.height-c.feet.y)*k,k,z:1,order:0});
  return items.sort((a,b)=>a.z-b.z||a.order-b.order||(a.id<b.id?-1:a.id>b.id?1:0));
}
function paintImage(a,left,top,width,height) {
  ctx.drawImage(a.image,a.rect.x,a.rect.y,a.rect.width,a.rect.height,left,top,width,height);
}
function render() {
  if(!history)return;
  ctx.clearRect(0,0,canvas.width,canvas.height);ctx.imageSmoothingEnabled=false;
  const r=room.exteriorRect;
  paintImage(art.get(room.exterior),r.x,r.y,r.width,r.height);
  paintImage(art.get(room.architecture),0,0,canvas.width,canvas.height);
  for(const item of entries()) if(item.state.visible) paintImage(item.art,item.left,item.top,item.art.rect.width*item.k,item.art.rect.height*item.k);
  if($('view').value!=='edit') {
    ctx.globalAlpha=$('view').value==='reference'?1:Number($('opacity').value)/100;
    paintImage(art.get('reference'),0,0,canvas.width,canvas.height);ctx.globalAlpha=1;
  }
  if($('guides').checked && $('view').value!=='reference') {
    const item=entries().find(e=>e.id===selected),b=item.art.bounds,p=item.state.position;
    ctx.strokeStyle='#67fff1';ctx.lineWidth=2;ctx.setLineDash([7,5]);
    ctx.strokeRect(item.left+b.left*item.k,item.top+b.top*item.k,(b.right-b.left)*item.k,(b.bottom-b.top)*item.k);
    ctx.setLineDash([]);ctx.beginPath();ctx.moveTo(p.x-9,p.y);ctx.lineTo(p.x+9,p.y);ctx.moveTo(p.x,p.y-9);ctx.lineTo(p.x,p.y+9);ctx.stroke();
  }
}
const current = () => selected==='character'?history.current.character:history.current.objects.find(s=>s.objectId===selected);
function sync() {
  const s=current();$('object').value=selected;$('identity').textContent=selected;
  $('x').value=Math.round(s.position.x*100)/100;$('y').value=Math.round(s.position.y*100)/100;
  $('scale').value=Math.round(s.scale*10000)/100;$('visible').checked=s.visible;
  $('layer').disabled=$('order').disabled=selected==='character';
  $('layer').value=s.layer||'behind-character';$('order').value=s.drawOrder??0;
  $('undo').disabled=!history.back.length;$('redo').disabled=!history.forward.length;render();
}
function persist() {
  try{localStorage.setItem(storageKey,JSON.stringify(history.current));return true;}
  catch{status('No se pudo guardar el borrador. Exporta JSON para conservar tus cambios.',true);return false;}
}
function commit(next,message='Borrador guardado en este navegador. Exporta JSON para compartirlo.') {
  history.commit(next);preview=null;sync();if(persist())status(message);
}
function patchSelected(patch) {
  try{commit(change(history.current,selected,patch,$('contents').checked,catalog,definitions,room));}
  catch(error){status(error.message,true);sync();}
}
function point(event) {
  const box=canvas.getBoundingClientRect();
  // Fullscreen uses object-fit:contain, so exclude its letterbox from the hit coordinates.
  const k=Math.min(box.width/canvas.width,box.height/canvas.height);
  return {x:(event.clientX-box.left-(box.width-canvas.width*k)/2)/k,y:(event.clientY-box.top-(box.height-canvas.height*k)/2)/k};
}
function hit(p) {
  return entries().reverse().find(e=>{
    if(!e.state.visible)return false;
    const x=Math.floor((p.x-e.left)/e.k),y=Math.floor((p.y-e.top)/e.k),a=e.art;
    return x>=0&&y>=0&&x<a.rect.width&&y<a.rect.height&&a.pixels[(y*a.rect.width+x)*4+3]>10;
  });
}
canvas.addEventListener('pointerdown',event=>{
  if(!history||$('view').value==='reference'||event.button!==0)return;
  const p=point(event),item=hit(p);if(!item)return;
  selected=item.id;sync();canvas.focus();canvas.setPointerCapture(event.pointerId);
  drag={pointer:event.pointerId,start:p,original:clone(history.current),position:clone(item.state.position),changed:false};
});
canvas.addEventListener('pointermove',event=>{
  if(!drag||event.pointerId!==drag.pointer)return;
  const p=point(event),position={x:drag.position.x+p.x-drag.start.x,y:drag.position.y+p.y-drag.start.y};
  try {preview=change(drag.original,selected,{position},$('contents').checked,catalog,definitions,room);drag.changed=true;render();
    $('x').value=position.x.toFixed(1);$('y').value=position.y.toFixed(1);}
  catch(error){status(error.message,true);}
});
function endDrag(cancel=false){
  if(!drag)return;const next=preview,changed=drag.changed;drag=null;preview=null;
  if(!cancel&&changed&&next)commit(next);else sync();
}
canvas.addEventListener('pointerup',()=>endDrag());canvas.addEventListener('pointercancel',()=>endDrag(true));
canvas.addEventListener('lostpointercapture',()=>endDrag(true));window.addEventListener('blur',()=>endDrag(true));
document.addEventListener('keydown',event=>{
  if(!history||event.target.closest('input,select,textarea'))return;
  if((event.ctrlKey||event.metaKey)&&event.key.toLowerCase()==='z'){
    event.preventDefault();(event.shiftKey?$('redo'):$('undo')).click();return;
  }
  if(event.target!==canvas||$('view').value==='reference')return;
  const directions={ArrowLeft:[-1,0],ArrowRight:[1,0],ArrowUp:[0,-1],ArrowDown:[0,1]},delta=directions[event.key];
  if(!delta)return;event.preventDefault();const s=current(),n=event.shiftKey?10:1;
  patchSelected({position:{x:s.position.x+delta[0]*n,y:s.position.y+delta[1]*n}});
});
$('object').addEventListener('change',()=>{endDrag(true);selected=$('object').value;sync();});
for(const id of ['x','y'])$(id).addEventListener('change',()=>patchSelected({position:{x:Number($('x').value),y:Number($('y').value)}}));
$('scale').addEventListener('change',()=>patchSelected({scale:Number($('scale').value)/100}));
$('smaller').onclick=()=>patchSelected({scale:Math.round((current().scale-.05)*100)/100});
$('larger').onclick=()=>patchSelected({scale:Math.round((current().scale+.05)*100)/100});
$('visible').onchange=()=>patchSelected({visible:$('visible').checked});
$('layer').onchange=()=>patchSelected({layer:$('layer').value});$('order').onchange=()=>patchSelected({drawOrder:Number($('order').value)});
for(const id of ['undo','redo'])$(id).onclick=()=>{endDrag(true);history[id]();sync();if(persist())status(id==='undo'?'Cambio deshecho.':'Cambio rehecho.');};
$('reset-item').onclick=()=> {
  const item=selected==='character'?base.character:base.objects.find(s=>s.objectId===selected);
  patchSelected(clone(item));
};
$('reset-all').onclick=()=> {if(confirm('¿Restablecer toda la distribución? Puedes deshacerlo.'))commit(clone(base),'Distribución inicial restaurada. Puedes deshacerlo.');};
$('view').onchange=()=>{endDrag(true);$('opacity-control').hidden=$('view').value!=='overlay';render();};
$('opacity').oninput=()=>{$('opacity-value').value=$('opacity').value+'%';render();};
$('guides').onchange=render;$('character').onchange=render;
$('fullscreen').onclick=async()=>{try{if(document.fullscreenElement)await document.exitFullscreen();else await $('stage').requestFullscreen();}catch{status('No se pudo activar la vista ampliada.',true);}};
$('export').onclick=()=>{
  const text=JSON.stringify(validate(history.current,room,definitions),null,2)+'\n';
  $('json-text').value=text;$('json-panel').hidden=false;
  const url=URL.createObjectURL(new Blob([text],{type:'application/json'})),link=document.createElement('a');
  link.href=url;link.download='sologym-home-layout-'+new Date().toISOString().slice(0,10)+'.json';document.body.append(link);link.click();link.remove();
  setTimeout(()=>URL.revokeObjectURL(url),1000);status('JSON preparado. Si no se descarga, usa Copiar JSON al final de la página.');
};
function importText(text) {
  if(new TextEncoder().encode(text).length>200000)throw new Error('Archivo demasiado grande.');
  const next=validate(JSON.parse(text),room,definitions);
  commit(next,'Distribución importada y guardada como borrador. Puedes deshacerla.');
}
$('copy-json').onclick=async()=>{
  try{await navigator.clipboard.writeText($('json-text').value);status('JSON copiado. Guárdalo o compártelo para aplicar tu distribución.');}
  catch{$('json-text').focus();$('json-text').select();status('Seleccionado. Usa Ctrl/Cmd + C para copiar el JSON.');}
};
$('apply-json').onclick=()=>{
  try{importText($('json-text').value);}
  catch(error){status('Importación rechazada: '+error.message+' Tu distribución no cambió.',true);}
};
$('close-json').onclick=()=>{$('json-panel').hidden=true;};
$('import').onclick=()=> $('file').click();
$('file').onchange=async()=>{
  const file=$('file').files[0];if(!file)return;
  try{if(file.size>200000)throw new Error('Archivo demasiado grande.');importText(await file.text());}
  catch(error){status('Importación rechazada: '+error.message+' Tu distribución no cambió.',true);}
  finally{$('file').value='';}
};
async function start() {
  room=await json(resources+'Rooms/RefugeR1/room.json');
  const list=await json(resources+'Rooms/RefugeR1/objects.json');
  definitions=await Promise.all(list.objects.map(e=>json(resources+e.resource+'.json')));
  catalog=list.objects.map((e,i)=>({...e,id:definitions[i].id}));
  characters=(await json(resources+'Characters/BarbarianR1/catalog.json')).characters;
  await Promise.all([
    ...[room.architecture,room.exterior,...definitions.flatMap(d=>d.variants.map(v=>v.resource))].map(async r=>art.set(r,await picture(resources+r+'.png'))),
    ...characters.map(async c=>art.set(c.resource,await picture(resources+c.resource+'.png',c.sourceRect))),
    picture('/design/fantasy-home-r2/home-approved.png').then(a=>art.set('reference',a))
  ]);
  for(const row of [...catalog,{id:'character',label:'Personaje'}])$('object').add(new Option(row.label,row.id));
  for(const c of characters)$('character').add(new Option(c.id,c.id));$('character').value='male-medium';
  base=defaults(room,definitions);let initial=base,message='Listo. Arrastra un objeto o selecciónalo en la lista.';
  try{const saved=localStorage.getItem(storageKey);if(saved){initial=validate(JSON.parse(saved),room,definitions);message='Borrador anterior restaurado.';}}
  catch{message='El borrador anterior no es compatible. Se muestra la distribución inicial; el borrador no se sobrescribió.';}
  history=new History(initial);$('controls').disabled=false;$('export').disabled=$('import').disabled=false;sync();status(message);
}
start().catch(error=>status('No se pudo abrir el editor: '+error.message,true));
