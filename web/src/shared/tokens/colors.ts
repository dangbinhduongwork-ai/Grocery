export const colors = {
  paper: '#FAF7F0',
  paper2: '#F2EDE0',
  ink: '#1B2B22',
  ink2: '#4A5A50',
  leaf: '#1F3A2D',
  leaf2: '#2F5A44',
  sprout: '#7FA38A',
  tomato: '#D9432F',
  tomatoInk: '#B8321F',
  turmeric: '#E3A21A',
  rule: '#D9D1BD',
  ok: '#2E7D4F',
  warn: '#B7791F',
  err: '#B8321F',
  info: '#2B6C8F',
} as const

export type ColorToken = keyof typeof colors
