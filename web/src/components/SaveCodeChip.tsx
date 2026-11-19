import { useEffect, useState } from 'react'

const copiedDuration = 1500

interface SaveCodeChipProps {
  code: string
}

export function SaveCodeChip({ code }: SaveCodeChipProps) {
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    if (!copied) return

    const timer = window.setTimeout(() => setCopied(false), copiedDuration)
    return () => window.clearTimeout(timer)
  }, [copied])

  async function copy() {
    try {
      await navigator.clipboard.writeText(code)
      setCopied(true)
    } catch {
      setCopied(false)
    }
  }

  return (
    <button
      className="save-code-chip"
      title="Your game saves after every turn. Use this code to continue on another device."
      onClick={() => void copy()}
    >
      <span className="label">Save</span> <code>{code}</code> <span className="save-code-hint">{copied ? 'copied' : 'copy'}</span>
    </button>
  )
}
