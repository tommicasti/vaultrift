import { BoxGeometry, DoubleSide, ConeGeometry, CylinderGeometry, Group, IcosahedronGeometry, Mesh, MeshStandardMaterial, Object3D, OctahedronGeometry, SphereGeometry, TorusGeometry } from 'three'

interface Palette {
  body: string
  shell: string
  legs: string
  accent: string
}

const materials = new Map<string, MeshStandardMaterial>()

function material(color: string): MeshStandardMaterial {
  let found = materials.get(color)
  if (!found) {
    found = new MeshStandardMaterial({ color, roughness: 0.75, flatShading: true })
    materials.set(color, found)
  }
  return found
}

function blob(radius: number, color: string, detail = 1): Mesh {
  const mesh = new Mesh(new IcosahedronGeometry(radius, detail), material(color))
  mesh.castShadow = true
  return mesh
}

function rod(length: number, radius: number, color: string): Mesh {
  const geometry = new CylinderGeometry(radius * 0.75, radius, length, 6)
  geometry.rotateZ(-Math.PI / 2)
  geometry.translate(length / 2, 0, 0)
  const mesh = new Mesh(geometry, material(color))
  mesh.castShadow = true
  return mesh
}

function eye(radius: number): Group {
  const group = new Group()
  const white = new Mesh(new SphereGeometry(radius, 12, 10), material('#f4f4f0'))
  const pupil = new Mesh(new SphereGeometry(radius * 0.48, 10, 8), material('#141414'))
  pupil.position.z = radius * 0.62
  group.add(white, pupil)
  return group
}

function place<T extends Object3D>(object: T, x: number, y: number, z: number): T {
  object.position.set(x, y, z)
  return object
}

function leg(side: 1 | -1, yaw: number, upper: number, lower: number, radius: number, color: string, rise: number): Group {
  const root = new Group()
  root.userData.leg = side
  root.userData.yaw = side > 0 ? -yaw : Math.PI + yaw
  root.rotation.y = root.userData.yaw

  const hip = new Group()
  hip.name = 'hip'
  hip.userData.base = rise
  hip.rotation.z = rise
  hip.add(rod(upper, radius, color))

  const knee = place(new Group(), upper, 0, 0)
  knee.rotation.z = -rise - 1.05
  knee.add(rod(lower, radius * 0.8, color))
  knee.add(place(blob(radius * 0.9, color, 0), 0, 0, 0))

  hip.add(knee)
  root.add(hip)
  return root
}

function legs(count: number, at: { x: number; y: number; z: number; spread: number }, sizes: { upper: number; lower: number; radius: number }, color: string): Group {
  const group = new Group()
  for (let index = 0; index < count; index++) {
    const yaw = count === 1 ? 0 : 0.65 - (index / (count - 1)) * 1.3
    const z = at.z + at.spread * (0.5 - index / Math.max(1, count - 1))
    for (const side of [1, -1] as const) {
      const limb = leg(side, yaw, sizes.upper, sizes.lower, sizes.radius, color, 0.5)
      limb.position.set(at.x * side, at.y, z)
      group.add(limb)
    }
  }
  return group
}

function claw(side: 1 | -1, color: string, scale: number): Group {
  const arm = new Group()
  arm.position.set(0.22 * side * scale, 0.38 * scale, 0.32 * scale)
  arm.rotation.y = side > 0 ? -1.1 : Math.PI + 1.1
  arm.add(rod(0.28 * scale, 0.05 * scale, color))

  const hand = place(new Group(), 0.28 * scale, 0.05 * scale, 0)
  hand.rotation.y = side > 0 ? 0.9 : 0.9
  const palm = blob(0.13 * scale, color)
  palm.scale.set(1.3, 0.8, 0.9)
  hand.add(palm)

  const fixed = new Mesh(new ConeGeometry(0.06 * scale, 0.28 * scale, 5), material(color))
  fixed.rotation.z = -Math.PI / 2
  fixed.position.set(0.2 * scale, 0, -0.04 * scale)
  hand.add(fixed)

  const pincer = new Group()
  pincer.userData.claw = side > 0 ? -1 : -1
  pincer.position.set(0.08 * scale, 0, 0.05 * scale)
  const moving = new Mesh(new ConeGeometry(0.05 * scale, 0.26 * scale, 5), material(color))
  moving.rotation.z = -Math.PI / 2
  moving.position.x = 0.12 * scale
  pincer.add(moving)
  hand.add(pincer)

  arm.add(hand)
  return arm
}

function spiderBody(palette: Palette, crown: boolean): Group {
  const spider = new Group()
  const abdomen = place(blob(0.36, palette.body), 0, 0.5, -0.36)
  abdomen.scale.set(1, 0.85, 1.15)
  const stripe = place(blob(0.2, palette.accent, 0), 0, 0.78, -0.4)
  stripe.scale.set(1, 0.35, 1.4)
  const head = place(blob(0.25, palette.shell), 0, 0.46, 0.1)
  const fangs = [-1, 1].map(side => {
    const fang = new Mesh(new ConeGeometry(0.035, 0.14, 5), material('#f0e8d8'))
    fang.rotation.x = Math.PI
    return place(fang, 0.06 * side, 0.3, 0.3)
  })
  spider.add(abdomen, stripe, head, ...fangs)
  spider.add(place(eye(0.1), 0.09, 0.6, 0.27), place(eye(0.1), -0.09, 0.6, 0.27))
  spider.add(legs(4, { x: 0.14, y: 0.44, z: 0.1, spread: 0.3 }, { upper: 0.42, lower: 0.62, radius: 0.045 }, palette.legs))

  if (crown) {
    const band = place(new Mesh(new CylinderGeometry(0.15, 0.17, 0.07, 10), material('#f2c23a')), 0, 0.7, 0.05)
    spider.add(band)
    for (let index = 0; index < 5; index++) {
      const angle = (index / 5) * Math.PI * 2
      const spike = new Mesh(new ConeGeometry(0.04, 0.13, 4), material('#f2c23a'))
      spider.add(place(spike, Math.cos(angle) * 0.14, 0.79, 0.05 + Math.sin(angle) * 0.14))
    }
  }
  return spider
}

export function buildSpider(): Object3D {
  return spiderBody({ body: '#4b2a6e', shell: '#33204a', legs: '#2a1a3a', accent: '#c44a8a' }, false)
}

export function buildSpiderQueen(): Object3D {
  return spiderBody({ body: '#6e2a4e', shell: '#3a1a30', legs: '#2a1424', accent: '#f2c23a' }, true)
}

export function buildCrab(): Object3D {
  const crab = new Group()
  const shell = place(blob(0.38, '#d9533a'), 0, 0.42, 0)
  shell.scale.set(1.2, 0.6, 0.9)
  const belly = place(blob(0.3, '#f0a07a', 0), 0, 0.33, 0.04)
  belly.scale.set(1.2, 0.5, 0.9)
  crab.add(shell, belly)
  for (const side of [1, -1]) {
    const stalk = place(new Mesh(new CylinderGeometry(0.025, 0.03, 0.22, 5), material('#d9533a')), 0.1 * side, 0.6, 0.22)
    crab.add(stalk, place(eye(0.08), 0.1 * side, 0.74, 0.24))
  }
  crab.add(legs(3, { x: 0.32, y: 0.38, z: -0.05, spread: 0.35 }, { upper: 0.28, lower: 0.4, radius: 0.04 }, '#c4462f'))
  crab.add(claw(1, '#d9533a', 1.55), claw(-1, '#d9533a', 1.55))
  return crab
}

export function buildScorpion(): Object3D {
  const scorpion = new Group()
  const color = '#d8a84a'
  const dark = '#a8782a'
  const head = place(blob(0.22, color), 0, 0.34, 0.25)
  head.scale.set(1.1, 0.7, 1)
  const thorax = place(blob(0.26, dark), 0, 0.34, -0.05)
  thorax.scale.set(1.15, 0.65, 1.2)
  scorpion.add(head, thorax)
  scorpion.add(place(eye(0.08), 0.08, 0.46, 0.4), place(eye(0.08), -0.08, 0.46, 0.4))
  scorpion.add(legs(4, { x: 0.18, y: 0.32, z: -0.05, spread: 0.4 }, { upper: 0.28, lower: 0.42, radius: 0.035 }, dark))
  scorpion.add(claw(1, color, 0.9), claw(-1, color, 0.9))

  const tail = new Group()
  tail.name = 'tail'
  tail.position.set(0, 0.38, -0.32)
  let joint: Group = tail
  const segments = 5
  for (let index = 0; index < segments; index++) {
    const segment = new Group()
    segment.position.set(0, index === 0 ? 0 : 0.15, 0)
    segment.rotation.x = index === 0 ? -0.55 : 0.42
    const piece = blob(0.09 - index * 0.006, index % 2 === 0 ? color : dark, 0)
    piece.scale.set(1, 1.2, 1)
    segment.add(piece)
    joint.add(segment)
    joint = segment
  }
  const sting = new Mesh(new ConeGeometry(0.05, 0.18, 5), material('#3a2a1a'))
  sting.rotation.x = Math.PI * 0.75
  joint.add(place(sting, 0, 0.1, 0.05))
  scorpion.add(tail)
  return scorpion
}

export function buildBow(): Object3D {
  const bow = new Group()
  const limb = new Mesh(new TorusGeometry(0.75, 0.045, 5, 14, Math.PI), material('#8a5a2a'))
  limb.rotation.set(0, -Math.PI / 2, -Math.PI / 2)
  limb.position.y = 0.2
  const grip = place(new Mesh(new CylinderGeometry(0.06, 0.06, 0.24, 6), material('#4a2a14')), 0, 0.2, 0.75)
  const string = place(new Mesh(new CylinderGeometry(0.008, 0.008, 1.5, 4), material('#f0f0e0')), 0, 0.2, 0)
  bow.add(limb, grip, string)
  bow.position.z = -0.75
  const holder = new Group()
  holder.add(bow)
  return holder
}

export function buildSpear(): Object3D {
  const spear = new Group()
  spear.add(place(new Mesh(new CylinderGeometry(0.04, 0.045, 2.4, 6), material('#7a5230')), 0, 0.45, 0))
  spear.add(place(new Mesh(new ConeGeometry(0.1, 0.4, 5), material('#c8ccd4')), 0, 1.85, 0))
  spear.add(place(new Mesh(new CylinderGeometry(0.07, 0.07, 0.08, 6), material('#c8a040')), 0, 1.64, 0))
  return spear
}

export function buildItem(category: string, color: string): Object3D {
  const item = new Group()
  switch (category) {
    case 'Potion': {
      item.add(place(blob(0.28, color), 0, 0.28, 0))
      item.add(place(new Mesh(new CylinderGeometry(0.08, 0.1, 0.22, 6), material('#d8e8f0')), 0, 0.6, 0))
      item.add(place(new Mesh(new CylinderGeometry(0.09, 0.09, 0.08, 6), material('#8a5a2a')), 0, 0.74, 0))
      break
    }
    case 'Scroll': {
      const sheet = new Mesh(new BoxGeometry(0.62, 0.04, 0.5), material('#f2e2b8'))
      sheet.rotation.x = -0.5
      item.add(place(sheet, 0, 0.32, 0))
      for (const side of [-1, 1]) {
        const roll = new Mesh(new CylinderGeometry(0.07, 0.07, 0.7, 8), material('#e0cc98'))
        roll.rotation.z = Math.PI / 2
        item.add(place(roll, 0, 0.32 + side * 0.12, side * 0.22))
      }
      const seal = new Mesh(new OctahedronGeometry(0.09, 0), material(color))
      item.add(place(seal, 0, 0.36, 0.03))
      item.add(place(new Mesh(new TorusGeometry(0.075, 0.02, 4, 10), material('#c84a3a')), 0.26, 0.2, 0.22))
      break
    }
    case 'Rune': {
      const slab = new Mesh(new BoxGeometry(0.42, 0.56, 0.12), material('#6a6878'))
      item.add(place(slab, 0, 0.34, 0))
      const glyph = new Mesh(new OctahedronGeometry(0.13, 0), material(color))
      glyph.scale.set(1, 1.4, 0.4)
      item.add(place(glyph, 0, 0.36, 0.07))
      break
    }
    case 'Ring': {
      const band = new Mesh(new TorusGeometry(0.25, 0.06, 6, 16), material('#f2c23a'))
      item.add(place(band, 0, 0.35, 0))
      item.add(place(blob(0.09, color, 0), 0, 0.62, 0))
      break
    }
    case 'Head': {
      const helmet = new Mesh(new SphereGeometry(0.34, 10, 6, 0, Math.PI * 2, 0, Math.PI / 2), material('#a8acb4'))
      item.add(place(helmet, 0, 0.05, 0))
      item.add(place(new Mesh(new ConeGeometry(0.06, 0.25, 4), material(color)), 0, 0.45, 0))
      break
    }
    case 'Body': {
      const plate = place(blob(0.32, '#a8acb4', 0), 0, 0.35, 0)
      plate.scale.set(1.1, 1.2, 0.6)
      item.add(plate, place(blob(0.1, color, 0), 0, 0.45, 0.17))
      break
    }
    case 'Feet':
      for (const side of [-1, 1]) {
        item.add(place(new Mesh(new CylinderGeometry(0.1, 0.11, 0.3, 6), material('#6a4a2a')), 0.14 * side, 0.2, 0))
        item.add(place(blob(0.12, '#5a3a1a', 0), 0.14 * side, 0.08, 0.1))
      }
      break
    case 'Cloak':
      item.add(place(new Mesh(new ConeGeometry(0.35, 0.7, 7), material(color)), 0, 0.35, 0))
      break
    case 'ClassMod': {
      const book = place(new Mesh(new CylinderGeometry(0.3, 0.3, 0.12, 4), material('#6a3a8a')), 0, 0.12, 0)
      book.rotation.y = Math.PI / 4
      item.add(book, place(blob(0.08, color, 0), 0, 0.2, 0))
      break
    }
    default:
      item.add(place(new Mesh(new OctahedronGeometry(0.42, 0), material(color)), 0, 0.45, 0))
  }
  return item
}

export function buildStairs(): Object3D {
  const stairs = new Group()
  const pit = new Mesh(new BoxGeometry(3.9, 3, 3.9), material('#050308'))
  stairs.add(place(pit, 0, -1.55, 0))
  for (let step = 0; step < 6; step++) {
    const stone = new Mesh(new BoxGeometry(3.2, 0.3, 0.62), material(step % 2 === 0 ? '#8a8478' : '#7a7468'))
    stone.receiveShadow = true
    stairs.add(place(stone, 0, -0.15 - step * 0.32, -1.6 + step * 0.62))
  }
  for (const side of [-1, 1]) {
    const post = new Mesh(new BoxGeometry(0.45, 2.6, 0.45), material('#6a645a'))
    post.castShadow = true
    stairs.add(place(post, 1.75 * side, 1.3, -1.75))
  }
  const lintel = new Mesh(new BoxGeometry(3.95, 0.45, 0.5), material('#5a544a'))
  lintel.castShadow = true
  stairs.add(place(lintel, 0, 2.7, -1.75))
  return stairs
}

export function buildTotem(): Object3D {
  const totem = new Group()
  const tiers = ['#8a4a2a', '#a85a2a', '#6a3a1a']
  tiers.forEach((color, index) => {
    const block = new Mesh(new BoxGeometry(1.1 - index * 0.1, 0.9, 1.1 - index * 0.1), material(color))
    block.castShadow = true
    totem.add(place(block, 0, 0.45 + index * 0.92, 0))
    for (const side of [-1, 1]) totem.add(place(new Mesh(new BoxGeometry(0.18, 0.12, 0.05), material('#ff4a3a')), 0.2 * side, 0.6 + index * 0.92, 0.56 - index * 0.05))
    totem.add(place(new Mesh(new BoxGeometry(0.5, 0.1, 0.05), material('#2a140a')), 0, 0.3 + index * 0.92, 0.56 - index * 0.05))
  })
  for (const side of [-1, 1]) {
    const wing = new Mesh(new ConeGeometry(0.35, 1.2, 3), material('#d8a84a'))
    wing.rotation.z = (Math.PI / 2) * side
    totem.add(place(wing, 0.95 * side, 2.55, 0))
  }
  totem.add(place(new Mesh(new OctahedronGeometry(0.32, 0), material('#ff4a6a')), 0, 3.35, 0))
  return totem
}

interface RatLook {
  fur: string
  belly: string
  skin: string
  eyes: string
  royal: boolean
}

function ratBody(look: RatLook): Group {
  const rat = new Group()
  const body = place(blob(0.3, look.fur), 0, 0.3, -0.05)
  body.scale.set(0.95, 0.85, 1.45)
  const belly = place(blob(0.22, look.belly, 0), 0, 0.22, 0)
  belly.scale.set(0.9, 0.6, 1.3)
  const head = place(blob(0.19, look.fur), 0, 0.36, 0.38)
  head.scale.set(0.95, 0.9, 1.1)
  const snout = new Mesh(new ConeGeometry(0.11, 0.26, 7), material(look.fur))
  snout.rotation.x = Math.PI / 2
  const nose = place(blob(0.045, look.skin, 0), 0, 0.33, 0.68)
  rat.add(body, belly, head, place(snout, 0, 0.33, 0.56), nose)

  for (const side of [1, -1]) {
    const ear = place(new Mesh(new CylinderGeometry(0.1, 0.1, 0.03, 10), material(look.fur)), 0.13 * side, 0.54, 0.3)
    ear.rotation.set(Math.PI / 2.4, 0, side * 0.5)
    const inner = place(new Mesh(new CylinderGeometry(0.065, 0.065, 0.032, 10), material(look.skin)), 0.13 * side, 0.545, 0.315)
    inner.rotation.copy(ear.rotation)
    const pupil = place(blob(0.04, look.eyes, 0), 0.085 * side, 0.43, 0.5)
    const whisker = place(rod(0.2, 0.006, '#e8e0d8'), 0.04 * side, 0.32, 0.62)
    whisker.rotation.set(0, side > 0 ? -0.3 : Math.PI + 0.3, 0.1)
    rat.add(ear, inner, pupil, whisker)
  }

  const teeth = place(new Mesh(new BoxGeometry(0.05, 0.05, 0.02), material('#f4efd8')), 0, 0.26, 0.64)
  rat.add(teeth)
  rat.add(legs(2, { x: 0.15, y: 0.2, z: 0.02, spread: 0.42 }, { upper: 0.12, lower: 0.18, radius: 0.04 }, look.skin))

  const tail = new Group()
  tail.name = 'tail'
  tail.userData.sway = true
  tail.position.set(0, 0.26, -0.45)
  let joint: Group = tail
  for (let index = 0; index < 6; index++) {
    const segment = new Group()
    segment.position.set(0, 0, index === 0 ? 0 : -0.11)
    segment.rotation.x = index === 0 ? 0.35 : -0.12
    const piece = new Mesh(new CylinderGeometry(0.035 - index * 0.005, 0.04 - index * 0.005, 0.13, 5), material(look.skin))
    piece.rotation.x = Math.PI / 2
    piece.position.z = -0.05
    segment.add(piece)
    joint.add(segment)
    joint = segment
  }
  rat.add(tail)

  if (look.royal) {
    const cape = place(new Mesh(new CylinderGeometry(0.34, 0.42, 0.5, 12, 1, true, Math.PI * 0.6, Math.PI * 0.8), material('#a8202a')), 0, 0.42, -0.05)
    cape.rotation.x = -Math.PI / 2.6
    cape.material = new MeshStandardMaterial({ color: '#a8202a', roughness: 0.7, flatShading: true, side: DoubleSide })
    const collar = place(new Mesh(new TorusGeometry(0.17, 0.045, 6, 14), material('#f0ece0')), 0, 0.38, 0.22)
    collar.rotation.x = Math.PI / 2.2
    rat.add(cape, collar)

    const band = place(new Mesh(new CylinderGeometry(0.12, 0.14, 0.07, 10), material('#f2c23a')), 0, 0.55, 0.36)
    rat.add(band)
    for (let index = 0; index < 5; index++) {
      const angle = (index / 5) * Math.PI * 2
      const spike = new Mesh(new ConeGeometry(0.035, 0.12, 4), material('#f2c23a'))
      rat.add(place(spike, Math.cos(angle) * 0.115, 0.64, 0.36 + Math.sin(angle) * 0.115))
    }
    rat.add(place(new Mesh(new OctahedronGeometry(0.04), material('#e8304a')), 0, 0.56, 0.49))
  }
  return rat
}

export function buildRat(): Object3D {
  return ratBody({ fur: '#7a6a62', belly: '#b0a298', skin: '#e0a0a0', eyes: '#141414', royal: false })
}

export function buildRatKing(): Object3D {
  return ratBody({ fur: '#4a3e3a', belly: '#8a7a70', skin: '#c88a8a', eyes: '#ff3a2a', royal: true })
}
