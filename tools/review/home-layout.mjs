// Pure layout operations shared by the browser editor and Node verification.
export const clone = value => JSON.parse(JSON.stringify(value));
export function defaults(room, definitions) {
  const anchors = new Map(room.anchors.map(a => [a.id, a.point]));
  return {format: 'sologym-home-layout', version: 1, roomId: room.id, referenceSize: clone(room.referenceSize),
    objects: definitions.map(d => ({version: 2, roomId: room.id, objectId: d.id, slotId: d.supportedSlots[0],
      variantId: d.variants[0].id, visible: true, position: clone(anchors.get(d.supportedSlots[0])),
      scale: 1, layer: d.layer, drawOrder: d.drawOrder})),
    character: {position: clone(anchors.get('hero.feet')), scale: 1, visible: true}};
}
export function footprint(state, definition) {
  const k = definition.sourceScale * state.scale;
  return definition.footprint.map(p => ({
    x: state.position.x + (p.x - definition.pivot.x * definition.sourceSize.x) * k,
    y: state.position.y + (p.y - (1 - definition.pivot.y) * definition.sourceSize.y) * k
  }));
}
export function validate(value, room, definitions) {
  const fail = reason => { throw new Error(reason); };
  const finite = v => typeof v === 'number' && Number.isFinite(v);
  const point = p => p && finite(p.x) && finite(p.y);
  if (!value || value.format !== 'sologym-home-layout' || value.version !== 1 || value.roomId !== room.id ||
    !point(value.referenceSize) || value.referenceSize.x !== room.referenceSize.x || value.referenceSize.y !== room.referenceSize.y)
    fail('Formato, versión o habitación incorrectos.');
  if (!Array.isArray(value.objects) || value.objects.length !== definitions.length)
    fail('El archivo debe incluir todos los objetos, una sola vez.');
  const defs = new Map(definitions.map(d => [d.id, d])), seen = new Set(), slots = new Set();
  for (const state of value.objects) {
    const d = defs.get(state?.objectId);
    if (!d || seen.has(d.id) || state.version !== 2 || state.roomId !== room.id ||
      !d.supportedSlots.includes(state.slotId) || slots.has(state.slotId) || !d.variants.some(v => v.id === state.variantId))
      fail('Objeto, ubicación o variante desconocidos/duplicados.');
    seen.add(d.id); slots.add(state.slotId);
    if (!point(state.position) || !finite(state.scale) || state.scale < .25 || state.scale > 3 ||
      typeof state.visible !== 'boolean' || !['behind-character', 'foreground'].includes(state.layer) ||
      !Number.isInteger(state.drawOrder) || state.drawOrder < -10000 || state.drawOrder > 10000)
      fail('Posición, tamaño, visibilidad o capa no válidos.');
    if (footprint(state, d).some(p => p.x < 0 || p.y < 0 || p.x > room.referenceSize.x || p.y > room.referenceSize.y))
      fail('El objeto saldría de la habitación.');
  }
  const c = value.character;
  if (!c || !point(c.position) || !finite(c.scale) || c.scale < .25 || c.scale > 3 || typeof c.visible !== 'boolean' ||
    c.position.x - 150*c.scale < 0 || c.position.x + 150*c.scale > room.referenceSize.x ||
    c.position.y - 500*c.scale < 0 || c.position.y > room.referenceSize.y)
    fail('El encuadre del personaje saldría de la habitación.');
  return clone(value);
}
export function change(layout, id, patch, withContents, catalog, definitions, room) {
  const next = clone(layout), entry = id === 'character' ? next.character : next.objects.find(s => s.objectId === id);
  if (!entry) throw new Error('Selecciona un objeto.');
  const previous = clone(entry);
  Object.assign(entry, patch);
  if (withContents && id !== 'character' && (patch.position || patch.scale !== undefined)) {
    const ratio = entry.scale / previous.scale;
    const affected = new Set([id]);
    // Transitive grouping remains deterministic; the catalog only contains authored relationships.
    for (let i = 0; i < catalog.length; i++)
      for (const row of catalog) if (affected.has(row.supportObjectId)) affected.add(row.id);
    for (const child of next.objects) if (child.objectId !== id && affected.has(child.objectId)) {
      child.position = {x: entry.position.x + (child.position.x - previous.position.x)*ratio,
        y: entry.position.y + (child.position.y - previous.position.y)*ratio};
      child.scale *= ratio;
    }
  }
  return validate(next, room, definitions);
}
export class History {
  constructor(initial) { this.current = clone(initial); this.back = []; this.forward = []; }
  commit(next) {
    if (JSON.stringify(next) === JSON.stringify(this.current)) return;
    this.back.push(clone(this.current)); if (this.back.length > 100) this.back.shift();
    this.current = clone(next); this.forward = [];
  }
  undo() { if (this.back.length) { this.forward.push(this.current); this.current = this.back.pop(); } return this.current; }
  redo() { if (this.forward.length) { this.back.push(this.current); this.current = this.forward.pop(); } return this.current; }
}
