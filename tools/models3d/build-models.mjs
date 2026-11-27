import { NodeIO, Document, getBounds } from '@gltf-transform/core';
import { ALL_EXTENSIONS, EXTMeshoptCompression } from '@gltf-transform/extensions';
import { prune, dedup, quantize, resample, weld, mergeDocuments, unpartition } from '@gltf-transform/functions';
import { MeshoptEncoder, MeshoptDecoder } from 'meshoptimizer';
import fs from 'node:fs';
await MeshoptEncoder.ready; await MeshoptDecoder.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({ 'meshopt.encoder': MeshoptEncoder, 'meshopt.decoder': MeshoptDecoder });
const [KK, OUT, QM] = process.argv.slice(2);
if (!KK || !OUT || !QM) throw new Error('Usage: node build-models.mjs <kaykit-folder> <output-folder> <ultimate-monsters-folder>');
const ADV = `${KK}/KayKit-Character-Pack-Adventures-1.0/addons/kaykit_character_pack_adventures`;
const SKL = `${KK}/KayKit-Character-Pack-Skeletons-1.0/addons/kaykit_character_pack_skeletons`;
const DUN = `${KK}/KayKit-Dungeon-Remastered-1.0/addons/kaykit_dungeon_remastered/Assets/gltf`;
fs.mkdirSync(OUT, { recursive: true });

const heroClips = ['Idle','2H_Melee_Idle','Walking_A','Running_A','1H_Melee_Attack_Chop','1H_Melee_Attack_Stab','2H_Melee_Attack_Slice','2H_Melee_Attack_Chop','2H_Melee_Attack_Stab','Unarmed_Idle','Unarmed_Melee_Attack_Punch_A','Dualwield_Melee_Attack_Stab','2H_Ranged_Shoot','1H_Ranged_Shoot','Spellcast_Shoot','Spellcast_Raise','Hit_A','Block_Hit','Death_A','Death_A_Pose','Cheer','Use_Item','PickUp','Interact'];
const skeletonClips = ['Idle','Idle_Combat','Walking_D_Skeletons','Walking_A','1H_Melee_Attack_Chop','Unarmed_Melee_Attack_Punch_A','Spellcast_Shoot','Hit_A','Death_A','Death_A_Pose','Skeletons_Awaken_Standing','Skeleton_Inactive_Standing_Pose','Taunt','Spawn_Ground'];

async function finish(doc, name) {
  for (const accessor of doc.getRoot().listAccessors()) if (accessor.listParents().every(p => p.propertyType === 'Root')) accessor.dispose();
  await doc.transform(dedup(), weld(), resample(), prune(), quantize({ quantizeTexcoord: 12 }), unpartition());
  doc.createExtension(EXTMeshoptCompression).setRequired(true).setEncoderOptions({ method: EXTMeshoptCompression.EncoderMethod.FILTER });
  await io.write(`${OUT}/${name}.glb`, doc);
  console.log(name, (fs.statSync(`${OUT}/${name}.glb`).size / 1024).toFixed(0) + ' KB');
}

async function character(file, name, clips, dropMeshes) {
  const doc = await io.read(file);
  for (const anim of doc.getRoot().listAnimations()) if (!clips.includes(anim.getName())) { anim.listChannels().forEach(c => c.dispose()); anim.listSamplers().forEach(s => s.dispose()); anim.dispose(); }
  for (const node of doc.getRoot().listNodes()) {
    if (node.getMesh() && (dropMeshes === undefined || !node.getSkin() || name === 'animations') && dropMeshes(node.getName())) { node.getMesh().dispose(); node.dispose(); }
  }
  await finish(doc, name);
}

const weaponLike = n => /Sword|Shield|Axe|Mug|Spellbook|Wand|Staff|Crossbow|Knife|Throwable/.test(n);
await character(`${SKL}/Characters/gltf/Skeleton_Minion.glb`, 'animations', [...new Set([...heroClips, ...skeletonClips])], () => true);
const none = [];
await character(`${ADV}/Characters/gltf/Knight.glb`, 'hero-knight', none, weaponLike);
await character(`${ADV}/Characters/gltf/Barbarian.glb`, 'hero-berserker', none, weaponLike);
await character(`${ADV}/Characters/gltf/Rogue_Hooded.glb`, 'hero-ranger', none, weaponLike);
await character(`${ADV}/Characters/gltf/Rogue.glb`, 'hero-rogue', none, weaponLike);
await character(`${ADV}/Characters/gltf/Mage.glb`, 'hero-mage', none, weaponLike);
for (const s of ['Minion', 'Warrior', 'Rogue', 'Mage'])
  await character(`${SKL}/Characters/gltf/Skeleton_${s}.glb`, `monster-skeleton-${s.toLowerCase()}`, none, () => false);

async function kit(name, entries) {
  const doc = new Document();
  const scene = doc.createScene('kit');
  for (const [nodeName, file] of entries) {
    const part = await io.read(file);
    const map = mergeDocuments(doc, part);
    const partScene = map.get(part.getRoot().listScenes()[0]);
    const holder = doc.createNode(nodeName);
    for (const child of partScene.listChildren()) holder.addChild(child);
    scene.addChild(holder);
    partScene.dispose();
  }
  doc.getRoot().setDefaultScene(scene);
  for (const s of doc.getRoot().listScenes()) if (s !== scene) s.dispose();
  await finish(doc, name);
}

await kit('dungeon', [
  ['floor', `${DUN}/floor_tile_large.gltf.glb`],
  ['floor-rocks', `${DUN}/floor_tile_large_rocks.gltf.glb`],
  ['floor-dirt', `${DUN}/floor_dirt_large.gltf.glb`],
  ['wall', `${DUN}/wall.gltf.glb`],
  ['wall-cracked', `${DUN}/wall_cracked.gltf.glb`],
  ['pillar', `${DUN}/pillar.gltf.glb`],
  ['barrel', `${DUN}/barrel_large.gltf.glb`],
  ['rubble', `${DUN}/rubble_half.gltf.glb`],
  ['chest', `${DUN}/chest.glb`],
  ['stairs-hole', `${DUN}/floor_tile_big_grate_open.gltf.glb`],
  ['stairs-sealed', `${DUN}/floor_tile_big_grate.gltf.glb`],
  ['torch', `${DUN}/torch_mounted.gltf.glb`],
  ['waystone', `${DUN}/pillar_decorated.gltf.glb`],
  ['banner', `${DUN}/banner_patternA_red.gltf.glb`],
  ['coins', `${DUN}/coin_stack_small.gltf.glb`],
  ['bottle', `${DUN}/bottle_A_green.gltf.glb`],
  ['grave', `${DUN}/sword_shield_broken.gltf.glb`],
  ['merchant-table', `${DUN}/table_small_decorated_A.gltf.glb`],
  ['wall-shelves', `${DUN}/wall_shelves.gltf.glb`],
  ['wall-window', `${DUN}/wall_window_closed.gltf.glb`],
  ['wall-arched', `${DUN}/wall_archedwindow_gated.gltf.glb`],
  ['banner-wall', `${DUN}/banner_patternA_red.gltf.glb`],
  ['banner-thin', `${DUN}/banner_thin_blue.gltf.glb`],
  ['crates', `${DUN}/crates_stacked.gltf.glb`],
  ['barrels-small', `${DUN}/barrel_small_stack.gltf.glb`],
  ['keg', `${DUN}/keg_decorated.gltf.glb`],
  ['shelf', `${DUN}/shelf_large.gltf.glb`],
  ['table', `${DUN}/table_medium_decorated_A.gltf.glb`],
  ['trunk', `${DUN}/trunk_medium_A.gltf.glb`],
  ['box', `${DUN}/box_large.gltf.glb`],
  ['candles', `${DUN}/candle_triple.gltf.glb`],
  ['weapon-rack', `${DUN}/sword_shield.gltf.glb`],
  ['column', `${DUN}/column.gltf.glb`],
  ['stool', `${DUN}/stool.gltf.glb`],
]);
const A = `${ADV}/Assets/gltf`, S = `${SKL}/Assets/gltf`;
await kit('weapons', [
  ['sword', `${A}/sword_1handed.gltf`], ['sword-2h', `${A}/sword_2handed.gltf`],
  ['axe', `${A}/axe_1handed.gltf`], ['axe-2h', `${A}/axe_2handed.gltf`],
  ['dagger', `${A}/dagger.gltf`], ['staff', `${A}/staff.gltf`], ['wand', `${A}/wand.gltf`],
  ['crossbow', `${A}/crossbow_2handed.gltf`], ['shield', `${A}/shield_round.gltf`], ['shield-square', `${A}/shield_square.gltf`],
  ['spellbook', `${A}/spellbook_closed.gltf`], ['arrow', `${A}/arrow.gltf`],
  ['skeleton-blade', `${S}/Skeleton_Blade.gltf`], ['skeleton-axe', `${S}/Skeleton_Axe.gltf`], ['skeleton-staff', `${S}/Skeleton_Staff.gltf`],
  ['skeleton-shield', `${S}/Skeleton_Shield_Small_A.gltf`],
]);

const clipSets = {
  Big: { Idle: 'Idle', Walk: 'Walk', Attack: 'Punch', Hit: 'HitReact', Death: 'Death', Wake: 'Yes', Sleep: 'Duck' },
  BigArmed: { Idle: 'Idle', Walk: 'Walk', Attack: 'Weapon', Hit: 'HitReact', Death: 'Death', Wake: 'Yes', Sleep: 'Duck' },
  Blob: { Idle: 'Idle', Walk: 'Walk', Attack: 'Bite_Front', Hit: 'HitRecieve', Death: 'Death', Wake: 'Jump' },
  Flying: { Idle: 'Flying_Idle', Walk: 'Fast_Flying', Attack: 'Headbutt', Hit: 'HitReact', Death: 'Death', Wake: 'Yes' },
};

const monsters = [
  ['Big', 'Demon', 'BigArmed'], ['Big', 'Dino', 'Big'], ['Big', 'Orc', 'BigArmed'],
  ['Big', 'Orc_Skull', 'BigArmed'], ['Big', 'Yeti', 'Big'], ['Big', 'Ninja', 'BigArmed'], ['Big', 'Tribal', 'Big'],
  ['Big', 'MushroomKing', 'BigArmed'],
  ['Blob', 'GreenBlob', 'Blob'], ['Blob', 'Mushnub', 'Blob'], ['Blob', 'Mushnub_Evolved', 'Blob'], ['Blob', 'Fish', 'Blob'],
  ['Blob', 'PinkBlob', 'Blob'], ['Blob', 'GreenSpikyBlob', 'Blob'],
  ['Flying', 'Ghost', 'Flying'], ['Flying', 'Ghost_Skull', 'Flying'], ['Flying', 'Dragon', 'Flying'], ['Flying', 'Armabee', 'Flying'],
  ['Flying', 'Squidle', 'Flying'], ['Flying', 'Goleling', 'Flying'], ['Flying', 'Goleling_Evolved', 'Flying'],
  ['Flying', 'Glub_Evolved', 'Flying'], ['Flying', 'Alpaking_Evolved', 'Flying'],
  ['Flying', 'Dragon_Evolved', 'Flying'],
];

for (const [group, name, set] of monsters) {
  const doc = await io.read(`${QM}/${group}/glTF/${name}.gltf`);
  const clips = clipSets[set];
  const wanted = new Map(Object.entries(clips).map(([standard, source]) => [source, standard]));
  for (const anim of doc.getRoot().listAnimations()) {
    const source = anim.getName().replace(/^.*\|/, '');
    const standard = wanted.get(source);
    if (standard) {
      anim.setName(standard);
      wanted.delete(source);
    } else {
      anim.listChannels().forEach(c => c.dispose());
      anim.listSamplers().forEach(s => s.dispose());
      anim.dispose();
    }
  }
  if (wanted.size > 0) throw new Error(`${name}: missing clips ${[...wanted.keys()].join(', ')}`);

  const scene = doc.getRoot().listScenes()[0];
  const bounds = getBounds(scene);
  const height = bounds.max[1] - Math.min(0, bounds.min[1]);
  const holder = doc.createNode('Monster').setScale([1 / height, 1 / height, 1 / height]);
  for (const child of scene.listChildren()) holder.addChild(child);
  scene.addChild(holder);
  await finish(doc, `monster-${name.toLowerCase().replace(/_/g, '-')}`);
}

{
  const doc = await io.read(`${DUN}/chest.glb`);
  const scene = doc.getRoot().listScenes()[0];
  const bounds = getBounds(scene);
  const height = bounds.max[1] - Math.min(0, bounds.min[1]);
  const holder = doc.createNode('Monster').setScale([1 / height, 1 / height, 1 / height]);
  for (const child of scene.listChildren()) holder.addChild(child);
  scene.addChild(holder);
  await finish(doc, 'monster-mimic');
}
