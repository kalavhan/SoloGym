"use strict";

const PRESETS = [
  ["skinny", "Skinny"], ["normal", "Normal"], ["chubby", "Chubby"],
  ["fat", "Fat"], ["skinny_muscular", "Skinny muscular"],
  ["muscular", "Muscular"], ["fat_muscular", "Fat muscular"]
];
const GENDERS = [["male", "Man"], ["female", "Woman"]];
const HASH_PATTERN = /^[a-f0-9]{64}$/;
const ASSET_CATALOG = window.SOLOGYM_APPROVAL_ASSETS;
const CATALOG_VALID = Boolean(ASSET_CATALOG && ASSET_CATALOG.schema_version === 1 &&
  ASSET_CATALOG.batch_id === "approval-r1" && HASH_PATTERN.test(ASSET_CATALOG.manifest_sha256) &&
  HASH_PATTERN.test(ASSET_CATALOG.style_reference_sha256) && ASSET_CATALOG.proposals &&
  typeof ASSET_CATALOG.proposals === "object");
const PROPOSALS = GENDERS.flatMap(([gender, label]) => PRESETS.map(([preset, name]) => {
  const id = `${gender}-${preset}`;
  const source = CATALOG_VALID ? ASSET_CATALOG.proposals[id] : null;
  const bound = Boolean(source && typeof source.asset_id === "string" && source.asset_id.length &&
    Number.isInteger(source.revision) && source.revision > 0 && HASH_PATTERN.test(source.sha256) &&
    typeof source.image === "string" && /^images\/[a-z0-9_-]+\.png$/i.test(source.image));
  return {
    id, gender, genderLabel: label, preset, name, title: `${label} · ${name}`, bound,
    asset_id: bound ? source.asset_id : null,
    revision: bound ? source.revision : null,
    sha256: bound ? source.sha256 : null,
    image: bound ? source.image : `images/${id}.png`,
    imported_review: bound ? source.review : null,
    previous_review: bound ? source.previous_review : null
  };
}));
const STORAGE_KEY = "sologym-character-art-review-r1";
const STATUS_LABELS = {pending: "Pending approval", changes: "Changes requested", approve: "Approved locally"};
let review = {};
let storageAvailable = true;
try { review = JSON.parse(localStorage.getItem(STORAGE_KEY) || "{}"); } catch (_) { storageAvailable = false; }
if (!review || typeof review !== "object" || Array.isArray(review)) review = {};
let genderFilter = "all";
let currentProposal = null;
let zoom = 1;
let toastTimeout;

const $ = id => document.getElementById(id);
function el(tag, className, text) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}
function sourceOf(proposal) {
  return {asset_id: proposal.asset_id, revision: proposal.revision, image: proposal.image, sha256: proposal.sha256};
}
function reviewKey(proposal) {
  return proposal.bound ? `${proposal.id}@${proposal.sha256}` : null;
}
function validatedReview(value, proposal, origin) {
  if (!proposal.bound || !value || typeof value !== "object" ||
      !Object.hasOwn(STATUS_LABELS, value.status) || !value.reviewed_source ||
      value.reviewed_source.sha256 !== proposal.sha256) return null;
  return {
    status: value.status,
    notes: typeof value.notes === "string" ? value.notes : "",
    updatedAt: typeof value.updatedAt === "string" && Number.isFinite(Date.parse(value.updatedAt)) ? value.updatedAt : null,
    reviewed_source: value.reviewed_source,
    review_origin: origin
  };
}
function reviewTime(value) {
  return value?.updatedAt ? Date.parse(value.updatedAt) : -Infinity;
}
function entry(id) {
  const proposal = PROPOSALS.find(p => p.id === id);
  const key = reviewKey(proposal);
  const local = validatedReview(key ? review[key] : null, proposal, "local");
  const imported = validatedReview(proposal.imported_review, proposal, "imported");
  if (local && (!imported || reviewTime(local) > reviewTime(imported))) return local;
  return imported || {status: "pending", notes: "", updatedAt: null, reviewed_source: null, review_origin: "unreviewed"};
}
function previousRequestedChange(proposal) {
  const previous = proposal.previous_review;
  return previous && previous.status === "changes" && previous.reviewed_source &&
    HASH_PATTERN.test(previous.reviewed_source.sha256) && previous.reviewed_source.sha256 !== proposal.sha256 ? previous : null;
}
function reviewLabel(value) {
  return value.status === "approve" && value.review_origin === "imported" ? "Approved" : STATUS_LABELS[value.status];
}
function reviewOriginText(value) {
  return value.review_origin === "imported" ? "Imported review · saved with this gallery" :
    value.review_origin === "local" ? "Local review · export to share your changes" : "This image is awaiting your review";
}
function needsReview(proposal) {
  return entry(proposal.id).status !== "approve";
}
function save(id, field, value) {
  const proposal = PROPOSALS.find(p => p.id === id);
  const key = reviewKey(proposal);
  if (!key) return;
  review[key] = {...entry(id), [field]: value, updatedAt: new Date().toISOString(), reviewed_source: sourceOf(proposal), review_origin: "local"};
  try { localStorage.setItem(STORAGE_KEY, JSON.stringify(review)); } catch (_) {
    if (storageAvailable) showToast("Browser storage is unavailable. Export JSON to keep your review.");
    storageAvailable = false;
  }
  updateCount();
}
function showToast(message) {
  clearTimeout(toastTimeout);
  $("toast").textContent = message;
  $("toast").hidden = false;
  toastTimeout = setTimeout(() => { $("toast").hidden = true; }, 5000);
}
function artwork(proposal, eager = false) {
  const img = el("img");
  img.alt = `${proposal.title}: body, equipped character and gameplay art proposal`;
  img.src = proposal.image;
  img.loading = eager ? "eager" : "lazy";
  img.decoding = "async";
  img.draggable = false;
  img.addEventListener("error", () => {
    img.hidden = true;
    img.insertAdjacentElement("afterend", el("span", "missing-art", "Artwork is being prepared"));
  }, {once: true});
  return img;
}
function renderGallery() {
  $("gallery").replaceChildren();
  const presetFilter = $("preset-filter").value;
  const onlyNeedsReview = $("review-filter").value === "needs-review";
  const visible = PROPOSALS.filter(p => (genderFilter === "all" || p.gender === genderFilter) &&
    (presetFilter === "all" || p.preset === presetFilter) && (!onlyNeedsReview || needsReview(p)));
  visible.forEach((proposal, index) => {
    const stored = entry(proposal.id);
    const card = el("article", "character-card");
    card.id = proposal.id;
    const title = el("div", "card-title");
    const heading = el("div");
    heading.append(el("p", "eyebrow", `${proposal.genderLabel.toUpperCase()} · ${String(PRESETS.findIndex(p => p[0] === proposal.preset) + 1).padStart(2, "0")} / 07`), el("h3", "", proposal.name));
    const badge = el("span", "status-badge", proposal.bound ? reviewLabel(stored) : "Revision data unavailable");
    badge.dataset.status = stored.status;
    title.append(heading, badge);
    const open = el("button", "artboard-button");
    open.type = "button";
    open.setAttribute("aria-label", `Inspect full artboard: ${proposal.title}`);
    open.append(artwork(proposal, index < 2), el("span", "image-hint", "OPEN FULL ARTBOARD ↗"));
    open.addEventListener("click", () => openArtboard(proposal));
    const fields = el("div", "card-review");
    const previous = previousRequestedChange(proposal);
    if (previous) {
      const feedback = el("aside", "previous-review");
      feedback.setAttribute("aria-label", `Requested change for the previous ${proposal.title} image`);
      const priorRevision = Number.isInteger(previous.reviewed_source.revision) ? ` · r${previous.reviewed_source.revision}` : "";
      feedback.append(el("p", "previous-review-label", `Previous requested change${priorRevision}`),
        el("p", "previous-review-notes", typeof previous.notes === "string" && previous.notes.trim() ? previous.notes : "Changes were requested for the previous image."),
        el("p", "previous-review-context", "Saved from the previous image; this revision is reviewed separately."));
      if (typeof previous.reviewed_source.image === "string" && /^images\/[a-z0-9_-]+\.png$/i.test(previous.reviewed_source.image)) {
        const priorImage = el("a", "", "View previous image ↗");
        priorImage.href = previous.reviewed_source.image;
        priorImage.target = "_blank";
        priorImage.rel = "noopener";
        feedback.append(priorImage);
      }
      fields.append(feedback);
    }
    const originLabel = el("p", "review-origin", reviewOriginText(stored));
    const decisionRow = el("div", "decision-row");
    const decisionLabel = el("label", "", "Your decision");
    decisionLabel.htmlFor = `decision-${proposal.id}`;
    const decision = el("select");
    decision.id = decisionLabel.htmlFor;
    [["pending", "Pending approval"], ["changes", "Changes requested"], ["approve", "Approve this proposal"]].forEach(([value, label]) => {
      const option = new Option(label, value);
      decision.add(option);
    });
    decision.value = stored.status;
    decision.disabled = !proposal.bound;
    decision.addEventListener("change", () => {
      save(proposal.id, "status", decision.value);
      const current = entry(proposal.id);
      badge.textContent = reviewLabel(current);
      badge.dataset.status = current.status;
      originLabel.textContent = reviewOriginText(current);
      if (onlyNeedsReview && current.status === "approve") {
        renderGallery();
        $("review-filter").focus();
      }
    });
    decisionRow.append(decisionLabel, decision);
    const notesLabel = el("label", "", "Notes for this proposal");
    notesLabel.htmlFor = `notes-${proposal.id}`;
    const notes = el("textarea");
    notes.id = notesLabel.htmlFor;
    notes.rows = 2;
    notes.placeholder = "Silhouette, face, hair, skin, outfit…";
    notes.value = stored.notes;
    notes.disabled = !proposal.bound;
    notes.addEventListener("input", () => {
      save(proposal.id, "notes", notes.value);
      const current = entry(proposal.id);
      originLabel.textContent = reviewOriginText(current);
      badge.textContent = reviewLabel(current);
    });
    const cardFooter = el("div", "card-footer");
    const source = el("a", "", "Original PNG ↗");
    source.href = proposal.image;
    source.target = "_blank";
    source.rel = "noopener";
    const assetLabel = el("span", "asset-id", proposal.bound ? `${proposal.id} · r${proposal.revision} · ${proposal.sha256.slice(0, 8)}` : proposal.id);
    if (proposal.bound) assetLabel.title = `SHA-256: ${proposal.sha256}`;
    cardFooter.append(assetLabel, source);
    fields.append(originLabel, decisionRow, notesLabel, notes, cardFooter);
    card.append(title, open, fields);
    $("gallery").append(card);
  });
  $("empty-state").hidden = visible.length > 0;
  const groupLabel = genderFilter === "all" ? "All character proposals" : genderFilter === "male" ? "Men · Seven body presets" : "Women · Seven body presets";
  const presetLabel = PRESETS.find(p => p[0] === presetFilter)?.[1];
  const galleryTitle = presetLabel ? `${genderFilter === "all" ? "Men & women" : genderFilter === "male" ? "Men" : "Women"} · ${presetLabel}` : groupLabel;
  $("gallery-title").textContent = `${galleryTitle}${onlyNeedsReview ? " · Needs review" : ""}`;
  updateCount();
}
function updateCount() {
  const counts = {pending: 0, changes: 0, approve: 0};
  PROPOSALS.forEach(p => { counts[entry(p.id).status]++; });
  const parts = [`${counts.pending} pending approval`];
  if (counts.changes) parts.push(`${counts.changes} changes requested`);
  if (counts.approve) parts.push(`${counts.approve} approved`);
  $("review-count").textContent = parts.join(" · ");
}
function setZoom(value) {
  zoom = Math.min(3, Math.max(0.5, value));
  $("dialog-image").style.width = `${zoom * 100}%`;
  $("zoom-value").textContent = `${Math.round(zoom * 100)}%`;
  $("zoom-out").disabled = zoom <= 0.5;
  $("zoom-in").disabled = zoom >= 3;
}
function openArtboard(proposal) {
  currentProposal = proposal;
  const isReference = proposal.id === "reference";
  $("dialog-title").textContent = proposal.title;
  $("dialog-kicker").textContent = isReference ? "APPROVED VISUAL STYLE REFERENCE" :
    `CHARACTER PROPOSAL · APPROVAL ROUND 1${proposal.bound ? ` · ASSET R${proposal.revision}` : ""}`;
  $("dialog-image").src = proposal.image;
  $("dialog-image").alt = isReference ? "Original illustrated character style reference" : `${proposal.title}: complete three-view artboard`;
  $("open-source").href = proposal.image;
  $("dialog-position").textContent = isReference ? "Style reference" : `${PROPOSALS.findIndex(p => p.id === proposal.id) + 1} / ${PROPOSALS.length}`;
  $("previous-artboard").disabled = isReference;
  $("next-artboard").disabled = isReference;
  setZoom(1);
  $("artboard-viewport").scrollTo(0, 0);
  if (!$("artboard-dialog").open) $("artboard-dialog").showModal();
}
function nextArtboard(direction) {
  const index = PROPOSALS.findIndex(p => p.id === currentProposal?.id);
  if (index < 0) return;
  openArtboard(PROPOSALS[(index + direction + PROPOSALS.length) % PROPOSALS.length]);
}
function renderComparison(side) {
  const proposal = PROPOSALS.find(p => p.id === $(`compare-${side}`).value);
  const target = $(`compare-${side}-image`);
  target.replaceChildren(artwork(proposal, true));
  target.setAttribute("aria-label", `Inspect full artboard: ${proposal.title}`);
  target.onclick = () => openArtboard(proposal);
}
function toggleComparison(show) {
  $("comparison").hidden = !show;
  $("toggle-compare").setAttribute("aria-expanded", String(show));
  if (show) { renderComparison("left"); renderComparison("right"); $("compare-left").focus(); }
  else $("toggle-compare").focus();
}

PRESETS.forEach(([id, label]) => $("preset-filter").add(new Option(label, id)));
$("preset-filter").addEventListener("change", renderGallery);
$("review-filter").addEventListener("change", renderGallery);
document.querySelectorAll("[data-gender]").forEach(button => button.addEventListener("click", () => {
  genderFilter = button.dataset.gender;
  document.querySelectorAll("[data-gender]").forEach(tab => tab.setAttribute("aria-pressed", String(tab === button)));
  renderGallery();
}));
["left", "right"].forEach(side => {
  PROPOSALS.forEach(p => $(`compare-${side}`).add(new Option(p.title, p.id)));
  $(`compare-${side}`).addEventListener("change", () => renderComparison(side));
});
$("compare-left").value = "male-normal";
$("compare-right").value = "male-muscular";
$("toggle-compare").addEventListener("click", () => toggleComparison($("comparison").hidden));
$("close-compare").addEventListener("click", () => toggleComparison(false));
$("open-reference").addEventListener("click", () => openArtboard({id: "reference", title: "Original illustrated style", image: "../references/illustrated-style-r1.png"}));
$("close-dialog").addEventListener("click", () => $("artboard-dialog").close());
$("zoom-in").addEventListener("click", () => setZoom(zoom + 0.25));
$("zoom-out").addEventListener("click", () => setZoom(zoom - 0.25));
$("zoom-reset").addEventListener("click", () => {setZoom(1); $("artboard-viewport").scrollTo(0, 0);});
$("previous-artboard").addEventListener("click", () => nextArtboard(-1));
$("next-artboard").addEventListener("click", () => nextArtboard(1));
$("artboard-dialog").addEventListener("keydown", event => {
  if (["+", "=", "-", "0", "ArrowLeft", "ArrowRight"].includes(event.key)) event.preventDefault();
  if (event.key === "+" || event.key === "=") setZoom(zoom + 0.25);
  if (event.key === "-") setZoom(zoom - 0.25);
  if (event.key === "0") {setZoom(1); $("artboard-viewport").scrollTo(0, 0);}
  if (event.key === "ArrowLeft") nextArtboard(-1);
  if (event.key === "ArrowRight") nextArtboard(1);
});
$("export-review").addEventListener("click", () => {
  const payload = {
    schema: "sologym.character-art-review", version: 2, round: "approval-r1",
    exportedAt: new Date().toISOString(),
    scope: "Local user review of illustrated art proposals only. Does not update canonical asset approval or validate runtime readiness.",
    manifest: {path: "manifest.json", sha256: CATALOG_VALID ? ASSET_CATALOG.manifest_sha256 : null},
    reference: {path: "../references/illustrated-style-r1.png", sha256: CATALOG_VALID ? ASSET_CATALOG.style_reference_sha256 : null},
    proposals: PROPOSALS.map(p => ({id: p.id, gender: p.gender, preset: p.preset,
      source: sourceOf(p), source_bound: p.bound, ...entry(p.id)}))
  };
  const url = URL.createObjectURL(new Blob([JSON.stringify(payload, null, 2) + "\n"], {type: "application/json"}));
  const link = el("a");
  link.href = url;
  link.download = `sologym-character-review-r1-${new Date().toISOString().slice(0, 10)}.json`;
  document.body.append(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
  showToast("Review exported. Keep this JSON file with the approval round.");
});
renderGallery();
if (PROPOSALS.some(p => !p.bound)) showToast("Some asset revision data is unavailable. Review decisions for those images are disabled.");
else if (!storageAvailable) showToast("Browser storage is unavailable. Export JSON to keep your review.");
