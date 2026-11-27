const openings = ['Al', 'Bor', 'Cae', 'Dor', 'El', 'Fen', 'Gar', 'Hal', 'Is', 'Ka', 'Lor', 'Mor', 'Nym', 'Or', 'Ry', 'Sa', 'Tor', 'Ul', 'Vey', 'Wyn']
const endings = ['dric', 'wen', 'ric', 'ion', 'a', 'iel', 'gar', 'mir', 'eth', 'ra', 'dor', 'lin', 'vyn', 'mund', 'is']

export function randomHeroName(random: () => number = Math.random): string {
  const pick = (options: string[]) => options[Math.min(options.length - 1, Math.floor(random() * options.length))]
  return pick(openings) + pick(endings)
}

export function classLabel(classId: string | null): string {
  if (!classId) return 'Hero'
  return classId.charAt(0).toUpperCase() + classId.slice(1)
}
